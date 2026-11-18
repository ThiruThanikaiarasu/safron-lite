# Tasks: Solution and build scaffolding

**Input**: Design documents from `specs/001-solution-and-build-scaffolding/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: The spec requires one passing wiring test per test project (FR-007) and a passing `dotnet test` from the repository root (FR-004). These wiring tests are deliverables of User Stories 2 and 3, so they appear as implementation tasks in the story phases. No additional TDD test-first tasks are requested.

**Organization**: Tasks are grouped by user story. Phase 2 creates the frame that every story verifies, so it blocks all stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task names the exact file or command it acts on

## Path Conventions

- Target solution: `Saffron.sln` at the repository root
- Api component: `api/Saffron.Api/`, `api/Saffron.Api.Tests/`
- Rules component: `rules/Saffron.Rules/`, `rules/Saffron.Rules.Tests/`
- Legacy single-application files deleted by FR-014: `App/App.csproj`, `tests/App.Tests/App.Tests.csproj`, and their `bin/` and `obj/` output

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Remove the single-application project files from the earlier plan so the root solution cannot reference them (FR-011, FR-014).

- [x] T001 [P] Delete the project file `App/App.csproj` from the repository
- [x] T002 [P] Delete the output directories `App/bin/` and `App/obj/` from the repository
- [x] T003 [P] Delete the project file `tests/App.Tests/App.Tests.csproj` from the repository
- [x] T004 [P] Delete the output directories `tests/App.Tests/bin/` and `tests/App.Tests/obj/` from the repository

Do NOT delete or edit the other files under `App/` and `tests/App.Tests/` (`*.cs`, `App/Dockerfile`, `App/Views/`, `App/wwwroot/`), nor `docker-compose.yml`. The plan's Structure Decision leaves them in place, and FR-009 keeps the Docker stack out of this specification.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the four project files and the root solution. Every user story builds or tests these projects, so no story can be verified until this phase is complete.

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T005 [P] Create `api/Saffron.Api/Saffron.Api.csproj` using `Microsoft.NET.Sdk.Web`, `<TargetFramework>net8.0</TargetFramework>` and `<Nullable>enable</Nullable>`, with no package references
- [x] T006 [P] Create `api/Saffron.Api/Program.cs` as a minimal host: `WebApplication.CreateBuilder(args).Build().Run();` with no endpoints, routes, controllers, data access, session handling or rules (FR-008)
- [x] T007 [P] Create `api/Saffron.Api/AssemblyMarker.cs` with `namespace Saffron.Api;` and an empty `public sealed class AssemblyMarker { }`
- [x] T008 [P] Create `rules/Saffron.Rules/Saffron.Rules.csproj` using `Microsoft.NET.Sdk`, `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, with no package references
- [x] T009 [P] Create `rules/Saffron.Rules/AssemblyMarker.cs` with `namespace Saffron.Rules;` and an empty `public sealed class AssemblyMarker { }`
- [x] T010 [P] Create `api/Saffron.Api.Tests/Saffron.Api.Tests.csproj` using `Microsoft.NET.Sdk`, `net8.0`, `<Nullable>enable</Nullable>`, `<IsTestProject>true</IsTestProject>`, package references `Microsoft.NET.Test.Sdk` 17.11.1, `xunit` 2.9.2, `xunit.runner.visualstudio` 2.8.2, and `<ProjectReference Include="../Saffron.Api/Saffron.Api.csproj" />`
- [x] T011 [P] Create `rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj` with the same SDK, target, nullable, package versions and `IsTestProject` as T010, and `<ProjectReference Include="../Saffron.Rules/Saffron.Rules.csproj" />`
- [x] T012 Rewrite the existing `Saffron.sln` at the repository root to list exactly four projects, with no solution folders and no `NestedProjects` section: `Saffron.Api` at `api\Saffron.Api\Saffron.Api.csproj`, `Saffron.Api.Tests` at `api\Saffron.Api.Tests\Saffron.Api.Tests.csproj`, `Saffron.Rules` at `rules\Saffron.Rules\Saffron.Rules.csproj`, and `Saffron.Rules.Tests` at `rules\Saffron.Rules.Tests\Saffron.Rules.Tests.csproj`. Keep the Visual Studio header, `Debug|Any CPU` and `Release|Any CPU` configurations, and a fresh GUID per project. Depends on T005 to T011.

**Checkpoint**: The frame exists. The four projects and the solution file are in place, and the user stories can now be verified.

---

## Phase 3: User Story 1 - Build the whole solution from the repository root (Priority: P1)

**Goal**: `dotnet build` from the repository root builds all four projects with zero errors, in both configurations, and repeats successfully.

**Independent Test**: On a clean checkout with a .NET 8 SDK, run `dotnet build` from the repository root and confirm `0 Error(s)`.

- [x] T013 [US1] Run `dotnet build` from the repository root (default Debug) against `Saffron.sln` and confirm all four projects build with `0 Error(s)`. If a project fails, fix the failing file among `api/Saffron.Api/Saffron.Api.csproj`, `api/Saffron.Api/Program.cs`, `api/Saffron.Api/AssemblyMarker.cs`, `rules/Saffron.Rules/Saffron.Rules.csproj`, `rules/Saffron.Rules/AssemblyMarker.cs`, `api/Saffron.Api.Tests/Saffron.Api.Tests.csproj` or `rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj`
- [ ] T014 [US1] Run `dotnet build -c Release` from the repository root and confirm `0 Error(s)` (edge case: Debug and Release both build). Runs after T013 to avoid concurrent writes to the same `obj/` folders
- [ ] T015 [US1] Run `dotnet build` from the repository root a second time with no source changes and confirm it succeeds again (User Story 1, scenario 2)

**Checkpoint**: User Story 1 is fully verified. The root build passes with zero errors in both configurations and on a repeat run.

---

## Phase 4: User Story 2 - Run the test projects from the repository root (Priority: P1)

**Goal**: `dotnet test` from the repository root runs both test projects, each with at least one passing test, none skipped, and exits successfully. Each test project references the project it tests.

**Independent Test**: After a successful build, run `dotnet test` from the repository root and confirm both test projects run, every test passes, and the exit status is 0.

- [ ] T016 [P] [US2] Create `api/Saffron.Api.Tests/WiringTests.cs` with one xUnit `[Fact]` that asserts `typeof(Saffron.Api.AssemblyMarker).Assembly.GetName().Name` equals `"Saffron.Api"`. Use `namespace Saffron.Api.Tests;`, `using Xunit;`, and no `[Skip]` attribute (FR-005, FR-007)
- [ ] T017 [P] [US2] Create `rules/Saffron.Rules.Tests/WiringTests.cs` with one xUnit `[Fact]` that asserts `typeof(Saffron.Rules.AssemblyMarker).Assembly.GetName().Name` equals `"Saffron.Rules"`. Use `namespace Saffron.Rules.Tests;`, `using Xunit;`, and no `[Skip]` attribute (FR-005, FR-007)
- [ ] T018 [US2] Run `dotnet test` from the repository root and confirm both test projects run, each reports at least one passing test, none is skipped, and the command exits with status 0. Depends on T016 and T017
- [ ] T019 [P] [US2] Confirm the project references: `dotnet list api/Saffron.Api.Tests/Saffron.Api.Tests.csproj reference` shows `Saffron.Api.csproj`, and `dotnet list rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj reference` shows `Saffron.Rules.csproj` (User Story 2, scenario 2)

**Checkpoint**: User Story 2 is fully verified. Both test projects run from the root, pass, skip nothing, and reach the projects they test.

---

## Phase 5: User Story 3 - Solution contains only the target components (Priority: P2)

**Goal**: The root solution lists exactly the four target projects. No Docker, database, screen, PHPUnit or legacy single-application member is in the solution, and this specification adds no route, script, screen or Docker file.

**Independent Test**: List the projects in the root solution and compare them with the four expected names.

- [ ] T020 [US3] Run `dotnet sln Saffron.sln list` from the repository root and confirm exactly these four lines appear: `api/Saffron.Api/Saffron.Api.csproj`, `api/Saffron.Api.Tests/Saffron.Api.Tests.csproj`, `rules/Saffron.Rules/Saffron.Rules.csproj`, `rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj` (SC-003)
- [ ] T021 [P] [US3] Inspect `api/Saffron.Api/Program.cs` and `api/Saffron.Api/AssemblyMarker.cs` and confirm they contain no `MapGet`/`MapPost`/route attributes, controllers, data access, session handling or rule logic (FR-008, FR-013). Inspect `rules/Saffron.Rules/AssemblyMarker.cs` and confirm it holds no rule logic (FR-008)
- [ ] T022 [P] [US3] Confirm that this specification added no Docker stack, database script, screen, view, static asset or PHPUnit file. Compare the repository against the pre-implementation snapshot, or against the plan's Structure section if no snapshot exists. Check `docker-compose.yml`, `App/Dockerfile`, `db/`, `proxy/`, `App/Views/`, `App/wwwroot/` and `tests/App.Tests/*.cs` remain unchanged (FR-009, FR-010, SC-004)

**Checkpoint**: User Story 3 is fully verified. The solution has exactly four members, and no out-of-scope file was added.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story checks and end-to-end validation.

- [ ] T023 [P] Confirm no `global.json` exists at the repository root, since the spec pins no SDK patch (FR-002)
- [ ] T024 [P] Confirm `<Nullable>enable</Nullable>` is set in all four project files (`api/Saffron.Api/Saffron.Api.csproj`, `rules/Saffron.Rules/Saffron.Rules.csproj`, `api/Saffron.Api.Tests/Saffron.Api.Tests.csproj`, `rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj`), and that no connection string, password or secret appears in any file created by this specification (FR-006, FR-012)
- [ ] T025 Run every step of `specs/001-solution-and-build-scaffolding/quickstart.md` in order from the repository root and record the outcome of each step

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies. Can start immediately.
- **Foundational (Phase 2)**: Depends on Setup. T012 (solution) depends on T005 to T011. Blocks all user stories.
- **User Stories (Phases 3 to 5)**: All depend on Foundational. US1 and US2 are both P1; US3 is P2.
- **Polish (Phase N)**: Depends on all user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2. No dependency on other stories.
- **User Story 2 (P1)**: Starts after Phase 2. T018 runs after T016 and T017. Its test run uses a successful build, so it is best run after US1's T013.
- **User Story 3 (P2)**: Starts after Phase 2. Independent of US1 and US2 except that T020 needs the solution file from T012.

### Within Each Phase

- Project files and their marker files can be written in parallel (different files).
- Build and test commands (T013 to T015, T018) run sequentially, because they share the same `obj/` and `bin/` folders.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Write the four project files and two marker files together (different files):
Task: "Create api/Saffron.Api/Saffron.Api.csproj"
Task: "Create api/Saffron.Api/Program.cs"
Task: "Create api/Saffron.Api/AssemblyMarker.cs"
Task: "Create rules/Saffron.Rules/Saffron.Rules.csproj"
Task: "Create rules/Saffron.Rules/AssemblyMarker.cs"
Task: "Create api/Saffron.Api.Tests/Saffron.Api.Tests.csproj"
Task: "Create rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj"
# Then, once all seven exist:
Task: "Rewrite Saffron.sln"
```

## Parallel Example: User Story 2

```bash
Task: "Create api/Saffron.Api.Tests/WiringTests.cs"
Task: "Create rules/Saffron.Rules.Tests/WiringTests.cs"
# Then:
Task: "dotnet test from the repository root"
```

---

## Implementation Strategy

### MVP First (User Stories 1 and 2)

1. Complete Phase 1: Setup (delete the legacy project files)
2. Complete Phase 2: Foundational (create the frame and the solution)
3. Complete Phase 3: User Story 1 (root build passes)
4. Complete Phase 4: User Story 2 (root test passes)
5. **STOP and VALIDATE**: both P1 stories are checked. The constitution makes `dotnet test` from the root a delivery requirement, so the MVP is US1 plus US2, not US1 alone.

### Incremental Delivery

1. Setup + Foundational: frame in place
2. Add User Story 1: root build verified
3. Add User Story 2: root test verified (MVP)
4. Add User Story 3: solution membership and scope verified
5. Polish: global.json, nullable and secret checks, quickstart run

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps each task to a user story for traceability
- Each user story is independently verifiable after Phase 2
- No behaviour, route, response body, status code, database write, Docker file, script or screen is produced by any task (FR-013)
- Do NOT change `Saffron.sln` membership to include the legacy PHPUnit suite, the Docker stack or the earlier single-application project (FR-010, FR-011)
- Commit after each task or logical group, if committing is requested
