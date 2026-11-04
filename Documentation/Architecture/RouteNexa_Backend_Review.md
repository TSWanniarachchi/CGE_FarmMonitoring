# RouteNexa Backend – Reference Review

| | |
|---|---|
| Step | 01 – Reference Analysis |
| Reference | RouteNexa API (`backend.zip`) |
| Reviewed | 2026-11-04 |
| Purpose | Identify reusable patterns for the Farm FMS Backend. No RouteNexa code, schema, entities, business logic, credentials or keys are carried over. |

> Paths in this document are relative to the root of the RouteNexa backend archive (`backend/`).
> RouteNexa was reviewed read-only from an extracted copy; the original archive was not modified.

---

## 1. Solution and project structure

```
RouteNexa.slnx
  RouteNexa.Domain          (no references, no packages)
  RouteNexa.Application     -> Domain          (BCrypt.Net-Next, Options, IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt)
  RouteNexa.Infrastructure  -> Application, Domain   (Dapper, EF Core, EF Core SqlServer, EF Core Design)
  RouteNexa.Api             -> Application, Infrastructure   (JwtBearer, OpenApi, Swashbuckle)
```

All projects target `net10.0` with `Nullable` and `ImplicitUsings` enabled.

Folder convention (applied consistently):

| Layer | Folder pattern | Example |
|---|---|---|
| Domain | `Entities/<Area>/<Feature>/` + `Common/` | `Entities/Master/Customers/Customer.cs` |
| Application | `Features/<Area>/<Feature>/` (DTOs, requests, service) and `Interfaces/<Area>/<Feature>/` (repository + service interfaces) | `Features/Master/Vehicles/VehicleService.cs` |
| Infrastructure | `Repositories/<Area>/<Feature>/`, `Persistence/`, `Persistence/Dapper/` | `Repositories/Master/Vehicles/VehicleRepository.cs` |
| API | `Controllers/<Area>/<Feature>/`, `Services/` | `Controllers/Master/Vehicles/VehicleController.cs` |

**Assessment:** the four-project split and dependency direction are correct and match the Farm FMS mandate. The `<Area>/<Feature>` folder layout is clear and should be reused.

**Issues:**
- Application holds JWT/BCrypt packages and the concrete `JwtTokenService` / `PasswordHashService`. These are infrastructure concerns (crypto, token formats) and pull framework packages into Application.
- One file name contains a space (`Controllers/Master/Depots/DepotController .cs`).
- `RouteNexa.Api.csproj` has `Compile Remove` entries for a deleted folder (`Controllers\Master\Sales\**`), which is leftover cleanup.
- There are no test projects.

---

## 2. Program.cs (`RouteNexa.Api/Program.cs`)

What it does, in order:
1. `AddControllers`, `AddEndpointsApiExplorer`, `AddSwaggerGen`.
2. `AddHttpContextAccessor` + `ICurrentUserService` → `CurrentUserService`.
3. Registers **every Application service by hand** (`AddScoped<IVehicleService, VehicleService>()` …).
4. `AddInfrastructure(configuration)`.
5. Binds `JwtSettings` and **fails fast** if `Jwt:Key` is missing or shorter than 32 characters.
6. JWT bearer authentication: validates issuer, audience, lifetime (30 s clock skew) and signing key. `NameClaimType = "name"`, `RoleClaimType = "role"`.
7. `AddAuthorization()` and a fixed-window rate limiter `"login"` (10 per minute).
8. Runs `AuthSeeder` at startup (creates the first admin only when the user table is empty and a seed password is configured).
9. Pipeline: Swagger (Development) → HTTPS redirect → rate limiter → authentication → authorization → `MapControllers().RequireAuthorization()`.

**Reuse:**
- Fail-fast validation of security configuration at startup.
- **Secure by default:** `MapControllers().RequireAuthorization()`, with `[AllowAnonymous]` only on login, refresh and logout.
- Rate limiting on the login endpoint.
- Pipeline order (rate limiter → authentication → authorization).

**Redesign:**
- Application services are registered in `Program.cs` rather than in an Application-layer `AddApplication()`. Farm FMS already has `FarmFMS.Application/DependencyInjection.cs` and should register there.
- Both Swashbuckle and `Microsoft.AspNetCore.OpenApi` are referenced. Farm FMS uses the built-in `AddOpenApi()`/`MapOpenApi()` only.
- No global exception handler, no `AddProblemDetails()`, no health checks, no CORS policy, no structured logging. See §11–§12.
- Seeding at startup runs on every boot and depends on a password in configuration. Farm FMS should seed the first administrator through a deployment script or stored procedure, per the approved specification.

---

## 3. DependencyInjection.cs (`RouteNexa.Infrastructure/DependencyInjection.cs`)

What it does:
- `AddDbContext<AppDbContext>` with `UseSqlServer(GetConnectionString("DefaultConnection"))`.
- `AddScoped<DapperExecutor>()`.
- Registers every repository by hand, plus `IJwtTokenService` (scoped) and `IPasswordHashService` (singleton). The concrete types live in Application but are registered from Infrastructure.

**Reuse:** one extension method per layer (`AddInfrastructure(IServiceCollection, IConfiguration)`), and grouping registrations by area with comments.

**Redesign:**
- The connection string is not validated. A missing value fails late, on first DB use. Farm FMS already throws at startup (`FarmFMS.Infrastructure/DependencyInjection.cs`). Keep that.
- Registrations for types defined in another layer (Auth services) are confusing. Each layer should register its own types.
- Consider one small `Add<Module>Infrastructure()` per module once modules grow, so `DependencyInjection.cs` doesn't become a long flat list.

---

## 4. DapperExecutor.cs (`RouteNexa.Infrastructure/Persistence/Dapper/DapperExecutor.cs`)

What it does: a sealed, scoped wrapper around Dapper that **always** calls Stored Procedures (`CommandType.StoredProcedure`). It has five methods:

| Method | Use |
|---|---|
| `QueryAsync<T>` | lists / search (returns `IReadOnlyList<T>`) |
| `QuerySingleOrDefaultAsync<T>` | get by id |
| `QueryMultipleAsync<T1,T2>` | header + detail (two result sets) |
| `ExecuteAsync` | commands returning affected rows |
| `ExecuteScalarAsync<T>` | insert returning new id / scalar result |

Each method validates the procedure name, builds a `CommandDefinition` with the `CancellationToken`, and enlists in the ambient EF transaction (`_context.Database.CurrentTransaction?.GetDbTransaction()`).

**Why it's useful:**
- It is the single place where "Stored Procedures only" is enforced. Repositories can't pass inline SQL because `CommandType.StoredProcedure` is fixed.
- It gives a uniform, cancellable API, so repositories become thin parameter-mapping classes.

**Must be redesigned for Farm FMS:**
1. **It borrows the connection from `AppDbContext`** (`_context.Database.GetDbConnection()`). This couples application data access to EF Core at runtime. Farm FMS rules say EF Core is for schema and migrations only. The Farm FMS executor must get connections from the existing `IDbConnectionFactory` (`FarmFMS.Infrastructure/Data/SqlConnectionFactory.cs`), and `FarmFmsDbContext` must never be injected into repositories.
2. **Transactions:** RouteNexa piggybacks on EF transactions. Farm FMS needs its own scoped unit of work (connection + `DbTransaction`) that the executor enlists in, for multi-procedure business operations. Most operations should be atomic inside a single stored procedure anyway.
3. **Duplication:** the `CommandDefinition` construction is repeated five times. Use one private `BuildCommand(...)` helper.
4. **Missing features:** `commandTimeout`, generic N-result-set reads (callback-based `QueryMultipleAsync`), output/return-value parameter support (`DynamicParameters` with `ParameterDirection.Output`/`ReturnValue`), and table-valued parameters for bulk operations.
5. **Error translation:** `SqlException`s propagate raw. Farm FMS should map SQL error numbers it raises deliberately (`THROW 50xxx`) to typed application exceptions (validation, conflict, not found). See §11.

---

## 5. AppDbContext.cs (`RouteNexa.Infrastructure/Persistence/AppDbContext.cs`)

What it does: declares three `DbSet`s (Vehicles, VehicleTypes, FuelTypes). `OnModelCreating`/`ApplyConfigurationsFromAssembly` is **commented out**. It has an unused `using Microsoft.VisualBasic.FileIO;`. There is no `Migrations` folder in the archive, and comments in request models refer to `database/tables/master/*.sql`. The schema was therefore created by hand-written SQL scripts, not by EF Code First.

**Assessment:** **Not reusable.** RouteNexa doesn't actually use EF Core Code First, and the DbContext's only runtime role is to supply a connection to Dapper. Farm FMS needs the opposite:
- `FarmFmsDbContext` (already scaffolded) owns the schema through `IEntityTypeConfiguration<T>` classes and migrations.
- It is never resolved by repositories.
- No navigation properties → no FK constraints are generated (see Architecture Decisions AD-07).

---

## 6. Repository interfaces and implementations

Interfaces live in `RouteNexa.Application/Interfaces/<Area>/<Feature>/I<Feature>Repository.cs`. Implementations live in `RouteNexa.Infrastructure/Repositories/<Area>/<Feature>/`.

Typical implementation (`Repositories/Master/Customers/CustomerRepository.cs`):
- Constructor-injects `DapperExecutor` with a null guard.
- One method per stored procedure. Procedure name as a local `const string storedProcedureName = "[master].[Customer_GetAll]"`.
- Parameters built with `DynamicParameters`, sometimes with an explicit `DbType`.
- `CancellationToken` passed through.

**Reuse:** thin repository, one method per procedure, `DynamicParameters`, cancellation, `const` procedure name.

**Redesign / inconsistencies found:**

| Issue | Where | Farm FMS rule |
|---|---|---|
| Some repositories return **Application DTOs** (`IReadOnlyList<VehicleDto>`), so the Infrastructure layer shapes API contracts | `VehicleRepository`, `IVehicleRepository` | Repositories return Domain entities or dedicated read models defined in Application (`…ReadModel`), never API DTOs. |
| Update and Delete use `ExecuteScalarAsync<int>` and rely on the procedure `SELECT`ing a row count | `CustomerRepository.UpdateAsync/DeleteAsync` | Define one result contract for every write procedure (see AD-09) and use it consistently. |
| Parameter name style mixed: `"Id"` vs `"@Id"` | `CustomerRepository` vs `VehicleRepository` | Pick one (no `@` prefix) and centralise procedure names in a static class per module. |
| Procedure names use underscores and non-PMS forms (e.g. `Customer_GetAllActive`) and RouteNexa schemas (`master`, `auth`) | all | Use the approved Farm FMS naming conventions (dbo, Scr/Sre/Sup/Sde/Sva prefixes, descriptive names; see `FarmFMS_Architecture_Decisions_v2.md` AD-08), names taken from the approved Module 01 specification. |
| Read of user + roles needs two round-trips | `UserRepository.WithRolesAsync` | Use one procedure with two result sets plus the multi-result executor method. |
| Domain entities carry display-only join columns (`Customer.SalesChannelName`, commented "Used by Dapper for display") | `Domain/Entities/Master/Customers/Customer.cs` | Keep Domain entities persistence-pure. Joined display data belongs in read models. |

---

## 7. Application services

Location: `RouteNexa.Application/Features/<Area>/<Feature>/<Feature>Service.cs`, behind `I<Feature>Service`.

Two generations of style coexist:

| Older style (Customer, Depot, VehicleType, SalesChannel, ProductMaster) | Newer style (Vehicle, FuelType, ProductChannel) |
|---|---|
| Client sends `CreatedBy` / `ModifiedBy` in the request body, or the value is hard-coded as `"System"` | `ICurrentUserService.UserName` sets audit fields on the server |
| Manual `.Trim()` | `StringExtensions` (`ToCode`, `TrimToNull`, `CollapseWhitespace`) and `NullableExtensions.Required()` |
| Large inline DTO mapping blocks duplicated 3× per service | Repository returns ready DTO (coupling issue, see §6) |
| `KeyNotFoundException` thrown for missing rows | Returns `int` affected rows / `null` |

**Reuse (newer style):**
- **Server-side audit identity** via `ICurrentUserService` (`Application/Common/Interfaces/ICurrentUserService.cs`, implemented in `Api/Services/CurrentUserService.cs`). Clients must never send audit fields.
- Normalisation helpers in `Application/Common/Extensions/StringExtensions.cs` and `NullableExtensions.cs` (`Required()` with `CallerArgumentExpression`).
- Cross-field business validation in the service (e.g. min ≤ max), with field validation left to Data Annotations.
- XML doc comments and the file header block (Description / Author / Created Date).

**Redesign:**
- Replace exception-free `null`/`0` signalling and mixed exception types with **one result model** (e.g. `Result<T>` with error codes `Validation`, `NotFound`, `Conflict`, `Forbidden`). Controllers then map these uniformly.
- Use a dedicated mapping helper or extension per feature (`ToDto()`) instead of duplicated object initialisers.
- Services shouldn't throw `ArgumentException` for user-input errors. Reserve `Argument*` exceptions for programmer errors.

---

## 8. DTOs

- Response DTOs: `<Feature>Dto` (flat, includes audit fields). `Application/Common/Dtos/AuditableDto.cs` exists but most DTOs repeat the audit fields instead of inheriting from it.
- Request DTOs: separate `<Feature>RequestCreate` and `<Feature>RequestUpdate` with Data Annotations (`[Required]`, `[StringLength]`, `[Range]`, explicit messages). Lengths are commented as matching the table scripts.
- Value-type fields use nullable types plus `[Required]` (`bool? IsActive`, `int? VehicleTypeId`), so a missing JSON field fails validation instead of silently defaulting to `false`/`0`. **This is a good pattern; reuse it.**

**Redesign:** inherit audit fields from a single base DTO; remove client-supplied `CreatedBy`/`ModifiedBy` from all requests; keep request lengths in sync with the EF configuration (single source: a `FieldLengths` constants class per module that both EF configuration and DTO attributes reference).

---

## 9. Controllers

- `[ApiController]`, constructor-injected service, `CancellationToken` on every action.
- `GET` all, `GET active`, `GET {id:int}`, `POST` → `CreatedAtAction(nameof(GetById), new { id }, new { id })`, `PUT {id:int}` → `204 NoContent` or `404`.
- DELETE endpoints are commented out (soft delete via `IsActive` instead).

**Inconsistencies:**
- Routes: `api/[controller]` (PascalCase class-derived) vs explicit kebab-case `api/fuel-types`, `api/vehicles`, `api/sales/invoices`.
- Error bodies: `BadRequest("text")`, `BadRequest(new { message })`, `NotFound()`, `NotFound(new { message })`, `Unauthorized(new { error })`. The frontend has to parse all of these shapes.
- Newer controllers wrap **every action** in `try/catch (ArgumentException)`, which repeats code that belongs in middleware.
- Class names drift (`VehicleController` vs `CustomersController`, `VehicleTypesController`).

**Farm FMS:** explicit kebab-case, versionable routes (`api/<module>/<resource>`); **RFC 7807 ProblemDetails** for every error; no try/catch in controllers (global handler + result mapping); one naming rule (`<Resource>Controller`, plural resource in route).

---

## 10. Authentication and authorization

Files: `Api/Controllers/Auth/AuthController.cs`, `Application/Features/Auth/*`, `Infrastructure/Repositories/Auth/*`, `Domain/Entities/Auth/*`.

What it does:
- `POST api/auth/login` (anonymous, rate-limited) → validates credentials, checks `IsActive` and lockout, records success/failure, writes a login audit row, issues a JWT access token (15 min) and an opaque refresh token (64 random bytes, **SHA-256 hash stored**, 7 days).
- `POST api/auth/refresh` (anonymous) → **refresh-token rotation with reuse detection**: if a revoked token is presented again, all sessions of that user are revoked.
- `POST api/auth/logout` → revokes the refresh token.
- `GET api/auth/me` → returns name, full name and roles from claims.
- Generic error message for unknown user, inactive user and wrong password (no user enumeration). There is a separate message for a locked account.
- Passwords: **BCrypt work factor 12** (`PasswordHashService`).
- Claims: `sub`, `name`, `full_name`, `role`.

**Reuse (patterns, not code):** login rate limiting; generic credential errors; lockout and failure counting done in stored procedures; login audit trail; refresh-token rotation, reuse detection and hashed storage; anonymous logout (the access token may already be expired); claims built in one place; fail-fast key validation.

**Must be redesigned:**
- **Password protection:** RouteNexa uses BCrypt hashing. Farm FMS must implement the **approved AES-256-GCM password encryption design** (see Architecture Decisions AD-11). The BCrypt service is not carried over.
- **Time handling bug:** `JwtTokenService` and `AuthService` use `DateTime.Now` (server local time), but the response property is named `AccessTokenExpiresUtc`. Lockout and refresh expiry comparisons also use local time. Farm FMS must use `TimeProvider` / UTC throughout, and the DB should store UTC (`datetime2`, `SYSUTCDATETIME()`).
- **Contract mismatch (bug):** the backend returns `AccessTokenExpiresUtc`, but the RouteNexa Web client expects `AccessTokenExpiresDateTime`. The client value always deserialises to `DateTime.MinValue`, so the client treats every token as expired and calls refresh before every API request. See the Frontend review §6.
- **Authorization model:** only "authenticated user" is enforced. Roles are issued but no policy or `[Authorize(Roles/Policy)]` is used anywhere. Farm FMS needs named policies or permissions per its approved security requirements.
- Crypto and token services live in Application. Move them to Infrastructure behind Application interfaces.
- `JwtSecurityTokenHandler` is legacy. Use `JsonWebTokenHandler` (Microsoft.IdentityModel.JsonWebTokens).
- Refresh tokens are not bound to a client or device, and there is no maximum session lifetime (absolute expiry). Decide per spec.

---

## 11. Exception handling

Current state:
- **No global exception handling** (`UseExceptionHandler`, `IExceptionHandler`, `AddProblemDetails` are all absent).
- Errors are handled per action with `try/catch (ArgumentException)` → `BadRequest(new { message = ex.Message })`. **Exception messages are sent directly to the client.**
- `KeyNotFoundException` → 404 in one controller. `InvalidOperationException` and `SqlException` (duplicate keys, deadlocks, timeouts) fall through as 500s, with the developer exception page in Development.
- The only consistent error format is the `[ApiController]` automatic `ValidationProblemDetails` for model validation.

**Farm FMS redesign (mandatory):**
1. `AddProblemDetails()` + a global `IExceptionHandler` that logs the exception with a correlation/trace id and returns RFC 7807 responses without internal details.
2. Typed application exceptions or a result model (`ValidationException`, `NotFoundException`, `ConflictException`, `ForbiddenException`) mapped to 400 / 404 / 409 / 403.
3. A `SqlException` translator in Infrastructure for deliberate business errors raised by stored procedures (agreed error-number range) and known constraint violations (unique index 2601/2627 → 409).
4. Controllers contain no try/catch.

---

## 12. Logging

Current state: default ASP.NET Core console logging configured in `appsettings.json` (`Default: Information`, `Microsoft.AspNetCore: Warning`). **No `ILogger<T>` is injected anywhere** in the backend: no security-event logging, no request logging, no correlation ids. The login audit table is the only audit trail.

**Farm FMS:** inject `ILogger<T>` in services and handlers; log security events (login failure, lockout, token reuse) at Warning; never log passwords, tokens or keys; use `UseHttpLogging`/request logging with redaction or Serilog per organisation standard (decision pending); correlate with `Activity.TraceId`, which is also returned in ProblemDetails.

---

## 13. Configuration

- `appsettings.json` contains sections `ConnectionStrings:DefaultConnection`, `Jwt` (Key, Issuer, Audience, AccessTokenMinutes, RefreshTokenDays) and `Seed` (a **commented-out sample admin password**).
- **Security finding:** `appsettings.json` contains a real connection string and a JWT signing key. The values were not copied into this review. They should be treated as exposed in RouteNexa and rotated by its owners.
- `JwtSettings` is bound with `Configure<JwtSettings>` and read a second time with `Get<JwtSettings>()` for the bearer setup.

**Farm FMS:** strongly typed options with `AddOptions<T>().Bind().ValidateDataAnnotations().ValidateOnStart()`; secrets only in user-secrets (dev) or environment variables / a secret store (other environments); `appsettings.json` holds non-secret placeholders only; no sample passwords in configuration files.

---

## 14. Summary: reuse vs redesign

| Area | Reuse | Redesign | Do not copy |
|---|---|---|---|
| Layering, folders | ✔ dependency direction, `<Area>/<Feature>` folders | Move crypto/JWT to Infrastructure | — |
| Program.cs | ✔ fail-fast config, `RequireAuthorization()` default, login rate limit | Register services in `AddApplication()`, single OpenAPI stack, add ProblemDetails/health/logging | Seed password in config |
| DapperExecutor | ✔ "Stored Procedures only" wrapper idea and method set | Connection from `IDbConnectionFactory`, own unit of work, error translation, output params, timeout | EF-borrowed connection |
| AppDbContext | — | Real Code First configurations and migrations, no FKs | RouteNexa DbSets/entities |
| Repositories | ✔ thin, one method per procedure, `DynamicParameters`, cancellation | Return entities/read models, standard write result, approved naming conventions (AD-08), centralised procedure-name constants | RouteNexa procedure names, schemas, tables |
| Services | ✔ `ICurrentUserService`, String/Nullable extensions, cross-field validation | Result model, mapping helpers | Client-sent audit fields, `"System"` literals |
| DTOs | ✔ Create/Update requests, nullable + `[Required]` | Audit base DTO, shared length constants | RouteNexa DTO shapes |
| Controllers | ✔ CancellationToken, `CreatedAtAction`, `{id:int}` | Kebab-case routes, ProblemDetails, no try/catch | Ad-hoc error bodies |
| Auth | ✔ rotation + reuse detection, hashed refresh tokens, lockout, audit, generic errors | AES-256-GCM per spec, UTC, policies, Infrastructure placement | BCrypt service, JWT key, claims names unless agreed |
| Errors/Logging | — | Global handler, typed errors, `ILogger<T>`, correlation | Raw `ex.Message` to client |
