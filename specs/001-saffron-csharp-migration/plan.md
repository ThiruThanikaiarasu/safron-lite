# Plan: Saffron-lite C# migration

## Layout

```
Saffron.sln
docker-compose.yml
App/                      ASP.NET Core app (net8.0): screens, API, services, data
  Program.cs
  Controllers/Api/        Health, Patients, Glucose, Orders (JSON API, /api/*)
  Controllers/Care/       Login, Patients, Glucose (MVC screens, /care/*)
  Services/               GlucoseService, PatientService, OrderService, RateCalculator, GlucoseRules, FacilityTime
  Data/                   CatalogRepository, TenantDatabase, PatientRepository, ReadingRepository, OrderRepository, UserRepository
  Session/                SharedSessionStore (Redis, same key and JSON as the PHP care app)
  Models/                 TenantInfo, SessionInfo, GlucoseRequest, ...
  Views/                  Razor templates mirroring care/application/views
  wwwroot/assets/         vendored jQuery, Bootstrap, saffron.css, glucose-entry.js, patient-detail.js
Dockerfile
db/                       SQL Server scripts (unchanged from legacy)
proxy/nginx.conf          front proxy on port 8095
tests/App.Tests/          xUnit tests (controllers, data, session, rules)
```

## Behaviour contract

### Tenant resolution
- API: header `X-Saffron-Tenant` trimmed and lower-cased. Empty -> no tenant. Lookup in `SaffronCatalog.dbo.Tenant`
  where `IsActive = 1`. Results cached per code.
- Screens: first DNS label of the host with port removed, lower-cased. Needs at least two labels and must
  match `^[a-z0-9-]{2,20}$`, otherwise no tenant. Unknown tenant -> HTML "Facility not found" page, status 404.

### Session
- Redis key `sess:{tenant}:{id}`, value JSON with fields `tenant, user_id, username, display_name, role, created, last_seen`.
- Cookie `SAFFRON_SID`, id must match `^[a-f0-9]{32,64}$`. New id = 20 random bytes as lower-case hex.
- Care cookie: HttpOnly, path `/`. Logout cookie: expired, path `/`.
- Idle TTL 1800 s, refreshed on each load. `last_seen` is set to the current Unix time on each load.
- A session whose `tenant` differs from the resolved tenant is treated as absent.

### API (`/api`)
| Method | Path | Guard | Response |
|---|---|---|---|
| GET | `/api/health` | none | 200 `{status:"ok"}` |
| GET | `/api/status` | tenant only | 200 `{status,tenant,name,hospital,facilityTimeZone,patients}`; tenant unknown -> 404 `{status:"unknown_tenant"}` |
| GET | `/api/patients` | tenant + session | 200 array of `{patientId,mrn,name:"Last, First",unit,bed}` |
| GET | `/api/patients/{id}` | tenant + session | 200 `{patient,order,recent:{count,mean},minutesSinceLastReading}`; not found -> 404 (no body) |
| GET | `/api/patients/{id}/readings?top=20` | tenant + session | 200 array (proc clamps top to 1..100, default 20) |
| POST | `/api/glucose/evaluate` | tenant + session (401 body `{ok:false,error:"Session expired. Please log in again."}`) | 200 `{ok,data}` or `{ok:false,error}` |
| POST | `/api/glucose/readings` | same | 200 `{ok,data}` or `{ok:false,error}` |
| POST | `/api/orders/{id}/discontinue` | tenant + session (guard body `{error:"unknown_tenant"}` 400 / `{error:"unauthorized"}` 401) | 200 `{orderId,status:"Discontinued"}`, 409 `{message:"Order not found or not active."}` |
| POST | `/api/orders/{id}/resume` | tenant + session, then role Admin (case-insensitive) else 403 `{message:"Only an administrator can resume an order."}` | 200 `{orderId,status:"Active"}`, 409 `{message:"Order cannot be resumed."}` |

Glucose preparation (`Prepare`), in order:
1. Null request or null value -> "Value is required."
2. Value outside 20..600 -> "Value must be between 20 and 600."
3. Patient missing or inactive -> "Patient not found."
4. Non-empty (after trim) `enteredTime`: parse `HH:mm` invariant; invalid -> "Time must be entered as HH:MM."
   Converted from facility local time today to UTC. Later than now + 5 min -> "Entered time is in the future."
5. Low (< 72): rate 0 and message "Low glucose. Hold the infusion and follow the low glucose procedure."
   Otherwise rate = RateCalculator(value, active order); no active order or any exception -> rate 0 (logged).
6. Value >= 300 -> "High value. Repeat the measurement within 15 minutes."
7. Last correction = MAX(TakenAtUtc) where Rate > 0. Next due = last + 60 min. If now < next due:
   "Correction interval not elapsed. Next check due in N minutes." (N = ceil of minutes remaining)

Save: after Prepare, call `dbo.usp_Reading_Insert` (rate, value, taken time, entered by session user, comment or NULL).
SQL error (e.g. RAISERROR interval) -> `{ok:false,error:<exception message>}`. Then read back Rate and IsLow.

Rate: `base + ((value - target) / divisor) * multiplier`, floored at 0, `Math.Round(…, 2)`.

Mean in `/api/patients/{id}`: `Math.Round(average, MidpointRounding.ToEven)` (legacy .NET default).

### Screens (`/care`)
- `GET /care`, `/care/` -> patients (requires login). `GET /care/login` and `POST /care/login`, `GET /care/logout`.
- `GET /care/patients` -> list (unit, bed, `LASTNAME, First`, MRN, Glucose button).
- `GET /care/patients/{id}` -> detail (404 page if id not digits or patient not found). Summary comes from the same
  service as `/api/patients/{id}`. Average of the first 6 history values rounded with PHP semantics (half away from zero).
  Discontinue and resume buttons only for admins; they POST to `/api/orders/{id}/discontinue|resume`.
  `?saved=N` shows "Reading #N saved." (leading digits, PHP int cast).
- `GET /care/patients/{id}/glucose` -> entry form. `POST /care/glucose/preview/{id}`, `POST /care/glucose/save/{id}`.
  Validation (CodeIgniter messages): value required|integer|>=20|<=600; confirm_value required|matches[value];
  entered_time HH:MM regex `^([01][0-9]|2[0-3]):[0-5][0-9]$` or empty; comment max 200 characters; save also requires
  ack_identity, ack_value and (when api_low = 1) ack_low.
- Preview: API 401 -> login; no response -> "The recommendation service is not available. Try again."; `ok` false -> error text.
  `next_due_local` is the facility-local HH:mm of `nextCheckDueUtc`. When that is null the legacy `new DateTime(null)`
  gives the current time, so the current facility-local time is shown.
- Rate shown as `round(min(rate, 15), 1)` with PHP semantics, formatted with one decimal.
- Save: failure re-renders the unlocked form with the error; success redirects to `/care/patients/{id}?saved={readingId}`.
- Dates in screens: UTC ISO string converted to facility time zone, format `d M H:i` (for example "10 Jan 09:30").
- Patient name in screens: `STRTOUPPER(last), first`; `(unknown)` if both empty.
- Unknown route under /care -> 404 page "The page you requested was not found."

### Rounding and number formats
- Care average: PHP `round()` (half away from zero), `(int)` cast.
- API mean: .NET `Math.Round` default (to even), serialised as decimal.
- Decimal JSON scale is preserved (for example baseRate `1.500`, divisor `40.00`), as System.Text.Json writes decimal with scale.

## Database

- Target: SQL Server 2017 image, `Microsoft.Data.SqlClient`, stored procedures in `db/11_tenant_procs.sql` called by name.
  This is the resolution of the conflict recorded in spec.md. The engine's Npgsql/EF Core instruction is not applied.
- Catalog `SaffronCatalog`; tenant databases `Saffron_Alder`, `Saffron_Birch` (copies of the legacy `db/` scripts).
- Tenant connection = catalog connection string with `Initial Catalog` replaced by the tenant database.

## Configuration (fail-fast)

- `SAFFRON_CATALOG` env or `ConnectionStrings:Catalog`: required, startup throws if absent.
- `SAFFRON_REDIS` env or `Redis`: required, startup throws if absent.
- `SESSION_IDLE_SECONDS` default 1800. `SAFFRON_API_BASE` not needed (in-process).
- Care screen HTML uses the request host for `base_url`, as the legacy `config.php` does.
