# FarmFMS Backend — CLAUDE.md

ASP.NET Core Web API (.NET 10), Simple Clean Architecture, Microsoft SQL Server.
This solution (`FarmFMS.sln`) is fully independent of the Frontend. Never reference Frontend projects.

## Build / run

```bash
dotnet build FarmFMS.sln
dotnet run --project src/FarmFMS.API            # http://localhost:5158, https://localhost:7196
```

Health: `GET /health` (liveness, no dependencies), `GET /health/ready` (readiness: opens a SQL Server connection; 200 `Healthy` / 503 `Unhealthy`, no details). Both are anonymous, mapped in `API/Common/Health/HealthEndpoints.cs` over the registered checks, and listed in Swagger (tag `Health`). Development only: OpenAPI document `GET /openapi/v1.json` (built-in `Microsoft.AspNetCore.OpenApi`, XML comments included)
and Swagger UI `GET /swagger` (`Swashbuckle.AspNetCore.SwaggerUI`, UI only — it renders that same document; no Swashbuckle generator;
approved AD-18 exception, enforced by `OpenApiPackageTests`).

## Projects and dependency rule

```
FarmFMS.Domain          -> (nothing)
FarmFMS.Application     -> Domain
FarmFMS.Infrastructure  -> Application, Domain
FarmFMS.API             -> Application, Infrastructure
```

- **Domain** – entities, value objects, enums, domain rules. No package or framework dependencies.
- **Application** – use cases/services, DTOs, interfaces (e.g. repositories, `IDbConnectionFactory`). No SQL, no EF, no Dapper.
- **Infrastructure** – EF Core `FarmFmsDbContext` (schema + migrations), Dapper repositories, `SqlConnectionFactory`.
- **API** – controllers, request/response wiring, DI composition (`AddApplication()`, `AddInfrastructure()`).

Never add a reference that points "outward" (e.g. Domain -> Application, Application -> Infrastructure).

## Data access rules (non-negotiable)

1. **EF Core Code First is used ONLY for schema definition and migrations.**
   - Entity configurations live in `FarmFMS.Infrastructure/Persistence/Configurations` (`IEntityTypeConfiguration<T>`, picked up by `ApplyConfigurationsFromAssembly`).
   - Never query or save application data through `FarmFmsDbContext`.
2. **Dapper is used for ALL application data access.**
   - Obtain connections through `IDbConnectionFactory.CreateOpenConnectionAsync()`.
3. **All CRUD and business operations go through Stored Procedures.**
   - Call through `IStoredProcedureExecutor` only. No inline/ad-hoc SQL anywhere.
   - Procedure references are **dbo-qualified** (`dbo.ScrAppUser`); the executor rejects anything else.
   - Stored Procedures are **separate version-controlled scripts** in `../Database/StoredProcedures/<Module>/dbo.<Name>.sql`.
     **Never deploy them through EF migrations; migrations must not use `migrationBuilder.Sql`.**
4. **No physical foreign keys** in any artifact (EF model, migrations, SQL scripts).
5. Naming (AD-08): the Module 01 specification overrides the PMS standard where it says so (no `Lk` prefix, `UserId`-style columns).
   **No custom length limit:** names are meaningful and descriptive, within SQL Server's 128-character limit, never truncated,
   deterministic and unique. Keys, constraints and indexes are named explicitly with `Persistence/Naming/DbObjectNames`
   using full names: `PK{Table}`, `UQ{Table}_{Columns}`, `IX{Table}_{Columns}`, `DF{Table}_{Column}`, `CK{Table}_{Column}`
   (e.g. `UQUserRole_UserIdRoleId`; Module 01 names in `Database_Conventions.md` §3.3). Abbreviations: only the approved list in
   `DbAbbreviations` (Usr, Pwd, Hier, Sess, Terr, Hist, Perm, Rev, Deact, Act, Req, Enc, Rol), and only where a rule requires it
   (readability first). Schema is always `dbo`.
   Module 01 procedure names are used exactly as in the specification (incl. `User`); new procedure names use `Usr`.
6. Stored Procedure errors: `THROW 50000` = generic; `THROW 50001–50999` = business errors, each registered in
   `Infrastructure/Data/StoredProcedureErrors.cs`. THROW text is never shown to clients.
7. Do **not** modify the approved database specification. If the spec looks wrong or incomplete, stop and ask.

Full rules: `../Documentation/06_Database/Database_Conventions.md`. Decisions: `../Documentation/Architecture/FarmFMS_Architecture_Decisions_v2.md`
(the single authoritative record; `FarmFMS_Architecture_Decisions.md` is superseded).

## Migrations

`dotnet-ef` is a local tool pinned to the EF Core version (`dotnet-tools.json`); run `dotnet tool restore` once.

```bash
dotnet ef migrations add <Name> --project src/FarmFMS.Infrastructure --startup-project src/FarmFMS.API
dotnet ef database update      --project src/FarmFMS.Infrastructure --startup-project src/FarmFMS.API
```

Connection string: `ConnectionStrings:FarmFMS`. It is **empty in the committed `appsettings.json`** and the API stops at startup until
it is supplied: User Secrets in Development (the API has a `UserSecretsId`)
`dotnet user-secrets set "ConnectionStrings:FarmFMS" "<value>" --project src/FarmFMS.API`, or env var `ConnectionStrings__FarmFMS`.
Never commit credentials: an architecture test rejects SQL credentials and non-empty password / secret / key / token values in `appsettings*.json`.

## Foundation (Step 02)

| Concern | Type | Location |
|---|---|---|
| Result / error model | `Result`, `Result<T>`, `Error`, `ErrorType` | `Application/Common/Results` |
| Audit base types | `AuditableEntity` (Id + CreatedBy/CreatedDate/ModifiedBy/ModifiedDate per the Module 01 spec), `AuditableDto` | `Domain/Common`, `Application/Common/Dtos` |
| Input helpers | `StringExtensions` (`TrimToNull`, `CollapseWhitespace`, `ToCode`, `ToEmail`, `ToDigitsOnly`, `ToSearchTerm`, `Mask`), `NullableExtensions.Required()` → validation `Result` | `Application/Common/Extensions` |
| DB error (translated) | `DataAccessException` | `Application/Common/Exceptions` |
| Connections | `IDbConnectionFactory` → `SqlConnectionFactory` | `Application/Abstractions/Data`, `Infrastructure/Data` |
| Transactions | `IUnitOfWork` → `UnitOfWork` (scoped; `ExecuteInTransactionAsync`) | same |
| Stored Procedures | `IStoredProcedureExecutor` → `StoredProcedureExecutor`; writes return `@NewId` via `ExecuteForNewIdAsync` and `@RowsAffected` via `ExecuteForRowsAffectedAsync` (AD-09; names in `StoredProcedureOutputParameters`) | `Infrastructure/Data` |
| Procedure-name rules | `StoredProcedureName` (dbo-qualified, Scr/Sre/Sup/Sde/Sva, PascalCase, ≤ 128 chars SQL Server limit) | `Infrastructure/Data` |
| SQL error mapping | `SqlErrorTranslator` + business error catalogue `StoredProcedureErrors`; each entry names its module range `StoredProcedureErrorRange` (Module01 50001–50099, Module02 50100–50199, Module03 50200–50299, SharedAndFuture 50300–50999) | `Infrastructure/Data` |
| DB object names | `DbObjectNames`, `DbAbbreviations` | `Infrastructure/Persistence/Naming` |
| Current user | `ICurrentUserService` → `CurrentUserService` | `Application/Abstractions/Identity`, `API/Common/Identity` |
| Audit actor (CreatedBy) | `IAuditActorService` → `AuditActorService`; `ISystemUserProvider` → `ConfiguredSystemUserProvider` | `Application/Abstractions/Identity`, `Application/Common/Identity`, `Infrastructure/Identity` |
| Errors → HTTP | `GlobalExceptionHandler`, `ErrorStatusCodes`, `ApiControllerBase` | `API/Common/Errors`, `API/Controllers` |
| Options | `DatabaseOptions` (`Database`), `SystemUserOptions` (`SystemUser`), validated on start | `Infrastructure/Data`, `Infrastructure/Identity` |

Repository convention: `Infrastructure/Repositories/<Module>/<Feature>Repository.cs` injects `IStoredProcedureExecutor`
(never `FarmFmsDbContext`); procedure names are dbo-qualified `const string` fields in a static `<Module>Procedures` class
(the architecture tests validate them). Services return `Result`/`Result<T>`; controllers inherit `ApiControllerBase`
and use `OkOrProblem` / `CreatedOrProblem` (201 + `{ id }`) / `NoContentOrProblem` / `ProblemFrom` — no try/catch.

Audit convention: every write service gets `CreatedBy`/`ModifiedBy` from `IAuditActorService.GetActorIdAsync()` and stops on
failure. Background jobs wrap their work in `using (auditActor.BeginSystemOperation("JobName"))`, which uses the System user.
`SystemUser:UserId` (the seeded System `AppUser.Id`) is set per environment (env var `SystemUser__UserId` or user-secrets),
never in committed files; without it, system operations fail closed.

## Tests

```bash
dotnet test FarmFMS.sln
```

- `tests/FarmFMS.ArchitectureTests`:
  - layer dependencies; no data-access packages in inner layers; DbContext never injected outside `Persistence`;
  - inline SQL via a Roslyn scan of all string forms (verbatim/raw/interpolated/concatenated, multi-line), migrations included;
  - no ad-hoc commands or EF data-access APIs; no FK constructs; migrations never use `migrationBuilder.Sql`;
  - EF model: no FKs or navigations, `dbo` only, PascalCase identifiers within SQL Server's 128 chars, PK/UQ/IX/DF/CK forms,
    named DEFAULT constraints, no duplicate object names;
  - the 40 Module 01 key/constraint/index names: generated as documented, valid, collision-free;
  - `../Database/**/*.sql`: no foreign keys, procedures dbo-qualified and valid;
  - procedure-name constants valid;
  - naming policy: abbreviation list = approved list; preserved procedure names = the specification workbook's
    "Stored Procedures" sheet; new procedure names must not use `User`.

  `DetectorTests` and the `ViolatingDbContext` model prove that each rule fails on bad input.
- `tests/FarmFMS.UnitTests` – Result model, procedure-name validation, SQL error translation and business error catalogue,
  unit of work, executor guard, output-parameter contract (fake commands), audit actor / System user, audit base types,
  string / nullable helpers, naming helpers, exception handler, `ApiControllerBase`, current user,
  options validation at startup, in-memory host (`/health`, ProblemDetails).
- **Pending:** SQL Server integration tests (live Dapper execution, real `SqlException` translation, procedure scripts).
  They need a SQL Server test database. **Live SQL Server execution has not been verified.**

## Conventions

- Nullable reference types and implicit usings are enabled in every project.
- Async all the way; pass `CancellationToken` through to Dapper calls.
- One DI extension per layer (`DependencyInjection.cs`: `AddApplication()`, `AddInfrastructure()`, `AddApi()`); register module services there.
- Time: inject `TimeProvider` (UTC); never `DateTime.Now`.
- Logging: built-in `ILogger<T>` with message templates (approved: no Serilog at this stage). Never log passwords, keys, tokens, connection strings, parameter values or request bodies. HTTP logging records method, path, status and duration only.
- Packages: keep EF Core packages at the same version; `System.Security.Cryptography.Pkcs` is pinned in Infrastructure to patch a vulnerable transitive version from `Microsoft.Data.SqlClient`.

## Current status

Step 02 (backend foundation) complete, including the Step 02 review decisions (AD-04, AD-08, AD-09, AD-14, AD-16, AD-24).
No tables, migrations, Stored Procedures, authentication or business modules exist yet.
