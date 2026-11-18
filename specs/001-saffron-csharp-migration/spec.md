# Feature: Saffron-lite migration to C# / ASP.NET Core (.NET 8)

## Goal

Migrate the Saffron-lite application at /synthetix/old/code/saffron-lite-20261116194251 to a single
ASP.NET Core (.NET 8) application that keeps behaviour identical: the same care screens and what they
show, the same HTTP routes, request and response bodies and status codes, the same data written to the
database, and the same behaviour for every facility group. Legacy bugs are not fixed.

Source estate (derived only from the source files; the platform docs under /synthetix/old/docs are
UNAVAILABLE for api_surface, data_model, screens, security, edge_cases and workflows):

- care/ : PHP 7.4 / CodeIgniter 3 care screens (login, patient list, patient detail, glucose entry)
- api/ : ASP.NET Core Saffron.Api (health, status, patients, glucose, orders)
- rules/ : Saffron.Rules infusion rate calculation
- db/ : SQL Server schema, stored procedures, seed scripts, init script
- proxy/ : nginx front proxy on port 8095 (/care/* and /api/*)
- Redis: shared login session between care screens and the API

## Requirements

1. `dotnet build` and `dotnet test` succeed from the repository root.
2. `docker-compose.yml` at the repository root serves the application on port 8095 the same way the
   legacy compose file does: `/care/*` for the screens, `/api/*` for the API. The facility group is
   chosen by host name for the screens and by the `X-Saffron-Tenant` header for the API. The
   development logins and seed data are the same.
3. Routes, bodies, status codes and database writes match the legacy behaviour (see plan.md, section
   "Behaviour contract").
4. The rate calculation, glucose checks and time handling match the legacy code exactly, including
   rounding modes (PHP `round()` in the care screens, .NET `Math.Round` banker's rounding in the API).
5. Fail-fast configuration: the app refuses to start without a catalog connection string and a Redis
   endpoint.

## Clarifications

[NEEDS CLARIFICATION: the goal says the SQL Server database code in db/ is the estate's database, and
requires the same data written to the database, but the engine instructions say Target DB: postgres — which one wins?]

Resolution used for this build until the operator answers: SQL Server is kept (Microsoft.Data.SqlClient,
the same stored procedures and seed scripts). The engine's Npgsql / EF Core instruction is not applied.

[NEEDS CLARIFICATION: the goal says the stored procedures and seeds are SQL Server code and the compose
file uses mssql/server:2017, but the engine instructions say Microsoft.EntityFrameworkCore with Npgsql — which one wins?]

## Out of scope

- Fixing legacy bugs (for example the care screen shows the current time as "next check due" when there
  is no earlier correction; this is kept).
- Deployment, pushing, merging.
- Visual redesign: markup follows the legacy templates.
