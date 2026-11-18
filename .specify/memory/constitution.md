<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.0.1 (PATCH: operator answers settle the database engine and the
  component structure; no principle changed)
- Adopted as initial constitution for the Saffron-lite migration to C# on .NET 8.
- Principles added at adoption: I. Behavioural Parity (NON-NEGOTIABLE); II. C# on .NET 8 (ASP.NET
  Core); III. Unit and Integration Testing; IV. Common Good Practice; V. Fail-Fast Configuration and
  Reproducible Delivery.
- Sections added at adoption: Technology Constraints; Development Workflow; Governance.
- Sections modified in 1.0.1: Technology Constraints (database engine and structure rules).
- Sections removed: none.
- Deferred items: none.
-->

# Saffron-lite C# Migration Constitution

## Core Principles

### I. Behavioural Parity (NON-NEGOTIABLE)

The migrated system MUST keep the legacy behaviour identical for every facility group. This covers
the screens and what they show, the HTTP routes, request and response bodies, status codes, the data
written to the database, and the development logins and seed data. Legacy bugs MUST NOT be fixed.
Behaviour that looks wrong is carried over unchanged and MAY be recorded in a follow-up list.
Rationale: the operator declared a rebuild, not a redesign; any silent behaviour change breaks
consumers the migration does not own.

### II. C# on .NET 8 (ASP.NET Core)

Every migrated component MUST be written in C# targeting .NET 8 on ASP.NET Core. This covers the care
screens (previously PHP, CodeIgniter and jQuery), the Saffron.Api service, the Saffron.Rules rate
calculation library, and the SQL Server data access. PHP and CodeIgniter code MUST NOT be part of the
result. Client-side screen behaviour MUST match the legacy scripts, and markup MUST follow the legacy
templates.

### III. Unit and Integration Testing

Every migrated rule, helper and calculation MUST have unit tests. Rounding and time-handling rules
MUST have tests that pin the legacy results, including the difference between PHP `round()` in the
screens and .NET `Math.Round` in the API. Every HTTP route and every path that writes to the database
MUST have an integration test that checks the status code, the response body and the stored data.
`dotnet test` from the repository root MUST pass. Tests MUST NOT be deleted, skipped or weakened to
make a build pass.

### IV. Common Good Practice

Code MUST follow mainstream C# and .NET conventions: nullable reference types enabled, asynchronous
I/O, dependency injection for services, and configuration supplied from the environment. Secrets MUST
NOT be committed to source. Data access MUST use parameters, never string-built SQL. Where good
practice would change observable behaviour, behavioural parity under Principle I takes precedence
and the conflict MUST be recorded.

### V. Fail-Fast Configuration and Reproducible Delivery

The application MUST refuse to start when a required setting is missing, including the catalog
connection string and the Redis endpoint. `dotnet build` MUST succeed from the repository root.
A `docker-compose.yml` at the repository root MUST serve the application on port 8095 the same way
the legacy compose file does: `/care/*` for the screens and `/api/*` for the API. The facility group
MUST be chosen by host name for the screens and by the `X-Saffron-Tenant` header for the API.

## Technology Constraints

- Runtime and tooling: .NET 8 SDK and ASP.NET Core. Builds and tests run from the repository root with
  `dotnet build` and `dotnet test`.
- Database: SQL Server. The operator chose SQL Server over the PostgreSQL engine and Npgsql provider
  recorded in specs/001-saffron-csharp-migration/spec.md; PostgreSQL and Npgsql MUST NOT be used. The
  scripts under `db/` (schema, stored procedures, seed scripts and init) define the schema and the
  data written, and are called by name.
- Session: the login session is shared between the care screens and the API through Redis, as in the
  legacy system.
- Structure: the Saffron.Api service in api/ and the Saffron.Rules library in rules/ MUST be migrated
  as separate components, as the operator chose; the single ASP.NET Core application recorded in
  specs/001-saffron-csharp-migration/spec.md is not adopted.
- Ingress: the front proxy listens on port 8095 and routes `/care/*` and `/api/*`.

## Development Workflow

- Work is spec-first: specify, plan, tasks, then implement. Each migrated screen, route, rule and
  table maps to an item in the spec or tasks.
- Every change MUST trace to a legacy source file, route, screen or rule. Reviewers MUST check parity
  against the legacy documentation under `old/docs/` before accepting a change.
- Migration agents MUST NOT push, merge or deploy, and MUST NOT decide whether a change is accepted.
  Acceptance rests with the referee and the platform gates.
- Each plan MUST include a Constitution Check that confirms Principles I to V, or records each
  deviation with its reason.

## Governance

- This constitution supersedes other practices in this repository. Specs, plans, tasks and code MUST
  comply with it. Where a document conflicts with it, the conflict MUST be recorded in that document
  as a [NEEDS CLARIFICATION] marker until the operator decides.
- Amendments require the operator's approval, a Sync Impact Report at the top of this file, and an
  update to any dependent spec, plan or task that the amendment affects.
- Versioning follows semantic versioning. MAJOR: removal or redefinition of a principle or of the
  operator's declared goal. MINOR: a new principle or section, or materially expanded guidance.
  PATCH: wording, clarification or typo fixes.
- Compliance is reviewed at every plan and at every acceptance gate. Runtime guidance for agents is
  in `CLAUDE.md` at the repository root.

**Version**: 1.0.1 | **Ratified**: 2026-11-17 | **Last Amended**: 2026-11-17
