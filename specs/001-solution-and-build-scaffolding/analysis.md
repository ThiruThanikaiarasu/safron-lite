# Specification Analysis Report: Solution and build scaffolding

Read-only analysis of spec.md, plan.md and tasks.md against constitution v1.0.1 and the operator's declared goal (php to csharp, .NET 8). Supporting files also read: research.md, data-model.md, quickstart.md, checklists/requirements.md, ../../old/docs/roadmap.md, and the workspace files the plan names (Saffron.sln, App/, tests/App.Tests/, App/Dockerfile, docker-compose.yml). No [NEEDS CLARIFICATION] markers exist in spec.md, plan.md or tasks.md.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| C1 | Constitution Alignment | CRITICAL | plan.md:45, plan.md:111; spec.md:100; App/Dockerfile:4-5; constitution.md:57-58, 84-85 | The plan's Constitution Check marks Principle V PASS. Deleting App/App.csproj (FR-014) leaves the root docker-compose.yml unable to build, because App/Dockerfile copies App/ and runs `dotnet publish App/App.csproj`. Principle V requires that compose file to serve port 8095. The break is recorded only in plan Risks, not as a deviation. | Record an interim deviation with its reason in the Principle V row, and put the operator question below to the operator before implementation. |
| I1 | Inconsistency | MEDIUM | spec.md:21; Saffron.sln:6, 10, 30 | Clarification 3 says the legacy Saffron.sln "already lists exactly these four" projects. The file lists App\App.csproj and tests\App.Tests\App.Tests.csproj, with a NestedProjects section. plan.md:113 notes the mismatch, but the spec was not corrected. | Correct Clarification 3 to record the actual listing. The rewrite decision in FR-001 stands. |
| I2 | Inconsistency | MEDIUM | spec.md:121; ../../old/docs/roadmap.md:59, 67, 75, 155, 171, 211, 219, 251 | The Assumption says specs 2 to 31 depend on spec 1, directly or indirectly. The roadmap gives no chain back to spec 1 for specs 004, 005, 006, 016, 018, 023, 024 and 028. | Rewrite the Assumption to name the real dependents, or state that these eight specs do not depend on spec 1. |
| U1 | Underspecification | MEDIUM | spec.md:113; tasks.md:95; repository is not a git repository | SC-004 and T022 compare the repository with a "pre-implementation snapshot", but no task captures a baseline and there is no git history. The fallback, the plan's Structure section, cannot show that Docker, database and screen files are unchanged. | Add a baseline task before T001 that records the file list and hashes, excluding App/bin, App/obj, tests/App.Tests/bin and tests/App.Tests/obj. |
| C2 | Constitution Alignment | LOW | constitution.md:55-56; plan.md:45 | Principle V's fail-fast requirement (refuse to start when the catalog connection string or Redis endpoint is missing) is not recorded as deferred. The scaffolding has no settings yet, so nothing is violated. | Note in the Principle V row that fail-fast is deferred to specs 003 and 009. |
| E1 | Coverage Gap | LOW | spec.md:114; tasks.md:81, 107 | SC-005 (later specs add code without changing layout or commands) has no dedicated check. It is covered only indirectly by T019 and T025. | Name the check in T025: each test project references its production project, as quickstart step 6 describes. |
| I3 | Inconsistency | LOW | checklists/requirements.md:35; spec.md:17 | The checklist says disposition of the single-application files is "left to the operator and the planning step". Clarification 1 already decided deletion. | Update the checklist note. |
| D1 | Duplication | LOW | spec.md:78; spec.md:97, 100 | The edge case restates FR-011 and FR-014. No execution effect. | Keep the requirements; the edge-case restatement can be dropped. |

## Coverage Summary

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 | Yes | T012, T020 | |
| FR-002 | Yes | T005, T008, T010, T011, T023 | |
| FR-003 | Yes | T013, T014, T015 | |
| FR-004 | Yes | T018 | |
| FR-005 | Yes | T010, T011, T016, T017, T019 | |
| FR-006 | Yes | T005, T008, T010, T011, T024 | |
| FR-007 | Yes | T016, T017, T018 | |
| FR-008 | Yes | T006, T021 | |
| FR-009 | Yes | T022 | Depends on U1 |
| FR-010 | Yes | T020, T022 | |
| FR-011 | Yes | T012, T020 | |
| FR-012 | Yes | T024 | |
| FR-013 | Yes | T021 | |
| FR-014 | Yes | T001, T002, T003, T004 | Produces C1 |
| SC-001 | Yes | T013, T014, T015 | |
| SC-002 | Yes | T018 | |
| SC-003 | Yes | T020 | |
| SC-004 | Yes | T022 | Depends on U1 |
| SC-005 | Partial | T019, T025 | No dedicated check (E1) |

## Constitution Alignment Issues

- **C1 (CRITICAL)**: Principle V and the operator's goal both require the root docker-compose.yml to serve the application on port 8095. After FR-014, that compose file cannot build until specification 031 replaces it. The Development Workflow requires each plan deviation to be recorded with its reason, and the Principle V row records none. The constitution treats conflicts as CRITICAL, so this is reported as CRITICAL even though the final-state requirement is delivered by specification 031.

  [NEEDS CLARIFICATION: the goal says the root docker-compose.yml must serve the application on port 8095, but plan.md says the root compose cannot build after the FR-014 deletion until specification 031 replaces it — which one wins for the interim state?]

- Principles I to IV: no conflict found. The spec defines no route, body, status code or database write (FR-013). Saffron.Api uses the Web SDK and all projects target net8.0 (Principle II). Wiring tests are the only tests and no rule exists yet (Principle III). Nullable reference types are enabled everywhere and no secrets are added (Principle IV).
- Structure: Saffron.Api and Saffron.Rules are separate components, as the constitution's Technology Constraints require.

## Unmapped Tasks

None. All 25 tasks (T001 to T025) map to at least one requirement or user story.

## Checks Run

- .NET SDK 8.0.428 is installed, matching the plan.
- The xUnit, Microsoft.NET.Test.Sdk and xunit.runner.visualstudio versions in the plan match tests/App.Tests/App.Tests.csproj.
- api/ and rules/ do not exist, so the Create tasks will not overwrite existing files.
- No global.json exists, matching FR-002.

## Metrics

- Total requirements: 19 (FR-001 to FR-014, SC-001 to SC-005)
- Total tasks: 25
- Coverage: 100% of requirements have at least one task (SC-005 only indirectly, E1)
- Ambiguity count: 0
- Duplication count: 1 (D1)
- Critical issues count: 1 (C1)

## Next Actions

- Resolve C1 with the operator before `/speckit-implement`. The question above needs a decision on the interim state of the root compose file.
- Correct I1 and I2 in spec.md (Clarification 3 and the dependency Assumption). Use `/speckit-clarify` or a manual edit, then re-run `/speckit-plan` if the plan's Risks change.
- Add the baseline task for U1 to tasks.md before T001.
- E1, I3 and D1 are optional polish.
- Re-run `/speckit-analyze` after these changes.
