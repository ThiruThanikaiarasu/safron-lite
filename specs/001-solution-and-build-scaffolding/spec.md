# Feature Specification: Solution and build scaffolding

**Feature Branch**: `001-solution-and-build-scaffolding`

**Created**: 2026-11-17

**Status**: Draft

**Input**: User description: "Creates the .NET 8 solution with the Saffron.Api, Saffron.Rules and test projects so that dotnet build and dotnet test succeed from the repository root. It delivers project files and wiring only, and leaves out the Docker stack, the database scripts and all screen code. The legacy PHPUnit suite is not part of dotnet test and is carried by the care helpers specification."

**Context**: Specification 1 of the migration of the Saffron-lite application to C# on .NET 8 (ASP.NET Core). The operator's goal is to keep behaviour identical across the care screens, the Saffron.Api service, the Saffron.Rules library and the database code. This specification delivers no behaviour of its own, so it changes none of the legacy behaviour the migration must keep. It creates the empty frame that every later specification adds code to.

## Clarifications

### Session 2026-11-17

- Q: Should the single-application files already in the repository (App/App.csproj, tests/App.Tests/App.Tests.csproj and their bin and obj output) be deleted now, or kept outside the root solution? → A: Delete them. They aren’t legacy code.

- Q: Should the root solution be a new file, or should the existing Saffron.sln at the repository root be rewritten to list only the four projects? → A: Rewrite Saffron.sln to list the four projects.

- Q: Should the four projects sit in the legacy locations api/Saffron.Api, api/Saffron.Api.Tests, rules/Saffron.Rules and rules/Saffron.Rules.Tests, or somewhere else? → A: Yes, the legacy locations. The legacy Saffron.sln already lists exactly these four: api\Saffron.Api, api\Saffron.Api.Tests, rules\Saffron.Rules, rules\Saffron.Rules.Tests

- Q: Should the .NET SDK be pinned to one specific .NET 8 patch version with a global.json file, or should any .NET 8 SDK be accepted? → A: Accept any .NET 8 SDK. Legacy has no global.json, so this keeps parity.

- Q: Must the build also report zero warnings, or is zero errors the only pass condition for SC-001? → A: Zero errors only. That’s what the goal asks for.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build the whole solution from the repository root (Priority: P1)

A developer clones the repository, runs the build from the repository root, and gets a successful build of the Saffron.Api project, the Saffron.Rules project and both test projects.

**Why this priority**: Every later specification adds code to this solution. If the root build is broken, no later specification can be verified.

**Independent Test**: On a clean checkout with the required SDK installed, run the build from the repository root and confirm it reports zero errors.

**Acceptance Scenarios**:

1. **Given** a clean checkout with the .NET 8 SDK installed, **When** the build runs from the repository root, **Then** all four projects build and the build reports zero errors.
2. **Given** the solution has already been built, **When** the build runs again with no source changes, **Then** it succeeds again.

---

### User Story 2 - Run the test projects from the repository root (Priority: P1)

A developer runs the test command from the repository root and sees both test projects run and pass, with each test project able to reach the project it tests.

**Why this priority**: The constitution makes a passing `dotnet test` from the repository root a delivery requirement, and every later specification adds its tests to these projects.

**Independent Test**: After a successful build, run the test command from the repository root and confirm both test projects run, all their tests pass, and the command exits successfully.

**Acceptance Scenarios**:

1. **Given** the solution has built, **When** the test command runs from the repository root, **Then** the Saffron.Api test project and the Saffron.Rules test project both run, and every test in them passes.
2. **Given** the Saffron.Api test project, **When** it is inspected, **Then** it refers to the Saffron.Api project, and the Saffron.Rules test project refers to the Saffron.Rules project.
3. **Given** a test project that has no real tests yet, **When** the test command runs, **Then** that project still runs at least one passing test, so the run is not reported as "no tests found".

---

### User Story 3 - Solution contains only the target components (Priority: P2)

A maintainer opens the root solution and sees only the Saffron.Api and Saffron.Rules projects and their test projects. Nothing from the Docker stack, the database scripts, the screen code, the legacy PHPUnit suite or the earlier single-application layout is part of the solution.

**Why this priority**: The constitution records that the operator chose separate components over a single application. A solution with the wrong members would make later specifications and reviews measure the wrong thing.

**Independent Test**: List the projects in the root solution and compare them with the four expected names.

**Acceptance Scenarios**:

1. **Given** the root solution, **When** the maintainer lists its projects, **Then** exactly four projects appear: Saffron.Api, Saffron.Rules, Saffron.Api.Tests and Saffron.Rules.Tests.
2. **Given** this specification is implemented, **When** the repository is compared with its state before implementation, **Then** no Docker stack, database script, screen, view, static asset or PHPUnit file has been added or changed by this specification.

---

### Edge Cases

- What happens when a test project has no tests yet? It still runs at least one passing test, and no test is skipped or deleted later to make the run pass.
- What happens when the earlier single-application project files remain in the repository? They are deleted by this specification (FR-014), and the root solution does not reference them, so the root build and test commands never compile or run them.
- What happens when the build runs in Debug and in Release? Both configurations build with zero errors.
- What happens when nullable reference types are disabled in one project? Every project enables them, so the scaffolding cannot introduce nullable-unsafe code that later specifications would have to clean up.
- What happens when the command is run from a subdirectory rather than the repository root? That case is not required. The success criteria are defined for the repository root only.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The existing solution file at the repository root, Saffron.sln, MUST be rewritten to list exactly four projects: Saffron.Api at api/Saffron.Api, Saffron.Api.Tests at api/Saffron.Api.Tests, Saffron.Rules at rules/Saffron.Rules and Saffron.Rules.Tests at rules/Saffron.Rules.Tests. No other project may be listed.
- **FR-002**: Saffron.Api MUST be an ASP.NET Core project, Saffron.Rules MUST be a class library, and all four projects MUST target .NET 8. No .NET SDK patch version is pinned: any .NET 8 SDK is accepted, matching the legacy repository, which pins none.
- **FR-003**: Running the build from the repository root MUST complete with zero errors for all four projects.
- **FR-004**: Running the test command from the repository root MUST run both test projects and MUST exit successfully.
- **FR-005**: Saffron.Api.Tests MUST reference the Saffron.Api project, and Saffron.Rules.Tests MUST reference the Saffron.Rules project.
- **FR-006**: Every project MUST enable nullable reference types.
- **FR-007**: Each test project MUST contain at least one passing test, and no test in the solution may be skipped.
- **FR-008**: Saffron.Api MUST contain no endpoints, routes, controllers, data access, session handling or business rules. Saffron.Rules MUST contain no rule logic. All of these come from later specifications.
- **FR-009**: This specification MUST NOT add a Docker stack, a docker-compose file, database scripts, or any screen, view, template or static asset.
- **FR-010**: The legacy PHPUnit suite MUST NOT be part of the solution or of the test command. It is carried by the care helpers specification.
- **FR-011**: The root solution MUST NOT reference the single-application project or its test project from the earlier plan, because the constitution records that the single-application structure is not adopted.
- **FR-012**: Project and configuration files added by this specification MUST NOT contain secrets, credentials or connection strings.
- **FR-013**: This specification MUST NOT define any HTTP route, response body, status code or database write, so that no legacy behaviour is changed by it.
- **FR-014**: The single-application project files App/App.csproj and tests/App.Tests/App.Tests.csproj, together with their bin and obj output, MUST be deleted from the repository, because they are not legacy code.

### Key Entities

Not applicable. This specification involves no data.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a clean checkout with the required SDK installed, a single build command run from the repository root completes with zero errors and no manual steps. Build warnings do not affect this criterion.
- **SC-002**: A single test command run from the repository root executes both test projects, and 100% of their tests pass with none skipped.
- **SC-003**: The root solution lists exactly four projects, matching the names in FR-001.
- **SC-004**: After implementation, the count of HTTP routes, database scripts, screen or view files, and Docker files added by this specification is zero.
- **SC-005**: Each later specification in the migration can add its code to one of the four projects without changing the solution layout or the build and test commands.

## Assumptions

- The target platform is .NET 8 with ASP.NET Core, as declared by the operator's goal and recorded in constitution Principle II. This specification names it as a fixed constraint, not as a design choice.
- The test projects are named Saffron.Api.Tests and Saffron.Rules.Tests, following the legacy test locations recorded in the platform's approach document (api/Saffron.Api.Tests and rules/Saffron.Rules.Tests). Their directory locations are api/Saffron.Api.Tests and rules/Saffron.Rules.Tests, as the operator decided in the Clarifications; the unit-test framework is decided in planning.
- The earlier single-application plan in specs/001-saffron-csharp-migration is superseded for the solution layout, as the constitution records. Files from that plan already present in the repository (App/App.csproj, tests/App.Tests/App.Tests.csproj and their bin and obj output) are deleted by this specification, as the operator decided (FR-014).
- This specification has no dependencies. Specifications 2 to 31 of the roadmap depend on it, directly or indirectly.
- The developer has a .NET 8 SDK installed, of any patch release (FR-002). This specification does not install it.
- The Docker stack, database scripts, CodeIgniter shell, API startup, rules, data access, screens and the care helpers suite are out of scope. They belong to the other specifications in the roadmap.
