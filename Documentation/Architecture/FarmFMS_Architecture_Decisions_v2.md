# Farm FMS – Architecture Decisions

| | |
|---|---|
| Document | **Single authoritative architecture decision record.** `FarmFMS_Architecture_Decisions.md` is superseded and only points here. |
| Step | 02 – Backend Foundation decisions; reconciled after the Backend Readiness Audit |
| Status | Per decision (see Index). Step 02 final approval pending. |
| Date | 2026-11-04 (recorded), 2026-11-04 (naming decisions approved after the readiness audit; reconciliation with the superseded record) |
| Inputs | `RouteNexa_Backend_Review.md`, `RouteNexa_Frontend_Review.md`, the Farm FMS mandatory architecture, the Farm FMS scaffold, `Documentation/06_Database/Farm_FMS_Module_01_Database_Specification_v0.2.xlsx`, `Documentation/Database/SQL Server Best Practices - Prima Management Services Pvt Ltd.pptx`, `Documentation/Security/Password_Encryption_Design.md` (Draft – Pending Approval) |

Each decision records the context, the decision, and its consequences.

**Decision status:**
- **Mandatory** – project rule given to the project.
- **Approved** – confirmed for this stage.
- **Proposed** – recommendation, not yet approved.
- **Pending** – needs an input or a decision.
- **Blocked** – cannot proceed until a dependency is resolved.

**Implementation status** is stated separately and only as verified in the code and tests:
- **Implemented** – built and covered by tests.
- **Partly implemented** – some of it is built; the rest is listed.
- **Not implemented.**

> **Reference documents:** the PMS SQL Server naming standard (slide deck), the Module 01 database specification v0.2 and the password encryption design are under `Documentation/`. Where a concrete table, column, key type, procedure name or encryption field is specified by an approved source document, that source remains authoritative. Do not invent or silently rename approved database objects. Database conventions derived from these decisions: `Documentation/06_Database/Database_Conventions.md`.

---

## Index

| ID | Decision | Decision status | Implementation |
|---|---|---|---|
| AD-01 | Two independent solutions, HTTP only | Mandatory | Implemented (scaffold; no Frontend → Backend references) |
| AD-02 | Simple Clean Architecture, four projects | Mandatory | Implemented (architecture tests) |
| AD-03 | Folder convention `<Module>/<Feature>` | Proposed | Partly implemented; API folder name differs (Pending) |
| AD-04 | EF Core Code First for schema and migrations only; procedures as separate scripts | Mandatory | Implemented (rules and tests; no schema yet) |
| AD-05 | Dapper through `StoredProcedureExecutor` on `IDbConnectionFactory` | Proposed (implements a mandatory rule) | Implemented, except the AD-09 output-parameter contract |
| AD-06 | Stored Procedures for all CRUD and business operations; no inline SQL | Mandatory | Implemented (executor guard, architecture tests) |
| AD-07 | No physical foreign keys in any database artifact | Mandatory | Implemented (architecture tests) |
| AD-08 | Naming: descriptive identifiers (no custom length limit; SQL Server's 128), approved prefixes and abbreviations | Approved | Implemented (naming rules and tests) |
| AD-09 | Stored Procedure result and error contract | Approved | Partly implemented (error translation, catalogue); output parameters and per-module ranges not implemented |
| AD-10 | Repositories return entities or read models, never API DTOs | Proposed | Not implemented (no repositories yet) |
| AD-11 | Password protection: AES-256-GCM | Mandatory; design document Pending Approval | Not implemented (Blocked on design approval) |
| AD-12 | JWT access token + rotating hashed refresh token | Proposed | Not implemented |
| AD-13 | Authorization: secure by default + named policies | Proposed | Not implemented |
| AD-14 | Audit identity (authenticated user / System user); UTC everywhere | Approved direction | Partly implemented (temporary configuration bridge); lookup procedure Blocked |
| AD-15 | Errors: RFC 7807 ProblemDetails + global exception handler | Proposed | Implemented |
| AD-16 | Logging: built-in `ILogger<T>`, no Serilog, no secrets, correlation id | Approved | Implemented |
| AD-17 | Configuration: validated options, secrets outside source | Proposed | Partly implemented (validated options; user-secrets not set up) |
| AD-18 | API conventions (incl. approved Swagger UI exception) | Proposed; Swagger UI exception Approved | Partly implemented (built-in OpenAPI + Swagger UI, Development only; 201 helper) |
| AD-19 | Frontend: Blazor Interactive Server, Radzen + Bootstrap split | Mandatory / Proposed | Scaffold only |
| AD-20 | Frontend HTTP layer: typed clients + `ApiException` funnel | Proposed | Not implemented |
| AD-21 | Frontend authentication state and server-side token storage | Approved direction | Not implemented |
| AD-22 | Frontend CRUD UI toolkit | Proposed | Not implemented |
| AD-23 | Testing | Approved scope | Partly implemented (architecture and unit tests); integration and contract tests Blocked / not implemented |
| AD-24 | Schema: `dbo` only; procedure references dbo-qualified | Approved | Implemented |

---

## AD-01 Two independent solutions, HTTP only (Mandatory)

**Decision:** `Backend/FarmFMS.sln` and `Frontend/FarmFMS.Web.sln` stay separate. The Frontend never references Backend assemblies. Contracts are duplicated as Frontend models in `Features/<Module>/<Feature>/Models`.
**Consequence:** contract drift is possible (RouteNexa shipped an `AccessTokenExpiresUtc` vs `AccessTokenExpiresDateTime` mismatch). Mitigate with the OpenAPI document as the contract source and contract tests (AD-23).
**Implementation:** verified in the readiness audit; no Frontend project references Backend projects.

## AD-02 Simple Clean Architecture (Mandatory)

```
FarmFMS.Domain          -> (nothing)
FarmFMS.Application     -> Domain
FarmFMS.Infrastructure  -> Application, Domain
FarmFMS.API             -> Application, Infrastructure
```

**Decision (clarification vs RouteNexa):** Application contains interfaces, services, DTOs, validation and the result model. It contains **no crypto, JWT, SQL, Dapper or EF packages**. Password encryption, token issuing and key access are Infrastructure implementations of Application interfaces (`IPasswordProtector`, `ITokenService`, `IDbConnectionFactory`).
Each layer registers its own types in its own `DependencyInjection.cs` (`AddApplication()`, `AddInfrastructure()`, `AddApi()`). `Program.cs` only composes.
**Implementation:** verified by `LayerDependencyTests` (project files and compiled assemblies). Since Step 02.1, `Program.cs` only composes `AddApplication()`, `AddInfrastructure()` and `AddApi()` and builds the pipeline. Controllers, OpenAPI and health checks are registered in `AddApi()`; the database readiness check is registered in `AddInfrastructure()`.

## AD-03 Folder convention (Proposed)

| Project | Layout |
|---|---|
| Domain | `Common/`, `<Module>/<Feature>/` (entities, enums, value objects) |
| Application | `Common/` (Results, Interfaces, Extensions, Dtos), `<Module>/<Feature>/` containing `I<Feature>Service`, `<Feature>Service`, `I<Feature>Repository`, `<Feature>Dto`, `<Feature>CreateRequest`, `<Feature>UpdateRequest`, read models |
| Infrastructure | `Data/` (connection factory, executor, unit of work, SQL error translator), `Persistence/` (DbContext, `Configurations/<Module>/`, `Migrations/`, `Naming/`), `Repositories/<Module>/`, `Security/`, `Identity/` |
| API | `Controllers/<Module>/`, `Infrastructure/` (exception handler, current user, auth setup) |
| Web | `Features/<Module>/<Feature>/{Components,Models,Pages,Services}`, `Features/Common/…`, `Services/Api/` (options + registration) |

Unlike RouteNexa (which splits `Features/` and `Interfaces/`), the interface sits beside its feature, so one feature means one folder per layer. No spaces in file names. File header block (Description / Author / Created Date) and XML docs as in RouteNexa's newer files.

**Pending:**
- The implemented API folder is `Common/` (`Common/Errors`, `Common/Identity`), not `Infrastructure/`. This was chosen to avoid a namespace clash with the `FarmFMS.Infrastructure` project. The folder name needs a decision.
- Application abstractions are in `Abstractions/` (`Abstractions/Data`, `Abstractions/Identity`), following the original scaffold, rather than `Common/Interfaces`.
- Health checks live in `API/Common/Health` (endpoints) and `Infrastructure/HealthChecks` (database readiness check). Neither folder is in the layout table above.
- Implemented as in the layout table (Step 02.1): `Domain/Common` (`AuditableEntity`), `Application/Common/Extensions` (string / nullable helpers) and `Application/Common/Dtos` (`AuditableDto`).

## AD-04 EF Core Code First for schema and migrations only (Mandatory)

**Decision:**
- `FarmFmsDbContext` holds `DbSet`s and `IEntityTypeConfiguration<T>` classes (`Persistence/Configurations/<Module>/`) that define tables, columns, lengths, PKs, unique indexes, defaults and check constraints **exactly per the approved Module 01 specification**.
- EF Core migrations manage database schema objects only. Stored Procedure definitions are maintained as separate version-controlled SQL scripts under `Database/` and deployed through the database release/deployment process; they are not embedded in EF migrations.
- `FarmFmsDbContext` is **never** injected into repositories or services.
- No navigation properties or `HasOne/WithMany` relationships, so EF generates no FK constraints (AD-07).

**Rejected:** RouteNexa's approach (hand-written table scripts + DbContext used only to borrow a connection).

**Implementation:**
- Architecture tests check that no class outside `Persistence` receives or holds a `DbContext`.
- They check that no EF data-access APIs are used, and that migrations never call `migrationBuilder.Sql(...)` or create procedures.
- Script layout: `Database/StoredProcedures/<Module>/dbo.<Name>.sql`.
- No migrations or scripts exist yet.
- **Not verified:** the deployment process for scripts (tooling/pipeline) is not defined.

## AD-05 Dapper through a Farm FMS StoredProcedureExecutor (Proposed; implements AD-06)

**Context:** RouteNexa's `DapperExecutor` is a good idea, but it gets its connection from `AppDbContext`.
**Decision:** `Infrastructure/Data/StoredProcedureExecutor` (scoped), modelled on RouteNexa's method set:

| Method | Purpose |
|---|---|
| `QueryAsync<T>` | list result |
| `QuerySingleOrDefaultAsync<T>` | single row |
| `QueryMultipleAsync<T1,T2>` / `QueryMultipleAsync(proc, Func<GridReader, Task<TResult>>, params)` | two / N result sets |
| `ExecuteAsync` | command; write results per AD-09 (output parameters) |
| `ExecuteScalarAsync<T>` | scalar |

- `CommandType.StoredProcedure` is fixed, and the procedure reference must be a valid dbo-qualified name (AD-08, AD-24), so inline SQL isn't possible through the executor.
- Connections come only from `IDbConnectionFactory` / `SqlConnectionFactory`.
- A scoped `IUnitOfWork` (`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` / `ExecuteInTransactionAsync`) owns one connection and transaction when a use case spans several procedures. The executor enlists automatically. The default is one atomic procedure per business operation.
- A single private `BuildCommand` (procedure name guard, parameters, transaction, `commandTimeout` from validated `DatabaseOptions`, `CancellationToken`).
- Output, return-value and table-valued parameters are passed through Dapper `DynamicParameters`.
- Every `SqlException` passes through `SqlErrorTranslator` (AD-09).
- Procedure names come from one `static class <Module>Procedures` of dbo-qualified `const string` names.

**Implementation:** implemented and unit-tested (guard, unit of work, output-parameter contract).
- AD-09 output-parameter contract (Step 02.1): `ExecuteForNewIdAsync` adds `@NewId INT OUTPUT` and returns it; `ExecuteForRowsAffectedAsync` adds `@RowsAffected INT OUTPUT` and returns it. Both reject caller parameters with the reserved names and throw when the procedure leaves the value unset (or `@NewId` ≤ 0, `@RowsAffected` < 0). Names are in `StoredProcedureOutputParameters`. Tested against in-memory fake commands, including enlistment in an active unit of work.
- **Not verified:** live execution against SQL Server (AD-23).

## AD-06 Stored Procedures for all CRUD and business operations (Mandatory)

No inline SQL, no ad-hoc `SqlCommand`, no EF LINQ for application data. Every read and write goes repository → executor → stored procedure. Business rules that need data consistency (uniqueness, referential checks, state transitions) live in the procedure inside a transaction; input-shape validation lives in DTO annotations and the Application service.
**Implementation:** the executor rejects anything that isn't a valid procedure reference. A Roslyn-based architecture test detects SQL in every string form, including multi-line verbatim, raw, interpolated and concatenated strings.

## AD-07 No physical foreign keys (Mandatory)

**Decision:** no `FOREIGN KEY` constraints are created in any database artifact (EF model, EF migrations, SQL scripts; no EF relationships, no `migrationBuilder.AddForeignKey`). Instead:
- Reference columns are indexed as per the specification.
- Insert/update procedures validate that referenced rows exist (and are active, where the spec requires) and raise a registered business error otherwise (AD-09).
- Delete/deactivate procedures check for dependents before proceeding.

**Implementation:** architecture tests cover three places:
- the EF design-time model: no foreign keys, no navigations;
- C# sources, migrations included: no relationship or FK constructs;
- `Database/**/*.sql`: no `FOREIGN KEY` / `REFERENCES`.

Each rule is proven against a deliberately invalid sample.

## AD-08 Naming: descriptive identifiers, approved prefixes (Approved)

**Approved (updated after the readiness-audit reconciliation): the custom 15-character identifier limit is removed.**
- Database object names are **meaningful and descriptive**.
- The only length limit is SQL Server's actual identifier limit of **128 characters** (`sysname`).
- Identifiers are **never truncated** to meet a length restriction.
- All generated names are **deterministic and unique**.
- RouteNexa names (e.g. `[master].[Customer_GetAllActive]`) are still not a valid pattern for Farm FMS (schema, prefixes, underscores).

> **Decision history:** the earlier rule "every identifier ≤ 15 characters" and its consequences are withdrawn: the compact form `UQUsrRol_UsrRol`, the trailing-`Id` compaction step, and the list of Module 01 names that could not fit. The approved prefixes, abbreviations and procedure-name rules below remain in force.

**Approved Farm FMS clarifications where the Module 01 specification intentionally overrides the general PMS examples:**
- Do not add an `Lk` prefix to link tables; retain the approved Module 01 table names (for example, `UserRole`).
- Use the specification's `UserId`-style reference columns, not `User_Id`.

**Approved object name patterns** (they replace the PMS `Cpk`/`Cun`/`Cck`/`I{u|n}{c|n}` forms):

| Object | Pattern | Example |
|---|---|---|
| Primary key | `PK{Table}` | `PKRoleMenuAccess` |
| Unique constraint | `UQ{Table}_{ColumnOrColumns}` | `UQUserRole_UserIdRoleId` |
| Index | `IX{Table}_{ColumnOrColumns}` | `IXLoginHistory_UserIdEventDate` |
| Default constraint | `DF{Table}_{Column}` | `DFUserAccount_CanAccessMobile` |
| Check constraint | `CK{Table}_{Column}` | `CKLoginHistory_EventType` |

`{Table}` and `{Column}` are the full names from the specification. Multiple columns are concatenated in key order, as in the PMS index example `InnOrderDetails_OrderIdCustomerId`. **Do not silently truncate names or generate duplicate names.**

**Approved abbreviations** (preserved; the complete list; do not introduce more without documenting and approving them):
`Usr`, `Pwd`, `Hier`, `Sess`, `Terr`, `Hist`, `Perm`, `Rev`, `Deact`, `Act`, `Req`, `Enc`, `Rol`.
**Readability comes first:** abbreviations are not applied unnecessarily. Generated key, constraint and index names use full names; the list is used where an approved rule requires it (new procedure names, below) and as a fallback only if a full name would exceed 128 characters.

**Stored Procedure names:**
- Existing Module 01 procedure names are preserved **exactly** as defined in the specification v0.2, including those that use `User` (`ScrUserRole`, `SdeUserRole`, `ScrUserAccount`, `ScrUserTerr`, `SupUserUnlock`, `SupUserDeact`, `SupUserSessRev`, and the `AppUser` procedures). They are not silently renamed to `Usr`.
- **Documented exception:** these specification names are the only procedure names allowed to contain the word `User`.
- Newly introduced procedure names use `Usr` consistently, unless an approved specification explicitly requires otherwise. Do not mix `Usr` and `User` in new names.

The default schema is `dbo`; Stored Procedure calls must be schema-qualified (AD-24). C# names stay descriptive. EF configurations map database names explicitly (`ToTable`, `HasColumnName`, `HasName`, `HasDatabaseName`, `HasCheckConstraint`, `SetDefaultConstraintName`), and procedure names are constants. DEFAULT constraints are always named explicitly, because SQL Server's generated `DF__…` names are not deterministic.

**Module 01 object names:** the 40 keys, constraints and indexes implied by the specification are reviewed and listed in `Documentation/06_Database/Database_Conventions.md` §3.3. All 40 use full descriptive names and none collide; these are names only, nothing has been created.

**Implementation:**
- `Infrastructure/Persistence/Naming/DbObjectNames` builds the full descriptive name. Only if it exceeds 128 characters does it apply the approved abbreviations. If it still doesn't fit, it throws; it never truncates.
- `DbAbbreviations` holds exactly the approved list. `StoredProcedureName` allows names up to 128 characters.
- Architecture tests check:
  - table, column, schema and sequence names are PascalCase, with no `Lk` prefix and no `_Id` columns, within 128 characters;
  - every key, constraint and index name has the approved form;
  - DEFAULT constraints are named explicitly;
  - no two objects share a name;
  - procedure-name constants and SQL scripts are valid;
  - the preserved procedure list equals the specification workbook's "Stored Procedures" sheet;
  - new procedure names don't use `User`;
  - the 40 Module 01 names are generated as documented, valid and unique.
- EF Core silently truncates some over-128 identifiers (it appends `~`). The PascalCase rule reports such names, so an EF truncation cannot pass unnoticed.

**Pending:** the specification's *Read Me* sheet still states "maximum 15 characters" under Naming. The specification is not modified by the project team; its owner should update it to match this decision (remaining item 1).

## AD-09 Standard Stored Procedure result and error contract (Approved)

**Context:** RouteNexa write procedures return row counts via `SELECT`, read as `ExecuteScalarAsync<int>`, with no business error channel.
**Approved write-result contract:** use output parameters consistently; do not return write metadata through ad-hoc `SELECT` result rows.
- Insert procedures expose `@NewId` as an output parameter.
- Update and delete procedures expose `@RowsAffected` as an output parameter.
- The executor/repository contract must read these output parameters after command execution.

**Approved SQL business-error allocation:**

| Error range | Allocation |
|---|---|
| `50001–50099` | Module 01 |
| `50100–50199` | Module 02 |
| `50200–50299` | Module 03 |
| `50300–50999` | Shared errors and future modules (reserved) |

Use `THROW` for known business-rule failures and maintain a central error catalogue with unique numbers, stable codes, and application-safe messages. `SqlErrorTranslator` maps known errors to `NotFound`, `Conflict`, `Validation`, or `Forbidden`. Unknown SQL errors, including unregistered user-defined errors, return a generic server error; raw SQL messages must never be exposed to clients. Unique index violations (2601/2627) map to `Conflict`. Deadlock (1205) and timeout map to a retryable server error, but writes are never retried silently.

**Implemented mapping** (`Infrastructure/Data/StoredProcedureErrors.cs`, `SqlErrorTranslator.cs`):

| SQL error | API response |
|---|---|
| 50000 (generic procedure error) | HTTP 500, generic message |
| 50001–50999 registered in the catalogue | Catalogue message, code and status (400/401/403/404/409) |
| 50001–50999 not registered, ≥ 51000, unknown | HTTP 500, generic message |
| 2601 / 2627 | 409 `Database.Duplicate` |
| 547 | 409 `Database.ConstraintViolation` |
| 1205, -2, connectivity | 503 (never retried automatically) |

THROW and SQL message text never reach clients. The catalogue is validated at startup (module range, unique numbers and codes, allowed types, message present) and has no entries yet.

**Implementation:**
- Implemented: error translation and the catalogue.
- Implemented (Step 02.1): per-module ranges. `StoredProcedureErrorRange` defines the four approved ranges above. Every catalogue entry names its range, and the catalogue rejects a number outside that range or an unapproved range. Tests check that the ranges match this table and cover 50001–50999 without gaps or overlaps.
- Implemented (Step 02.1): the output-parameter contract in the executor (AD-05). Repositories are not built yet.

## AD-10 Repositories return entities or read models (Proposed)

Repositories return Domain entities (writes, single-aggregate reads) or Application-defined **read models** (joined, display-oriented rows). Never API DTOs (RouteNexa's `VehicleRepository` returned `VehicleDto`). Domain entities carry no display-only join columns. Services map to DTOs via per-feature `ToDto()` extensions.

## AD-11 Password protection: AES-256-GCM (Mandatory; design Pending Approval)

**Decision:** implement the approved Farm FMS AES-256-GCM password encryption design. RouteNexa's BCrypt `PasswordHashService` is not reused. Mechanism constraints Farm FMS will follow unless the approved design says otherwise:
- `IPasswordProtector` (Application interface) with an Infrastructure implementation using `System.Security.Cryptography.AesGcm`, a 256-bit key, a **unique 96-bit random nonce per encryption** and a 128-bit tag. Ciphertext, nonce, tag and **key version** are stored per the DB spec (`AppUser.EncPassword`, `PasswordNonce`, `PasswordTag`, `KeyVersion`).
- The key is never in source or `appsettings.json`. It is loaded from user-secrets (dev) or environment/secret store (other environments), validated at startup (length), and supports **key versioning for rotation**.
- Comparison of the recovered secret is constant-time (`CryptographicOperations.FixedTimeEquals`). Plaintext is never logged and is cleared from buffers where practical.

> **Review note (for the approvers):** AES-256-GCM is reversible encryption, which differs from the usual industry guidance (OWASP/NIST) to store passwords with a slow one-way hash such as Argon2id, BCrypt or PBKDF2. The Module 01 specification records the reason (an authorized Admin must be able to view a user's password) and calls it high-risk. `Documentation/Security/Password_Encryption_Design.md` exists as **Draft – Pending Approval** with 25 open items; key custody and the reversible-password rationale must be explicitly approved before implementation.
>
> **2026-11-05 update:** the user approved the login-path items (P-01, P-02, P-03, P-07, P-08 for Development only, P-23) for Module 01 Step 0. See the design document §11.1. All other items remain Pending Approval.

## AD-12 Tokens (Proposed)

Reuse the RouteNexa *pattern*, not its code or keys:
- Short-lived JWT access token (lifetime from options), signed with a Farm FMS key from secret storage, validated for issuer, audience, lifetime and signature with a small clock skew. Use `JsonWebTokenHandler`.
- Opaque random refresh token; **only its SHA-256 hash is stored** (spec: `UserSession.TokenHash VARBINARY(32)`); rotation on every refresh; **reuse detection revokes all of the user's sessions**; logout revokes; optional absolute session lifetime.
- Login endpoint rate-limited; generic "invalid username or password" for unknown, inactive and wrong-password cases; lockout and failure counts handled in stored procedures; login audit trail.
- All times UTC (fixes RouteNexa's `DateTime.Now` bug).
- *Whether Farm FMS uses JWT at all, and token lifetimes, follow the approved security requirements.*

## AD-13 Authorization (Proposed)

- `MapControllers().RequireAuthorization()` (secure by default); `[AllowAnonymous]` only on login, refresh, logout and `/health`.
- Named policies (`AddAuthorizationBuilder().AddPolicy(...)`) per permission defined in the security requirements, applied with `[Authorize(Policy = …)]`. The frontend mirrors them with `AuthorizeView Policy=…` for UI trimming only; the API remains the enforcement point.

**Implementation:** not implemented. All API endpoints are currently anonymous; there is no authentication middleware. The health endpoints (`/health`, `/health/ready`) are already explicitly `AllowAnonymous`.

## AD-14 Audit identity and time (Approved direction)

- Requests **never** carry `CreatedBy`/`ModifiedBy` (`INT` user IDs per the specification). Authenticated operations use the authenticated user's ID from `ICurrentUserService`.
- Background/system operations use a dedicated database System user record. Its username is configured as `SYSTEM`; its database ID must be resolved from the database through a dedicated Stored Procedure when the relevant security/module work is implemented. Do not hardcode the ID.
- Do not silently assign the System identity to ordinary unauthenticated writes. An unauthenticated write must be explicitly authorized as a system/public use case; otherwise reject it.
- Until the System user record and lookup procedure exist, the ID may be supplied through validated configuration as a temporary implementation bridge only; this is not the final source of truth and must not be hardcoded.
- All timestamps are UTC (`TimeProvider` in C#, `SYSUTCDATETIME()` in procedures); the frontend converts for display.

**Implementation (temporary bridge):**
- `IAuditActorService` returns the authenticated user's ID, or the System user's ID only inside an explicit `BeginSystemOperation(name)` scope.
- Otherwise it fails: `Audit.ActorRequired` (401), or `Audit.SystemUserNotConfigured` (500, fail closed).
- The System user ID comes from validated `SystemUser:UserId` (per environment; not committed; not set anywhere yet).
- **Blocked:** the Module 01 specification defines neither the `SYSTEM` user record nor a lookup procedure.

## AD-15 Errors (Proposed)

- `builder.Services.AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()` + `app.UseExceptionHandler()`.
- Services return `Result`/`Result<T>` with `ErrorType` (Validation, NotFound, Conflict, Forbidden, Unauthorized, Unavailable, Failure). A single controller base (`ApiControllerBase`) maps results to `ProblemDetails`. Model validation keeps `[ApiController]`'s `ValidationProblemDetails`.
- Unexpected exceptions → 500 ProblemDetails with `traceId`, no exception message or stack trace.
- **No try/catch in controllers.** No raw `ex.Message` returned to clients.

**Implementation:** implemented and tested. `ApiControllerBase` (`OkOrProblem`, `NoContentOrProblem`, `CreatedOrProblem`, `ProblemFrom`) is unit-tested with the MVC `ProblemDetailsFactory`; no business controllers use it yet.

## AD-16 Logging (Approved)

`ILogger<T>` everywhere, with structured message templates. Use the built-in Microsoft logging providers at this stage; do not add Serilog. Security events (failed login, lockout, token reuse, authorization failure) are logged at Warning. Never log passwords, keys, tokens, connection strings, plaintext credentials, or sensitive encryption payloads. `traceId` correlates API logs, ProblemDetails and the frontend `ApiException`. A different logging sink may be considered later as a separate decision.

**Implementation:**
- Console output is JSON with scopes and UTC timestamps by default, and simple single-line in Development.
- HTTP logging records method, path (no query string), status and duration only.
- Stored Procedure parameter values are never logged.

## AD-17 Configuration (Proposed)

- Options classes with `AddOptions<T>().Bind(section).ValidateDataAnnotations().ValidateOnStart()`.
- `appsettings.json` holds non-secret values only. Secrets come from **user-secrets** (Development) and **environment variables / secret store** (other environments). Farm FMS has its own names: `ConnectionStrings:FarmFMS`, `Database`, `SystemUser`, and `Jwt`/`Encryption` sections to be defined.
- **Nothing is copied from RouteNexa configuration** (its connection string, JWT key and Google Maps keys are committed in plain text and should be considered exposed).

**Implementation:**
- `DatabaseOptions` and `SystemUserOptions` are validated on start; a missing or blank connection string fails at startup (tested).
- User-secrets: the API project has a `UserSecretsId`. The committed `appsettings.json` has an empty `ConnectionStrings:FarmFMS`. The real value comes from user-secrets (Development) or the `ConnectionStrings__FarmFMS` environment variable.
- An architecture test fails if any committed `appsettings*.json` contains SQL credentials or a non-empty password / secret / key / token value.
- The unit tests set a placeholder connection string through an environment variable, so they never use a developer's database.
- **Open:** earlier commits (from `5ffe3f2`) still contain a SQL credential in history. Rewriting history and rotating the credential need owner approval.

## AD-18 API conventions (Proposed)

- Routes: lower-case kebab-case `api/<module>/<resource>` (e.g. `api/setup/farms`); `{id:int}` constraint (primary keys are `INT IDENTITY(1,1)` per the specification).
- `GET` (all), `GET active`, `GET {id}`, `POST` → `201 CreatedAtAction` with `{ id }`, `PUT {id}` → `204`, deactivate via an explicit endpoint or `IsActive` per spec (RouteNexa disabled hard DELETE).
- Paging/filtering/sorting for large lists is done **in stored procedures** (parameters), not in memory, where data volume warrants it. Lookup lists keep client-side grid filtering as in RouteNexa.
- Built-in OpenAPI (`AddOpenApi`/`MapOpenApi`, Development only) is the **only** OpenAPI document generator. Swashbuckle is not used for document generation.
- **Approved exception (M3, approved by the project owner, 2026-11-05):** Swagger UI is provided by `Swashbuckle.AspNetCore.SwaggerUI`, used **only as the UI**.
  - It renders the built-in document at `/openapi/v1.json`.
  - It is served at `/swagger`, in Development only, like the document.
  - No other Swashbuckle package (e.g. `Swashbuckle.AspNetCore.SwaggerGen`, `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`) may be referenced.
  - Replacing the built-in generator with Swashbuckle generation needs a new decision.
- `CancellationToken` on every action, passed to Dapper.

**Implementation:**
- `ApiControllerBase.CreatedOrProblem` returns `201 CreatedAtAction` with body `CreatedResponse { id }`.
- The built-in OpenAPI document is mapped in Development only and includes the documented health endpoints.
- Swagger UI per the approved exception above: `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1 in `FarmFMS.API`, configured in `Program.cs` (`UseSwaggerUI` → `/openapi/v1.json`, route prefix `swagger`, Development only).
- Verified by tests:
  - The API references only `Swashbuckle.AspNetCore.SwaggerUI` from Swashbuckle, together with `Microsoft.AspNetCore.OpenApi` (architecture test).
  - In Development, the built-in document is served and Swagger UI loads it.
  - Swashbuckle's own generator endpoint (`/swagger/v1/swagger.json`) does not exist.
  - In Production, neither the document nor the UI is exposed (unit tests).

## AD-19 Frontend platform (Mandatory / Proposed)

- Blazor Web App, .NET 10, global Interactive Server (Mandatory, scaffolded).
- Radzen for DataGrid, forms, dialogs, notifications and the layout shell; Bootstrap for grid and utilities.
- **Proposed:** keep the Farm FMS `material-base.css` theme (rather than RouteNexa's full `material` theme plus three overlapping CSS files); one `wwwroot/css/farmfms-theme.css` exposing CSS variables consumed by component-scoped CSS.
- Wrap `@Body` in an `ErrorBoundary`.

## AD-20 Frontend HTTP layer (Proposed)

Port RouteNexa's `Features/Common/Http` (ApiException, ApiJson, HttpClientExtensions, HttpResponseMessageExtensions, QueryString) into Farm FMS, adding `traceId`. Every typed client calls the API only through these helpers. Registration via the existing `AddBackendApiClients` with a helper that **always** attaches the bearer handler (RouteNexa forgot it for one client).

## AD-21 Frontend authentication state and token storage (Approved direction)

Port the architecture: anonymous auth client, `CircuitServicesAccessor` + `CircuitHandler`, `BearerTokenHandler`, custom `AuthenticationStateProvider` with single-flight refresh, `@attribute [Authorize]` by default with `[AllowAnonymous]` on Login.
Fixes: one matching token contract, UTC comparisons, no `Console.WriteLine`, a `RedirectToLogin` component with `returnUrl`, and persisted Data-Protection keys outside Development.

**Approved token-storage direction:** keep access and refresh tokens on the server side; do not store authentication tokens in browser `localStorage` or `sessionStorage`. Use a protected `HttpOnly`, `Secure`, appropriately configured `SameSite` session cookie for the browser session identifier. The cookie must not contain the raw API access or refresh tokens. The server-side session/token store owns those tokens. Keep the API JWT design compatible with future mobile clients.

**Approved idle timeout:** 30 minutes (matches the specification's session timeout). Enforce the timeout server-side and invalidate the associated session/token state on expiry. The exact re-authentication UX can be implemented with the frontend foundation.

## AD-22 Frontend CRUD toolkit (Proposed)

Port `CrudFormBase<TRequest>`, `ICrudForm`, `CrudFormType` (+ extensions), `DialogServiceExtensions` (main overload only), `PageHeader`, `DataGridExtensions`, `NotificationServiceExtensions`, `TooltipServiceExtensions`, `AuditInfo` and `AuditableDto`, de-branded, plus load/save hooks and server-validation mapping in `CrudFormBase`. The FuelType list and form pages are the template for every Farm FMS master screen.

## AD-23 Testing (Approved scope)

The agreed test scope includes architecture tests, application tests, infrastructure integration tests, and API/frontend contract tests. Source checks must detect prohibited SQL even when SQL text is split across lines inside multi-line string literals.

**Integration-test limitation:** live Dapper execution and translation of actual `SqlException` instances remain unverified until a dedicated SQL Server test database is provisioned. Do not use production data. Keep the integration test project/test cases documented as pending if the database is not yet available; do not claim live database behavior is verified.

**Implementation:**
- `Backend/tests/FarmFMS.ArchitectureTests` (implemented):
  - layers and data-access packages;
  - `DbContext` usage;
  - EF model rules (FKs, navigations, `dbo`, AD-08 names and uniqueness, the 40 Module 01 names);
  - Roslyn-based inline-SQL detection, including multi-line strings;
  - migrations and SQL scripts;
  - procedure naming against the specification workbook.

  Every rule is proven against known-bad samples.
- `Backend/tests/FarmFMS.ArchitectureTests` also checks (Step 02.1) that committed `appsettings*.json` files contain no credentials (AD-17).
- `Backend/tests/FarmFMS.UnitTests` (implemented; covers Application and Infrastructure units and the in-memory API host): Result model, procedure names, error translation and catalogue (including per-module ranges), unit of work, executor guard, output-parameter contract (fake commands), audit actor, audit base types, string / nullable helpers, naming builder, exception handler, `ApiControllerBase`, current user, options validation, health checks, OpenAPI / Swagger UI exposure.
- `FarmFMS.Infrastructure.IntegrationTests`: **Blocked** (no SQL Server test database). **Live SQL Server behaviour is not verified.**
- API/frontend contract tests: not implemented.

**Known limitation:** SQL assembled at run time (e.g. `StringBuilder.Append`) or loaded from files/resources is not detected statically.

## AD-24 Schema (Approved)

- `dbo` is the default and only schema (`FarmFmsDbContext` sets `HasDefaultSchema("dbo")`; entity configurations must not set another schema).
- Stored Procedure references are always **dbo-qualified** (`dbo.ScrAppUser`). The executor rejects unqualified names and any other schema; no other schema is ever inferred.

**Implementation:** tests check that the EF default schema is `dbo`, that every table is in `dbo`, and that procedure constants and scripts are dbo-qualified.

---

## Remaining items requiring input or verification

1. **Pending – specification text:** the specification's *Read Me* sheet still says "maximum 15 characters" under Naming, which contradicts the approved AD-08 decision. The specification owner should update it. Table, column and procedure names in the specification are unaffected.
2. **To confirm – naming details:**
   - (a) Multiple columns are concatenated in key order without a separator, following the PMS index example (`IXLoginHistory_UserIdEventDate`). This is deterministic, and the architecture tests reject any collision in the model. Please confirm, or choose a separator (e.g. `_`) between columns.
   - (b) The specification lists both a unique constraint on `RoleMenuAccess (RoleId, MenuId, PermissionId)` and an index on `RoleMenuAccess (RoleId, MenuId)`. The index repeats the leading columns of the unique constraint's index and may be redundant. This is noted only; the specification is not changed.
3. **Pending – spec consistency:** the *Relationships* sheet names `UserHierarchy.ManagerAccountId`, while the *UserHierarchy* sheet and the ERD name the column `ManagerId`.
4. **Blocked – AD-14:** the `SYSTEM` user record and its lookup Stored Procedure are not in the Module 01 specification.
5. **Resolved (Step 02.1) – AD-09:** the output-parameter contract (`@NewId`, `@RowsAffected`) and the per-module error ranges are implemented and tested.
6. **Pending – AD-03:** the API folder name (`Common/` implemented vs `Infrastructure/` documented).
7. **Pending approval – AD-11:** `Password_Encryption_Design.md`, including key custody and the reversible-password rationale.
8. **Blocked – AD-23:** a dedicated non-production SQL Server integration-test database. Live Dapper and real `SqlException` behaviour remain unverified.
9. **Resolved:**
   - The repository contains the Module 01 specification v0.2. (The PMS naming standard was later removed from the repository; see item 12.)
   - The primary-key type is `INT IDENTITY(1,1)` on all tables (specification).
   - Module 01 object names: all 40 keys, constraints and indexes have full descriptive, unique names (`Database_Conventions.md` §3.3). The earlier blocked-name list was withdrawn with the 15-character limit.
10. **Resolved – AD-18 (M3):** the project owner approved keeping Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`, UI only, built-in OpenAPI generator retained). It is recorded as an approved exception in AD-18.
11. **Pending approval – AD-17 (M2):** a SQL credential remains in Git history (commits from `5ffe3f2`); rewriting history and rotating the credential need owner approval. Nothing has been pushed (the remote has no branches).
12. **Missing input document:** the PMS SQL Server naming standard (`SQL Server Best Practices - Prima Management Services Pvt Ltd.pptx`, listed under Inputs) is no longer in the repository. Restore it, or update the reference.

**Step gate:** Step 03 (Frontend Foundation) must not start until the updated Step 02 implementation report has been reviewed and Step 02 is explicitly approved.
