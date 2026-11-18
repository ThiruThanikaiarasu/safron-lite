# Quickstart: Solution and build scaffolding

Validates User Stories 1 to 3 and success criteria SC-001 to SC-005 on a clean checkout of this branch. Run every command from the repository root.

## Prerequisites

- A .NET 8 SDK of any patch release (`dotnet --list-sdks` shows an `8.0.x` entry).
- Network access to a NuGet feed for the first restore.

## 1. Build the solution (Story 1, SC-001)

```bash
dotnet build
```

Expected: the build covers `Saffron.Api`, `Saffron.Rules`, `Saffron.Api.Tests` and `Saffron.Rules.Tests`, and reports `0 Error(s)`. Warnings do not fail this check.

Run it a second time with no source changes. Expected: it succeeds again (Story 1, scenario 2).

## 2. Run the tests (Story 2, SC-002)

```bash
dotnet test
```

Expected: both test projects run. Each has one passing test (`WiringTests`), none skipped, and the command exits with status 0.

## 3. Confirm the solution members (Story 3, SC-003)

```bash
dotnet sln Saffron.sln list
```

Expected output lists exactly these four project paths:

```text
api/Saffron.Api/Saffron.Api.csproj
api/Saffron.Api.Tests/Saffron.Api.Tests.csproj
rules/Saffron.Rules/Saffron.Rules.csproj
rules/Saffron.Rules.Tests/Saffron.Rules.Tests.csproj
```

## 4. Confirm the deletions (FR-014)

Expected: `App/App.csproj` and `tests/App.Tests/App.Tests.csproj` do not exist. Their `bin/` and `obj/` output is gone.

## 5. Confirm nothing out of scope was added (FR-009, FR-013, SC-004)

Compare the tree with the state before this branch. Expected: no new route, controller, endpoint, database script, screen, view, static asset, Docker file or `docker-compose.yml` was added by this specification. The root `docker-compose.yml` and `App/Dockerfile` are unchanged from before this branch.

## 6. Confirm later specifications can extend the frame (SC-005)

Expected: each test project references its production project, so a later specification can add a type to `api/Saffron.Api` or `rules/Saffron.Rules` and a test to the matching test project, with no change to `Saffron.sln` or to the build and test commands.

## Known gaps

- `dotnet build` and `dotnet test` fail without access to a NuGet feed on a clean checkout. This is an environment condition, not a defect in the scaffolding.
- The root `docker-compose.yml` and `App/Dockerfile` cannot build after step 4 until the Docker stack specification replaces them. This is expected by the spec's scope.
