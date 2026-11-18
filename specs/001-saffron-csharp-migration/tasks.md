# Tasks: Saffron-lite C# migration

- [x] T1 Solution, App.csproj (net8.0), NuGet packages, Program.cs with fail-fast configuration
- [x] T2 Models: TenantInfo, SessionInfo, GlucoseRequest, OrderRecord, DTO shapes for JSON responses
- [x] T3 Services: RateCalculator, GlucoseRules (thresholds), FacilityTime (HH:mm to UTC, ISO format), PatientNames
- [x] T4 Data: CatalogRepository (tenant cache), TenantDatabase (connection per tenant), stored-procedure repositories
- [x] T5 Session: SharedSessionStore on Redis (key, JSON, sliding TTL, id validation)
- [x] T6 Services: PatientService, OrderService, GlucoseService (Prepare and Save)
- [x] T7 API controllers: Health, Patients, Glucose, Orders with the guard flow of the legacy controllers
- [x] T8 Care controllers and Razor views mirroring the legacy templates, form validation with CodeIgniter messages
- [x] T9 Static assets, glucose-entry.js, patient-detail.js (copied), 404 and facility-not-found pages
- [x] T10 xUnit tests: rate calculator, glucose rules, facility time, session id, tenant parsing, form validation, API responses (in-memory fakes for data and Redis)
- [x] T11 Dockerfile, docker-compose.yml (sql, sql-init, redis, app, proxy on 8095), nginx.conf
- [x] T12 dotnet build and dotnet test from the root, zero errors
