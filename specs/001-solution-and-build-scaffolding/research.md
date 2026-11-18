# Research: Solution and build scaffolding

No NEEDS CLARIFICATION markers remain in the plan. The spec left one open item for planning (the unit-test framework); the others were settled in the spec Clarifications or by the constitution.

## Unit-test framework

- **Decision**: xUnit 2.9.2 with Microsoft.NET.Test.Sdk 17.11.1 and xunit.runner.visualstudio 2.8.2.
- **Rationale**: The earlier single-application test project (`tests/App.Tests`) already uses these versions on net8.0, so the migration keeps one framework and one set of versions. The `dotnet test` runner integrates with xUnit out of the box.
- **Alternatives considered**: NUnit and MSTest. Both work with `dotnet test`, but neither is used by the existing test project, so choosing either would add a second framework with no benefit.

## Target framework and SDK

- **Decision**: `net8.0` for all four projects. No `global.json`.
- **Rationale**: FR-002 requires .NET 8. Spec Clarifications accept any .NET 8 SDK, and the legacy repository pins none.
- **Alternatives considered**: Pinning one SDK patch with `global.json`. Rejected because the spec explicitly chooses parity with the legacy repository.

## Project SDK per component

- **Decision**: `Saffron.Api` uses `Microsoft.NET.Sdk.Web`. `Saffron.Rules` and both test projects use `Microsoft.NET.Sdk`.
- **Rationale**: The spec requires `Saffron.Api` to be an ASP.NET Core project (FR-002) and `Saffron.Rules` to be a class library. A Web SDK project needs an entry point, which is why `Program.cs` exists. That entry point is minimal and defines no endpoints (FR-008).
- **Alternatives considered**: Using a plain `Microsoft.NET.Sdk` class library for `Saffron.Api` and adding ASP.NET Core packages later. Rejected because it contradicts FR-002.

## Test wiring proof

- **Decision**: Each production project holds one empty `public` marker type. Each test project holds one xUnit test asserting that the marker's assembly name equals the project name.
- **Rationale**: The spec requires at least one passing test per project, with no skipping (FR-007), and proof that each test project reaches the project it tests (FR-005, Story 2). A test on the marker's assembly name fails if the project reference is removed, and it needs no rule or endpoint code.
- **Alternatives considered**: Testing `Program` through `InternalsVisibleTo`, or using `WebApplicationFactory`. Both pull in endpoint and host behaviour that the spec places in later specifications.

## Solution file

- **Decision**: Rewrite the existing `Saffron.sln` (format version 12.00) to list the four projects with no solution folders.
- **Rationale**: FR-001 requires the root solution file to be rewritten, not replaced. Omitting solution folders keeps the project count at exactly four (SC-003), with no nested "tests" folder to argue about.
- **Alternatives considered**: A new `.sln` file beside the old one. Rejected by the spec Clarifications, and it would leave two solution files at the root, which makes `dotnet build` ambiguous.

## Deletion scope

- **Decision**: Delete `App/App.csproj` and `tests/App.Tests/App.Tests.csproj` with their bin and obj output. Leave other files in place.
- **Rationale**: FR-014 names only these files. The spec's Clarifications state these files are not legacy code, so they may be removed. Other sources are not named for deletion, and removing them would go beyond the spec.
- **Alternatives considered**: Removing the whole `App/` and `tests/App.Tests/` trees. Rejected as beyond the named scope; the remaining sources are not compiled by the root solution.
