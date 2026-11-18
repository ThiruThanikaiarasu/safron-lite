"""Acceptance tests for specs/001-solution-and-build-scaffolding.

Black-box checks against the repository root: they run the dotnet CLI, read
Saffron.sln and the project files, compare the repository with baseline.sha256
(recorded before implementation), and scan for out-of-scope artefacts. They
never import implementation code.

Run from any directory; paths resolve from this file's location:
    python3 .referee/001-solution-and-build-scaffolding/test_acceptance.py -v
or
    python3 -m unittest discover -s .referee/001-solution-and-build-scaffolding -v
"""

import functools
import hashlib
import json
import os
import re
import subprocess
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SLN = ROOT / "Saffron.sln"
BASELINE_FILE = HERE / "baseline.sha256"
DOTNET_TIMEOUT_SECONDS = 1200

PROJECT_PATHS = {
    "Saffron.Api": ROOT / "api" / "Saffron.Api" / "Saffron.Api.csproj",
    "Saffron.Api.Tests": ROOT / "api" / "Saffron.Api.Tests" / "Saffron.Api.Tests.csproj",
    "Saffron.Rules": ROOT / "rules" / "Saffron.Rules" / "Saffron.Rules.csproj",
    "Saffron.Rules.Tests": ROOT / "rules" / "Saffron.Rules.Tests" / "Saffron.Rules.Tests.csproj",
}
PRODUCTION_PROJECTS = ("Saffron.Api", "Saffron.Rules")
TEST_PROJECTS = ("Saffron.Api.Tests", "Saffron.Rules.Tests")
TEST_DLLS = {f"{name}.dll" for name in TEST_PROJECTS}
TEST_PACKAGES = {
    "Microsoft.NET.Test.Sdk": "17.11.1",
    "xunit": "2.9.2",
    "xunit.runner.visualstudio": "2.8.2",
}
CSHARP_PROJECT_TYPE = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"

LEGACY_PROJECT_FILES = [
    ROOT / "App" / "App.csproj",
    ROOT / "tests" / "App.Tests" / "App.Tests.csproj",
]
LEGACY_OUTPUT_DIRS = [
    ROOT / "App" / "bin",
    ROOT / "App" / "obj",
    ROOT / "tests" / "App.Tests" / "bin",
    ROOT / "tests" / "App.Tests" / "obj",
]
DELETED_BY_FR014 = {"App/App.csproj", "tests/App.Tests/App.Tests.csproj"}

EXCLUDED_TOP_LEVEL = {".referee", ".specify", ".claude", ".agents", ".gemini", "specs"}
EXEMPT_FROM_BASELINE = {"Saffron.sln", "CLAUDE.md"}
ALLOWED_NEW_ROOT_FILES = {"global.json"}

SCAFFOLD_ROOTS = [ROOT / "api", ROOT / "rules"]
GENERATED_DIR_NAMES = {"bin", "obj", ".vs", ".idea", "TestResults"}
ALLOWED_SCAFFOLD_SUFFIXES = {".cs", ".csproj", ".json", ".props", ".targets", ".editorconfig"}
TEXT_SUFFIXES = {".cs", ".csproj", ".json", ".props", ".targets", ".sln"}

API_FORBIDDEN_PATTERNS = [
    r"\.Map(Get|Post|Put|Delete|Patch|Methods|Controllers|Razor|Fallback|Group|ControllerRoute)?\s*\(",
    r"\[\s*(ApiController|Route|Http(Get|Post|Put|Delete|Patch))\b",
    r"\bclass\s+\w+Controller\b",
    r"\bDbContext\b|\bDbConnection\b|\bSqlConnection\b|\bMySqlConnection\b|\bNpgsqlConnection\b|\bSqliteConnection\b|\bDbCommand\b",
    r"\bAddSession\b|\bUseSession\b|\bISession\b|\.Session\b",
    r"\bResults\.|\bHttpContext\b|\bResponse\.|\.WriteAsync\s*\(",
    r"\.Run\s*\(\s*(async\b|\(|\w+\s*=>)",
]

RULES_FORBIDDEN_PATTERNS = [
    r"\b(if|for|foreach|while|switch|return)\s*\(",
    r"^\s*(public|internal|private|protected)\s+(static\s+|override\s+|virtual\s+|async\s+)*[\w<>\[\],?.]+\s+\w+\s*\(",
]

SKIP_PATTERNS = [r"\bSkip\s*=", r"\[\s*Ignore\b"]

SECRET_PATTERN = (
    r"(?i)(password|pwd\s*=|connectionstring|connection\s*string|server\s*=|"
    r"data\s+source\s*=|initial\s+catalog|user\s*id\s*=|api[_-]?key|secret|access[_-]?token)"
)

PROJECT_LINE_RE = re.compile(
    r'^Project\("\{([^}]*)\}"\)\s*=\s*"([^"]+)",\s*"([^"]+)",\s*"\{[^}]*\}"', re.MULTILINE
)
ERROR_COUNT_RE = re.compile(r"(\d+)\s+Error\(s\)")
SUMMARY_RE = re.compile(
    r"^\s*(Passed|Failed)!\s+-\s+Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+).*?-\s*(\S+\.dll)",
    re.MULTILINE,
)


def _run(args, cwd=ROOT):
    env = dict(
        os.environ,
        DOTNET_CLI_TELEMETRY_OPTOUT="1",
        DOTNET_NOLOGO="1",
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
    )
    proc = subprocess.run(
        args, cwd=cwd, env=env, capture_output=True, text=True, timeout=DOTNET_TIMEOUT_SECONDS
    )
    return proc.returncode, proc.stdout + proc.stderr


def _tail(text, lines=40):
    return "\n".join(text.splitlines()[-lines:])


def _files_under(root, suffixes=None):
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        if GENERATED_DIR_NAMES.intersection(path.relative_to(root).parts[:-1]):
            continue
        if suffixes is None or path.suffix.lower() in suffixes:
            yield path


def _pattern_hits(files, patterns):
    hits = []
    for path in files:
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        for lineno, line in enumerate(text.splitlines(), 1):
            if any(re.search(pattern, line) for pattern in patterns):
                hits.append(f"{path.relative_to(ROOT)}:{lineno}: {line.strip()}")
    return hits


def _sln_text():
    if not SLN.is_file():
        raise AssertionError("Saffron.sln is missing from the repository root")
    return SLN.read_text(encoding="utf-8-sig", errors="replace")


def _sln_projects():
    return [
        (type_guid.upper(), name, path.replace("/", "\\"))
        for type_guid, name, path in PROJECT_LINE_RE.findall(_sln_text())
    ]


def _win_relative(path):
    return path.relative_to(ROOT).as_posix().replace("/", "\\")


def _load_baseline():
    entries = {}
    for line in BASELINE_FILE.read_text(encoding="utf-8").splitlines():
        digest, rel = line.split("  ", 1)
        entries[rel] = digest
    return entries


def _sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _current_repo_files():
    found = set()
    for dirpath, dirnames, filenames in os.walk(ROOT):
        current = Path(dirpath)
        if current == ROOT:
            dirnames[:] = [d for d in dirnames if d not in EXCLUDED_TOP_LEVEL]
        dirnames[:] = [d for d in dirnames if d not in GENERATED_DIR_NAMES]
        for name in filenames:
            found.add((current / name).relative_to(ROOT).as_posix())
    return found


def _error_count(output):
    found = ERROR_COUNT_RE.findall(output)
    return int(found[-1]) if found else None


@functools.lru_cache(maxsize=None)
def _root_build(configuration):
    args = ["dotnet", "build"]
    if configuration:
        args += ["-c", configuration]
    return _run(args)


@functools.lru_cache(maxsize=None)
def _root_test():
    return _run(["dotnet", "test"])


@functools.lru_cache(maxsize=None)
def _msbuild_property(name, prop):
    code, out = _run(["dotnet", "msbuild", str(PROJECT_PATHS[name]), f"-getProperty:{prop}"])
    if code != 0:
        raise AssertionError(f"dotnet msbuild failed for {name} ({prop}):\n{_tail(out)}")
    lines = [line.strip() for line in out.splitlines() if line.strip()]
    return lines[-1] if lines else ""


def _referenced_projects(project):
    code, out = _run(["dotnet", "list", str(project), "reference"])
    if code != 0:
        raise AssertionError(f"dotnet list reference failed for {project}:\n{_tail(out)}")
    refs = set()
    for line in out.splitlines():
        entry = line.strip().replace("\\", "/")
        if entry.endswith(".csproj"):
            refs.add((project.parent / entry).resolve())
    return refs


def _package_references(project):
    refs = {}
    for element in ET.parse(project).getroot().iter("PackageReference"):
        version = element.get("Version")
        if version is None:
            child = element.find("Version")
            version = child.text.strip() if child is not None and child.text else None
        refs[element.get("Include")] = version
    return refs


class SolutionMembership(unittest.TestCase):
    """FR-001, SC-003: the root solution lists exactly the four target projects."""

    def test_sln_lists_exactly_the_four_projects(self):
        expected = sorted((name, _win_relative(path)) for name, path in PROJECT_PATHS.items())
        actual = sorted((name, path) for _, name, path in _sln_projects())
        self.assertEqual(actual, expected)

    def test_sln_has_no_solution_folders_or_nested_projects(self):
        for type_guid, name, path in _sln_projects():
            self.assertEqual(type_guid, CSHARP_PROJECT_TYPE, f"{name} at {path} is not a C# project")
        self.assertNotIn("NestedProjects", _sln_text())

    def test_dotnet_sln_list_shows_exactly_the_four_project_files(self):
        code, out = _run(["dotnet", "sln", SLN.name, "list"])
        self.assertEqual(code, 0, _tail(out))
        listed = sorted(
            line.strip().replace("\\", "/")
            for line in out.splitlines()
            if line.strip().endswith(".csproj")
        )
        expected = sorted(path.relative_to(ROOT).as_posix() for path in PROJECT_PATHS.values())
        self.assertEqual(listed, expected)


class ProjectSettings(unittest.TestCase):
    """FR-002, FR-006: .NET 8 target, nullable enabled, web vs class-library kinds."""

    def test_every_project_targets_net8(self):
        for name in PROJECT_PATHS:
            framework = _msbuild_property(name, "TargetFramework") or _msbuild_property(
                name, "TargetFrameworks"
            )
            self.assertEqual(framework, "net8.0", name)

    def test_every_project_enables_nullable_reference_types(self):
        for name in PROJECT_PATHS:
            self.assertEqual(_msbuild_property(name, "Nullable").lower(), "enable", name)

    def test_saffron_api_is_an_aspnet_core_web_project(self):
        self.assertEqual(_msbuild_property("Saffron.Api", "UsingMicrosoftNETSdkWeb").lower(), "true")

    def test_saffron_rules_is_a_class_library_and_not_a_web_project(self):
        self.assertIn(_msbuild_property("Saffron.Rules", "OutputType").lower(), {"library", ""})
        self.assertNotEqual(
            _msbuild_property("Saffron.Rules", "UsingMicrosoftNETSdkWeb").lower(), "true"
        )

    def test_test_projects_are_marked_as_test_projects(self):
        for name in TEST_PROJECTS:
            self.assertEqual(_msbuild_property(name, "IsTestProject").lower(), "true", name)


class PackageReferences(unittest.TestCase):
    """Plan, Technical Context: production projects use no packages; test projects use the three pinned xUnit packages."""

    def test_production_projects_have_no_package_references(self):
        for name in PRODUCTION_PROJECTS:
            self.assertEqual(_package_references(PROJECT_PATHS[name]), {}, name)

    def test_test_projects_reference_exactly_the_pinned_xunit_packages(self):
        for name in TEST_PROJECTS:
            self.assertEqual(_package_references(PROJECT_PATHS[name]), TEST_PACKAGES, name)


class ProjectReferences(unittest.TestCase):
    """FR-005: each test project refers to the project it tests."""

    def test_api_tests_reference_saffron_api(self):
        expected = PROJECT_PATHS["Saffron.Api"].resolve()
        self.assertIn(expected, _referenced_projects(PROJECT_PATHS["Saffron.Api.Tests"]))

    def test_rules_tests_reference_saffron_rules(self):
        expected = PROJECT_PATHS["Saffron.Rules"].resolve()
        self.assertIn(expected, _referenced_projects(PROJECT_PATHS["Saffron.Rules.Tests"]))


class LegacyRemoval(unittest.TestCase):
    """FR-011, FR-014: the single-application project files and their output are gone."""

    def test_single_application_project_files_are_deleted(self):
        for path in LEGACY_PROJECT_FILES:
            self.assertFalse(path.exists(), f"{path.relative_to(ROOT)} still exists")

    def test_single_application_output_directories_are_deleted(self):
        for path in LEGACY_OUTPUT_DIRS:
            self.assertFalse(path.exists(), f"{path.relative_to(ROOT)} still exists")


class BuildFromRoot(unittest.TestCase):
    """User Story 1, FR-003, SC-001: dotnet build from the repository root."""

    def test_default_build_reports_zero_errors(self):
        code, out = _root_build(None)
        self.assertEqual(code, 0, _tail(out))
        self.assertEqual(_error_count(out), 0, _tail(out))

    def test_every_project_produces_its_assembly(self):
        _root_build(None)
        for name, project in PROJECT_PATHS.items():
            dlls = list((project.parent / "bin" / "Debug").rglob(f"{name}.dll"))
            self.assertTrue(dlls, f"no {name}.dll built under {project.parent / 'bin' / 'Debug'}")

    def test_release_build_reports_zero_errors(self):
        code, out = _root_build("Release")
        self.assertEqual(code, 0, _tail(out))
        self.assertEqual(_error_count(out), 0, _tail(out))

    def test_repeat_build_without_changes_succeeds(self):
        _root_build(None)
        code, out = _run(["dotnet", "build"])
        self.assertEqual(code, 0, _tail(out))
        self.assertEqual(_error_count(out), 0, _tail(out))


class TestRunFromRoot(unittest.TestCase):
    """User Story 2, FR-004, FR-007, SC-002: dotnet test from the repository root."""

    def test_root_test_run_exits_successfully(self):
        code, out = _root_test()
        self.assertEqual(code, 0, _tail(out))

    def test_root_test_run_finds_tests(self):
        _, out = _root_test()
        self.assertNotIn("No test is available", out)
        self.assertNotIn("No tests found", out)

    def test_both_test_projects_run_exactly_once(self):
        _, out = _root_test()
        dlls = sorted(dll for _, _, _, _, _, dll in SUMMARY_RE.findall(out))
        self.assertEqual(dlls, sorted(TEST_DLLS), _tail(out))

    def test_each_test_project_has_a_passing_test_and_skips_none(self):
        _, out = _root_test()
        for verdict, failed, passed, skipped, total, dll in SUMMARY_RE.findall(out):
            with self.subTest(dll=dll):
                self.assertEqual(verdict, "Passed")
                self.assertEqual(int(failed), 0)
                self.assertEqual(int(skipped), 0)
                self.assertGreaterEqual(int(passed), 1)
                self.assertEqual(int(total), int(passed))

    def test_no_test_source_marks_a_test_as_skipped(self):
        for name in TEST_PROJECTS:
            files = list(_files_under(PROJECT_PATHS[name].parent, {".cs"}))
            with self.subTest(project=name):
                self.assertEqual(_pattern_hits(files, SKIP_PATTERNS), [])


class ScopeGuards(unittest.TestCase):
    """FR-008, FR-009, FR-010, FR-012, FR-013, SC-004: nothing out of scope is added or changed."""

    def test_scaffold_folders_hold_only_project_wiring_files(self):
        for root in SCAFFOLD_ROOTS:
            self.assertTrue(root.is_dir(), f"{root.relative_to(ROOT)} is missing")
            unexpected = [
                str(path.relative_to(ROOT))
                for path in _files_under(root)
                if path.suffix.lower() not in ALLOWED_SCAFFOLD_SUFFIXES
            ]
            self.assertEqual(unexpected, [], "files outside the scaffold allowlist")

    def test_api_declares_no_routes_controllers_data_access_or_session(self):
        files = list(_files_under(PROJECT_PATHS["Saffron.Api"].parent, {".cs"}))
        self.assertTrue(files, "no Saffron.Api source files found")
        self.assertEqual(_pattern_hits(files, API_FORBIDDEN_PATTERNS), [])

    def test_rules_declares_no_rule_logic(self):
        files = list(_files_under(PROJECT_PATHS["Saffron.Rules"].parent, {".cs"}))
        self.assertTrue(files, "no Saffron.Rules source files found")
        self.assertEqual(_pattern_hits(files, RULES_FORBIDDEN_PATTERNS), [])

    def test_no_secrets_or_connection_strings_in_scaffold_files(self):
        files = [SLN]
        for root in SCAFFOLD_ROOTS:
            files.extend(_files_under(root, TEXT_SUFFIXES))
        self.assertGreater(len(files), 1, "no scaffold files found to scan")
        self.assertEqual(_pattern_hits(files, [SECRET_PATTERN]), [])

    def test_global_json_if_present_does_not_pin_an_sdk_version(self):
        path = ROOT / "global.json"
        if not path.exists():
            return
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        self.assertIsNone((data.get("sdk") or {}).get("version"), "global.json pins an SDK version")

    def test_out_of_scope_files_are_unchanged_since_baseline(self):
        baseline = _load_baseline()
        self.assertTrue(baseline, "baseline.sha256 is empty")
        problems = []
        for rel, digest in sorted(baseline.items()):
            path = ROOT / rel
            if rel in DELETED_BY_FR014:
                if path.exists():
                    problems.append(f"{rel} must be deleted (FR-014)")
            elif rel in EXEMPT_FROM_BASELINE:
                continue
            elif not path.is_file():
                problems.append(f"{rel} was removed")
            elif _sha256(path) != digest:
                problems.append(f"{rel} was changed")
        self.assertEqual(problems, [])

    def test_nothing_out_of_scope_is_added_since_baseline(self):
        added = sorted(_current_repo_files() - set(_load_baseline()))
        unexpected = [
            rel
            for rel in added
            if rel not in ALLOWED_NEW_ROOT_FILES and not rel.startswith(("api/", "rules/"))
        ]
        self.assertEqual(unexpected, [], "files added outside api/ and rules/ (FR-009, SC-004)")


class HarnessSelfCheck(unittest.TestCase):
    """Guards the output parsers, so a parser bug cannot pass or fail a correct build."""

    def test_summary_lines_parse_passed_and_failed_verdicts(self):
        passed = (
            "Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, "
            "Duration: 12 ms - Saffron.Api.Tests.dll (net8.0)"
        )
        failed = (
            "Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, "
            "Duration: 3 ms - Saffron.Rules.Tests.dll (net8.0)"
        )
        self.assertEqual(
            SUMMARY_RE.findall(passed), [("Passed", "0", "2", "0", "2", "Saffron.Api.Tests.dll")]
        )
        self.assertEqual(
            SUMMARY_RE.findall(failed), [("Failed", "1", "0", "0", "1", "Saffron.Rules.Tests.dll")]
        )

    def test_error_count_parses_build_summary(self):
        self.assertEqual(_error_count("    0 Warning(s)\n    0 Error(s)\n"), 0)
        self.assertEqual(_error_count("    2 Error(s)"), 2)


if __name__ == "__main__":
    unittest.main()
