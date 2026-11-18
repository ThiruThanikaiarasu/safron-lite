# Implementation Plan: Solution and build scaffolding

**Branch**: `001-solution-and-build-scaffolding` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-solution-and-build-scaffolding/spec.md`

## Summary

Create the empty .NET 8 frame that every later specification adds code to: a root solution `Saffron.sln` listing exactly four projects (`Saffron.Api` as an ASP.NET Core web project, `Saffron.Rules` as a class library, and their two xUnit test projects), with project references from each test project to the project it tests. `dotnet build` and `dotnet test` run from the repository root and succeed. The single-application project files from the earlier plan (`App/App.csproj`, `tests/App.Tests/App.Tests.csproj`, and their bin/obj output) are deleted. No behaviour, route, response, database write, Docker file, script or screen is introduced, so no legacy behaviour is changed by this specification.

Research decisions are in [research.md](research.md). The spec has no entities, so [data-model.md](data-model.md) records that none apply. No external interface is defined here, so no `contracts/` directory is produced. [quickstart.md](quickstart.md) gives the validation run.

## Technical Context

**Language/Version**: C# on .NET 8 (`net8.0`), built with any .NET 8 SDK patch release. No `global.json` is added (spec FR-002). The SDK available in this environment is 8.0.428.

**Primary Dependencies**: ASP.NET Core via the `Microsoft.NET.Sdk.Web` SDK for `Saffron.Api`; no framework dependency for `Saffron.Rules`. Test projects use xUnit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1 and xunit.runner.visualstudio 2.8.2, the same versions as the earlier `tests/App.Tests` project. No other packages are added.

**Storage**: N/A. This specification involves no data (spec Key Entities).

**Testing**: xUnit, run with `dotnet test` from the repository root. Each test project holds one wiring test that proves its project reference (FR-005, FR-007).

**Target Platform**: Any host with a .NET 8 SDK for build and test. Runtime targets ASP.NET Core on .NET 8.

**Project Type**: Multi-project solution: one ASP.NET Core service, one class library, two test projects.

**Performance Goals**: N/A for this specification. Success is defined by zero build errors and passing tests (SC-001, SC-002).

**Constraints**: Nullable reference types enabled in every project (FR-006). Zero build errors; warnings do not count (spec Clarifications). No secrets, credentials or connection strings in any file (FR-012). No endpoints, data access, rule logic, Docker, DB script or screen file (FR-008, FR-009, FR-013).

**Scale/Scope**: Exactly four projects in the root solution (FR-001, SC-003). Each later specification adds code to one of these four projects without changing the solution layout (SC-005).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against `.specify/memory/constitution.md` (version 1.0.1) and the operator's declared goal.

| Principle | Check | Result |
|---|---|---|
| I. Behavioural Parity | The spec defines no route, body, status code, database write or screen (FR-013). Nothing is added that changes legacy behaviour. | PASS |
| II. C# on .NET 8 (ASP.NET Core) | `Saffron.Api` uses `Microsoft.NET.Sdk.Web`; `Saffron.Rules` and the test projects target `net8.0`. No PHP or CodeIgniter file is added. | PASS |
| III. Unit and Integration Testing | Both test projects are added and run from the root through `dotnet test`. Wiring tests pass and are not skipped. No rule exists yet, so no rule tests are required in this specification. | PASS |
| IV. Common Good Practice | Nullable reference types are enabled in all four projects. No secrets are committed. Async I/O and DI are not yet in use because no code is written. | PASS |
| V. Fail-Fast Configuration and Reproducible Delivery | `dotnet build` succeeds from the root (SC-001). The root `docker-compose.yml` is not touched by this specification (FR-009). The existing one is discussed under Risks. | PASS, with the Risks noted |
| Technology Constraints (structure) | `Saffron.Api` (in `api/`) and `Saffron.Rules` (in `rules/`) are separate components. The single-application structure is not adopted, and its project files are deleted (FR-011, FR-014). | PASS |
| Development Workflow (spec-first, parity traced) | Work follows spec, plan, tasks, implement. Scaffolding has no legacy behaviour to trace, so the parity review in the workflow has nothing to check beyond FR-013. | PASS |
| Operator's declared goal (C# on .NET 8, build and test from root, same routes and behaviour) | This specification serves the goal by creating the frame the rest of the migration depends on. No document in the workspace contradicts the goal for this specification's scope. | PASS |

**Gate result**: PASS. No violation needs justification, so Complexity Tracking stays empty.

**Post-design re-check (after Phase 1)**: PASS. The design adds nothing beyond the four projects, the solution file, the deletions, and the three planning artifacts.

## Project Structure

### Documentation (this feature)

```text
specs/001-solution-and-build-scaffolding/
├── spec.md
├── plan.md                  # This file (/speckit-plan output)
├── research.md              # Phase 0 output
├── data-model.md            # Phase 1 output (no entities)
├── quickstart.md            # Phase 1 output
└── tasks.md                 # Produced by /speckit-tasks, not by this plan
```

`contracts/` is not created. The spec defines no interface (FR-013), so there is nothing to contract.

### Source Code (repository root)

```text
Saffron.sln                              # rewritten: lists exactly the four projects below (FR-001)

api/
├── Saffron.Api/
│   ├── Saffron.Api.csproj               # Microsoft.NET.Sdk.Web, net8.0, Nullable enable
│   ├── Program.cs                       # minimal host: WebApplication.CreateBuilder(args).Build().Run(); no endpoints
│   └── AssemblyMarker.cs                # empty public marker type for the wiring test
└── Saffron.Api.Tests/
    ├── Saffron.Api.Tests.csproj         # Microsoft.NET.Sdk, xUnit, ProjectReference to ..\Saffron.Api\Saffron.Api.csproj
    └── WiringTests.cs                   # one passing test: referenced assembly is Saffron.Api

rules/
├── Saffron.Rules/
│   ├── Saffron.Rules.csproj             # Microsoft.NET.Sdk, net8.0, Nullable enable
│   └── AssemblyMarker.cs                # empty public marker type for the wiring test
└── Saffron.Rules.Tests/
    ├── Saffron.Rules.Tests.csproj       # Microsoft.NET.Sdk, xUnit, ProjectReference to ..\Saffron.Rules\Saffron.Rules.csproj
    └── WiringTests.cs                   # one passing test: referenced assembly is Saffron.Rules

# Deleted by this specification (FR-014), including bin/ and obj/ output:
App/App.csproj
tests/App.Tests/App.Tests.csproj
```

**Structure Decision**: Four projects in the legacy-location directories named in the spec Clarifications (`api/Saffron.Api`, `api/Saffron.Api.Tests`, `rules/Saffron.Rules`, `rules/Saffron.Rules.Tests`). `Saffron.sln` lists these four and nothing else; no solution folders are added so the list is unambiguous (SC-003). Each production project holds one empty marker type. The wiring test asserts on that type's assembly name, so the test fails if the project reference is removed (FR-005). Later specifications may replace the marker when real types arrive.

Only the two named project files and their bin/obj output are deleted. The `.cs` sources under `App/` and `tests/App.Tests/`, `App/Dockerfile`, and the root `docker-compose.yml` are left in place, because the spec names only the project files for deletion and the Docker stack is out of scope (FR-009). The root solution never compiles them.

## Complexity Tracking

> No Constitution Check violations. Nothing to justify.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | | |

## Risks and Open Items

- **Stale Docker files.** The root `docker-compose.yml` and `App/Dockerfile` build from `App/App.csproj` (`dotnet publish App/App.csproj`, `COPY App/ App/`). After FR-014 deletes that project file, `docker compose build` fails until the Docker stack specification replaces them. This is expected by the spec's scope (FR-009) and is not a conflict with the operator's goal, which is delivered by that later specification.
- **Leftover single-application sources.** `App/**/*.cs` and `tests/App.Tests/*.cs` remain after deletion (see Structure Decision). They are not compiled by the root solution. Their removal or migration belongs to later specifications.
- **Legacy solution wording.** The spec's Clarifications say the legacy `Saffron.sln` already lists `api\Saffron.Api`, `api\Saffron.Api.Tests`, `rules\Saffron.Rules` and `rules\Saffron.Rules.Tests`. The `Saffron.sln` in this workspace lists `App\App.csproj` and `tests\App.Tests\App.Tests.csproj` instead. This plan follows the spec's directory decisions and rewrites `Saffron.sln` to the four projects, as FR-001 requires. No goal conflict results.
- **NuGet restore.** Restoring xUnit and the test SDK needs access to a NuGet feed. This is a condition of the environment, not a design choice. A clean checkout without feed access cannot run `dotnet test`.
