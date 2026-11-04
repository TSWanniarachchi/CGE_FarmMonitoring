# Farm FMS – Reference Mapping (RouteNexa → Farm FMS)

| | |
|---|---|
| Step | 01 – Reference Analysis |
| Status | **Proposed**: awaiting approval before Step 02 |
| Date | 2026-11-04 |

This document maps every reusable RouteNexa pattern to its Farm FMS target. For each pattern it lists: **Source** (RouteNexa location), **What it does**, **Why it is useful**, **Farm FMS adaptation** (target location and form), and **Required changes**.

RouteNexa paths are relative to the archive roots (`backend/`, `frontend/`). Farm FMS paths are relative to `FarmFMS/`. Target paths are **planned**; nothing has been created yet. Decisions referenced as AD-xx are in `FarmFMS_Architecture_Decisions_v2.md` (the authoritative record; the original `FarmFMS_Architecture_Decisions.md` is superseded).

Rule for every row: **patterns are re-implemented, not copied**. No RouteNexa entities, schema, procedure names, business logic, branding, credentials, connection strings, JWT keys or API keys are transferred.

---

## Part A – Backend

### B-01 Layered solution and dependency direction
- **Source:** `RouteNexa.slnx`, `*/RouteNexa.*.csproj`
- **What:** Domain ← Application ← Infrastructure ← Api, `net10.0`, nullable + implicit usings.
- **Why:** it matches the mandated Simple Clean Architecture and keeps business logic free of data-access code.
- **Adapt:** already in place in `Backend/FarmFMS.sln` (`FarmFMS.Domain/Application/Infrastructure/API`).
- **Changes:** keep crypto, JWT and SQL packages out of Application (RouteNexa has BCrypt and JWT packages there). Add an architecture test (AD-02, AD-23).

### B-02 Per-layer DI extension
- **Source:** `RouteNexa.Infrastructure/DependencyInjection.cs`
- **What:** `AddInfrastructure(IServiceCollection, IConfiguration)` registers the DbContext, the executor and the repositories.
- **Why:** it keeps `Program.cs` thin and keeps each layer in charge of its own registrations.
- **Adapt:** `Backend/src/FarmFMS.Infrastructure/DependencyInjection.cs` (exists) and `Backend/src/FarmFMS.Application/DependencyInjection.cs` (exists, empty).
- **Changes:** register Application services in `AddApplication()`, not in `Program.cs`. Keep Farm FMS's fail-fast connection-string check. Each layer registers only its own types. Optionally add a per-module `Add<Module>()` extension.

### B-03 Program.cs composition and security defaults
- **Source:** `RouteNexa.Api/Program.cs`
- **What:** JWT bearer setup with fail-fast key validation, `MapControllers().RequireAuthorization()`, the `"login"` fixed-window rate limiter, and pipeline order.
- **Why:** endpoints are secure by default, misconfiguration fails at startup, and brute-force attempts are throttled.
- **Adapt:** `Backend/src/FarmFMS.API/Program.cs` plus a `FarmFMS.API/Infrastructure/AuthenticationSetup.cs` extension.
- **Changes:** add `AddProblemDetails`, the exception handler, health checks (already present) and named authorization policies. Use built-in OpenAPI only (no Swashbuckle). No seeding from a configured password. Options are validated with `ValidateOnStart` (AD-12, AD-13, AD-15, AD-17).

### B-04 Stored-Procedure-only Dapper executor
- **Source:** `RouteNexa.Infrastructure/Persistence/Dapper/DapperExecutor.cs`
- **What:** a scoped wrapper with `QueryAsync`, `QuerySingleOrDefaultAsync`, `QueryMultipleAsync`, `ExecuteAsync` and `ExecuteScalarAsync`; `CommandType.StoredProcedure` is fixed; supports cancellation and transaction enlistment.
- **Why:** it enforces "Stored Procedures only" in one place and gives repositories a small, uniform API.
- **Adapt:** `Backend/src/FarmFMS.Infrastructure/Data/StoredProcedureExecutor.cs` + `Data/UnitOfWork.cs` + `Data/SqlErrorTranslator.cs`.
- **Changes:**
  1. Get connections from `IDbConnectionFactory` (exists) and **never** from `FarmFmsDbContext`.
  2. Add an explicit unit of work for multi-procedure transactions.
  3. Build every command through one `BuildCommand` helper.
  4. Add `commandTimeout`, output/return parameters, table-valued parameters, and a callback-based N-result-set read.
  5. Translate `SqlException` into typed errors (AD-05, AD-09, AD-15).

### B-05 EF Core DbContext
- **Source:** `RouteNexa.Infrastructure/Persistence/AppDbContext.cs`
- **What:** three DbSets; configuration loading is commented out; there are no migrations.
- **Why:** it is useful only as a counter-example. RouteNexa doesn't really use Code First.
- **Adapt:** `Backend/src/FarmFMS.Infrastructure/Persistence/FarmFmsDbContext.cs` (exists, already calls `ApplyConfigurationsFromAssembly`) + `Persistence/Configurations/<Module>/*Configuration.cs` + `Persistence/Migrations/`.
- **Changes:** add explicit `ToTable`/`HasColumnName`/lengths/indexes per the Module 01 spec; no navigations or relationships (no FKs); never injected into repositories (AD-04, AD-07, AD-08).

### B-06 Repository interface + thin implementation
- **Source:** `RouteNexa.Application/Interfaces/Master/Customers/ICustomerRepository.cs`, `RouteNexa.Infrastructure/Repositories/Master/Customers/CustomerRepository.cs`
- **What:** one method per procedure, `DynamicParameters`, `CancellationToken`, null guard on the executor.
- **Why:** repositories stay small, readable and predictable.
- **Adapt:** interface in `FarmFMS.Application/<Module>/<Feature>/I<Feature>Repository.cs`; implementation in `FarmFMS.Infrastructure/Repositories/<Module>/<Feature>Repository.cs`; procedure names in `FarmFMS.Infrastructure/Repositories/<Module>/<Module>Procedures.cs`.
- **Changes:** return entities or read models, never DTOs; one write-result contract; consistent parameter naming (no `@`); descriptive procedure and parameter names per the approved naming conventions (AD-08; no custom length limit); one procedure with multiple result sets instead of chained round-trips (AD-08, AD-09, AD-10).

### B-07 Auditable entity base
- **Source:** `RouteNexa.Domain/Common/AuditableEntity.cs`
- **What:** `IsActive`, `CreatedBy`, `CreatedDateTime`, `ModifiedBy`, `ModifiedDateTime`.
- **Why:** it gives every master record uniform audit columns.
- **Adapt:** `FarmFMS.Domain/Common/AuditableEntity.cs` + a shared EF configuration helper for audit columns.
- **Changes:** the field set, types and names (user id vs name, UTC `datetime2`) must come from the Module 01 spec; column names as in the spec (AD-08).

### B-08 Current user service
- **Source:** `RouteNexa.Application/Common/Interfaces/ICurrentUserService.cs`, `RouteNexa.Api/Services/CurrentUserService.cs`
- **What:** reads the authenticated user from `HttpContext` and falls back to `"system"`.
- **Why:** audit fields are set on the server, so clients can't spoof them.
- **Adapt:** `FarmFMS.Application/Common/Interfaces/ICurrentUserService.cs`, `FarmFMS.API/Infrastructure/CurrentUserService.cs`.
- **Changes:** expose the user **id** (`sub`) as well as the name; no anonymous fallback for write operations (throw instead); remove every client-sent `CreatedBy`/`ModifiedBy` (AD-14).

### B-09 String and nullable normalisation helpers
- **Source:** `RouteNexa.Application/Common/Extensions/StringExtensions.cs`, `NullableExtensions.cs`
- **What:** `TrimToNull`, `TrimOrEmpty`, `CollapseWhitespace`, `ToCode`, `ToEmail`, `ToDigitsOnly`, `ToSearchTerm`, `Mask`, `Required()` with `CallerArgumentExpression`.
- **Why:** input normalisation is consistent and null-safe; `Mask` makes logging safe.
- **Adapt:** `FarmFMS.Application/Common/Extensions/`.
- **Changes:** re-implement the general-purpose helpers only. `Required()` should produce a validation `Result` error rather than `ArgumentException` when used for user input.

### B-10 Request DTO validation style
- **Source:** `RouteNexa.Application/Features/Master/Vehicles/VehicleRequestCreate.cs`
- **What:** separate Create/Update requests; Data Annotations with explicit messages; nullable value types + `[Required]` (`bool? IsActive`, `int? …Id` + `[Range(1, …)]`).
- **Why:** a missing JSON field is reported as an error instead of silently becoming `false`/`0`, and the messages are user-ready.
- **Adapt:** `FarmFMS.Application/<Module>/<Feature>/<Feature>CreateRequest.cs` / `UpdateRequest.cs`.
- **Changes:** no audit fields in requests; lengths come from a shared `<Module>FieldLengths` constants class that the EF configurations also use (single source of truth).

### B-11 Response DTO + audit base
- **Source:** `RouteNexa.Application/Common/Dtos/AuditableDto.cs`, `Features/*/…Dto.cs`
- **What:** flat response DTOs with audit info.
- **Why:** gives the frontend a predictable contract and drives the `AuditInfo` UI.
- **Adapt:** `FarmFMS.Application/Common/Dtos/AuditableDto.cs`; feature DTOs inherit from it.
- **Changes:** actually inherit (RouteNexa repeats the fields); UTC timestamps; mapping via `ToDto()` extensions instead of duplicated initialisers.

### B-12 Application service shape
- **Source:** `RouteNexa.Application/Features/Master/Vehicles/VehicleService.cs` (newer style)
- **What:** an interface plus implementation; null-guarded constructor; normalise input → build entity → call repository; cross-field rules in the service.
- **Why:** business rules stay in one testable place.
- **Adapt:** `FarmFMS.Application/<Module>/<Feature>/<Feature>Service.cs`.
- **Changes:** return `Result`/`Result<T>` instead of `null`/`0`/exceptions for expected outcomes; inject `ILogger<T>`; use `TimeProvider` for time.

### B-13 Controller shape
- **Source:** `RouteNexa.Api/Controllers/Master/Vehicles/VehicleController.cs`
- **What:** `[ApiController]`; GET all/active/by-id; POST → `CreatedAtAction`; PUT → 204/404; `CancellationToken`.
- **Why:** a predictable REST surface that matches the frontend service pattern.
- **Adapt:** `FarmFMS.API/Controllers/<Module>/<Resource>Controller.cs`.
- **Changes:** kebab-case `api/<module>/<resource>` routes; **no try/catch**; results mapped to ProblemDetails by a shared extension; `[Authorize(Policy = …)]` per action group (AD-13, AD-15, AD-18).

### B-14 Authentication flow
- **Source:** `RouteNexa.Api/Controllers/Auth/AuthController.cs`, `RouteNexa.Application/Features/Auth/AuthService.cs`, `RouteNexa.Infrastructure/Repositories/Auth/*`
- **What:** login (rate-limited, generic errors, lockout, audit), refresh with rotation and reuse detection, hashed refresh tokens, logout, `me`.
- **Why:** a sound session model that resists brute force, user enumeration and refresh-token theft.
- **Adapt:** `FarmFMS.API/Controllers/Auth/AuthController.cs`, `FarmFMS.Application/Auth/*` (interfaces, service, DTOs), `FarmFMS.Infrastructure/Security/*` (token service, password protector), `FarmFMS.Infrastructure/Repositories/Auth/*`.
- **Changes:** **AES-256-GCM password protection per the approved design (no BCrypt)**; key from a secret store with versioning; UTC everywhere; `JsonWebTokenHandler`; crypto in Infrastructure; claim names and token lifetimes per the Farm FMS security requirements; table and procedure names from the Module 01 spec (AD-11, AD-12).

### B-15 Startup configuration validation
- **Source:** `RouteNexa.Api/Program.cs` (Jwt:Key length check)
- **What:** throws at startup when a security setting is missing or too weak.
- **Why:** fails fast instead of failing insecurely at runtime.
- **Adapt:** options classes `JwtOptions`, `PasswordEncryptionOptions`, `DatabaseOptions` with `ValidateDataAnnotations().ValidateOnStart()` and custom validators (key length/format).
- **Changes:** generalise the single `if` into validated options for every security-relevant section (AD-17).

### Backend: not carried over
RouteNexa entities, DTO shapes, schemas (`master`, `auth`, …), table scripts, procedure names, business services (Customer, Depot, Vehicle, Plan, SalesInvoice, …), `AuthSeeder` (startup seeding from config), `PasswordHashService` (BCrypt), `JwtSettings` defaults, every value in `appsettings*.json`, Swashbuckle, ad-hoc error bodies, and per-action try/catch.

---

## Part B – Frontend

### F-01 Vertical-slice feature folders
- **Source:** `Features/<Area>/<Feature>/{Components,Models,Pages,Services}`
- **What:** each feature's form, models, list page and API client sit together.
- **Why:** features are easy to find and to add, and shared code is clearly separated in `Features/Common`.
- **Adapt:** `Frontend/src/FarmFMS.Web/Features/<Module>/<Feature>/…` and `Features/Common/…`.
- **Changes:** update `Frontend/CLAUDE.md` (which currently says `Components/Pages` + `Services/Api`) when approved; keep `Services/Api/` for options and registration only.

### F-02 HttpClient registration
- **Source:** `Extensions/ApiServiceCollectionExtensions.cs`
- **What:** a typed client per feature, base address, bearer handler.
- **Why:** gives each feature a typed client while the handler pipeline is managed centrally.
- **Adapt:** `Frontend/src/FarmFMS.Web/Services/Api/ApiClientServiceCollectionExtensions.cs` (exists; already validates `BackendApiOptions`, sets timeout).
- **Changes:** add a private `AddBackendClient<T>()` that applies `ConfigureBackendClient` **and** `BearerTokenHandler`, so no client can be registered without the handler (RouteNexa's `PlanApiService` was). Add a named anonymous client for auth endpoints. Resilience handler is optional (GET only).

### F-03 HTTP helpers and ApiException funnel
- **Source:** `Features/Common/Http/ApiException.cs`, `ApiJson.cs`, `HttpClientExtensions.cs`, `HttpResponseMessageExtensions.cs`, `QueryString.cs`
- **What:** short helpers for GET/POST/PUT/DELETE; every failure becomes one `ApiException` parsed from ProblemDetails or ValidationProblemDetails, with friendly status fallbacks; invariant query-string builder.
- **Why:** pages handle errors one way, and server validation messages reach the user.
- **Adapt:** `Frontend/src/FarmFMS.Web/Features/Common/Http/`.
- **Changes:** add `TraceId` to `ApiException`; keep the fallbacks; rename the namespace to `FarmFMS.Web.Features.Common.Http`.

### F-04 Typed API service
- **Source:** `Features/Master/FuelTypes/Services/FuelTypeApiService.cs`
- **What:** an `Endpoint` constant; `GetAll`/`GetActive`/`GetById` (null on 404), `Create`, `Update`; cancellation; uses only the F-03 helpers.
- **Why:** gives every resource the same small client.
- **Adapt:** `Features/<Module>/<Feature>/Services/<Feature>ApiService.cs`. The existing `Services/Api/SystemHealthApiClient.cs` stays as the health client.
- **Changes:** endpoints follow Farm FMS routes (AD-18). `CreateAsync` returns the new id from the `201` body.

### F-05 Authentication state and token handling
- **Source:** `Features/Authentication/RouteNexaAuthStateProvider.cs`, `BearerTokenHandler.cs` (incl. `CircuitServicesAccessor`, `ServicesAccessorCircuitHandler`), `AuthApiService.cs`, `Login.razor`, `Components/Routes.razor`, `_Imports.razor`
- **What:** a custom `AuthenticationStateProvider` with protected browser storage, single-flight refresh, prerender-safe loading; a bearer handler that reaches circuit-scoped services; an anonymous auth client; `[Authorize]` by default.
- **Why:** this is the correct, documented way to attach tokens from `IHttpClientFactory` handlers in Blazor Server, without refresh races.
- **Adapt:** `Features/Authentication/FarmFmsAuthStateProvider.cs`, `BearerTokenHandler.cs`, `CircuitServicesAccessor.cs`, `AuthApiService.cs`, `Login.razor`, `RedirectToLogin.razor`; `Components/Routes.razor` → `AuthorizeRouteView`.
- **Changes:**
  1. Match the backend token contract exactly (RouteNexa bug: `AccessTokenExpiresUtc` vs `AccessTokenExpiresDateTime`).
  2. Use UTC comparisons.
  3. Remove the `Console.WriteLine`; use `ILogger`.
  4. Use a `RedirectToLogin` component with `returnUrl` instead of navigating during render.
  5. Session vs local storage per security requirements.
  6. Persist Data-Protection keys outside Development.
  7. Use Farm FMS branding on Login.

### F-06 CrudFormBase and dialog contract
- **Source:** `Features/Common/Dialogs/CrudFormBase.cs`, `ICrudForm.cs`, `CrudFormType.cs`, `CrudFormTypeExtensions.cs`, `DialogServiceExtensions.cs`
- **What:** add/edit/view modes; required `FormType` enforcement; JSON-snapshot dirty tracking; confirm-on-discard; Esc handling; standard dialog options; returns `true` only after save.
- **Why:** every master form behaves the same way and users don't lose edits by accident.
- **Adapt:** `Features/Common/Dialogs/`.
- **Changes:** drop the legacy `idParameterName` overload; add `LoadAsync`/`SaveAsync` template hooks plus `IsLoading`/`IsSaving` handling in the base; map `ApiException.Errors` to form fields; make the key type follow the DB spec (generic `TKey` if not `int`).

### F-07 PageHeader
- **Source:** `Features/Common/Components/PageHeader/PageHeader.razor`, `.razor.css`, `PageHeaderBreadcrumb.cs`
- **What:** sets the page title and browser tab title, shows an optional description and an accessible, responsive breadcrumb.
- **Why:** gives every page a consistent header.
- **Adapt:** `Features/Common/Components/PageHeader/`.
- **Changes:** use an app-name option (not `"RouteNexa"`); use theme CSS variables instead of hex colours; fix the `.page-title` vs `main-text` class mismatch; align the namespace with the folder.

### F-08 DataGridExtensions
- **Source:** `Features/Common/Extensions/DataGridExtensions.cs`
- **What:** null-safe `HasActiveFilters`, `ClearAllFiltersAsync`, `HasActiveSorting`, `ClearSortingAsync`, `ResetAsync`, `GetFilteredCount`.
- **Why:** gives every grid the same filter and reset behaviour.
- **Adapt:** `Features/Common/Extensions/DataGridExtensions.cs`.
- **Changes:** namespace only.

### F-09 Notification and tooltip extensions
- **Source:** `Features/Common/Notifications/NotificationServiceExtensions.cs`, `Features/Common/Extensions/TooltipServiceExtensions.cs`
- **What:** severity helpers with standard durations; an exception overload that maps `ApiException`/network/timeout errors to friendly text; a one-line tooltip helper.
- **Why:** user messages look and read the same everywhere.
- **Adapt:** `Features/Common/Notifications/`, `Features/Common/Extensions/`.
- **Changes:** replace the `_ => ex.Message` fallback with a generic message (and log the exception); optionally append the `traceId`.

### F-10 AuditInfo + AuditableDto
- **Source:** `Features/Common/Components/AuditInfo.razor`, `Features/Common/Dtos/AuditableDto.cs`
- **What:** shows created/modified by and when in Edit/View forms.
- **Why:** gives users the audit trail without extra screens.
- **Adapt:** `Features/Common/Components/AuditInfo.razor`, `Features/Common/Dtos/AuditableDto.cs`.
- **Changes:** convert UTC to display time explicitly; the date format follows Farm FMS UI standards.

### F-11 List page template
- **Source:** `Features/Master/FuelTypes/Pages/FuelTypeList.razor`
- **What:** PageHeader + RadzenCard + DataGrid (filter, sort, page, reorder, resize), a toolbar (Refresh, Add), a three-state empty template, a status filter dropdown and chip, View/Edit actions with tooltips and aria-labels, and a Clear-filters cell.
- **Why:** gives Farm FMS a proven, accessible master-list layout.
- **Adapt:** template for `Features/<Module>/<Feature>/Pages/<Feature>List.razor`.
- **Changes:** remove the no-op `catch (Exception) { throw; }`; server-side paging through stored-procedure parameters where volumes require it; policy-based visibility for the Add/Edit buttons.

### F-12 Form dialog template
- **Source:** `Features/Master/FuelTypes/Components/FuelTypeForm.razor`
- **What:** `@inherits CrudFormBase<T>`; RadzenTemplateForm + RadzenFormField (Outlined) + DataAnnotation validators; Bootstrap rows; character counter; status switch + chip; AuditInfo; Cancel/Save footer with `IsBusy`; first-field focus.
- **Why:** gives every form the same look and behaviour.
- **Adapt:** template for `Features/<Module>/<Feature>/Components/<Feature>Form.razor`.
- **Changes:** use the new base-class hooks; remove the duplicated loading/saving flags.

### F-13 Layout shell
- **Source:** `Components/Layout/MainLayout.razor`, `LoginLayout.razor`, `NavMenu.razor`
- **What:** RadzenLayout with header (sidebar toggle, logo, actions, logout), sidebar NavMenu, body and footer; a separate login layout.
- **Why:** a standard admin shell built from Radzen components.
- **Adapt:** `Frontend/src/FarmFMS.Web/Components/Layout/` (exists, scaffold-level).
- **Changes:** structure only; Farm FMS branding, menu and footer; wrap `@Body` in an `ErrorBoundary`; trim the NavMenu by policy.

### F-14 Theme and CSS
- **Source:** `Components/App.razor`, `wwwroot/theme/*.css`, `wwwroot/main.css`
- **What:** the full Radzen Material theme, Bootstrap and four custom CSS files.
- **Why:** it shows how RouteNexa splits Radzen and Bootstrap usage (Radzen for widgets, Bootstrap for layout).
- **Adapt:** keep the Farm FMS `App.razor` (`material-base.css`, fingerprinted assets) + a single `wwwroot/css/farmfms-theme.css` with CSS variables.
- **Changes:** don't copy RouteNexa CSS or branding assets.

### Frontend: not carried over
RouteNexa feature screens (Masters, Sales, Optimization), Google Maps components (`LocationPicker`, `PlanMap`), logos/SVG/branding, `wwwroot/theme/*` and `main.css`, template pages (Counter, Weather), the NuGet `bootstrap` package duplication, and every value in `appsettings*.json` (**the committed Google Maps API keys must not be reused**).

---

## Part C – Reference-based implementation plan (proposed steps)

Each step ends with a review, and the next step starts only after approval. Steps 02–04 build only shared infrastructure. **Module 01 business features start no earlier than Step 05.**

| Step | Scope | Main mapping items | Output |
|---|---|---|---|
| **02** Backend foundation | `StoredProcedureExecutor`, `UnitOfWork`, `SqlErrorTranslator`, `Result` model, ProblemDetails + global handler, `ICurrentUserService`, validated options, logging conventions, Application common extensions/DTO base, architecture test project | B-02, B-03, B-04, B-07–B-09, B-11, B-12, B-15 | Builds, `/health` green, architecture tests pass. No tables, no procedures. |
| **03** Frontend foundation | `Features/Common` (Http, Dialogs, Notifications, Extensions, PageHeader, AuditInfo, Dtos), client registration helper, layout shell + ErrorBoundary, theme file | F-01–F-04, F-06–F-10, F-13, F-14 | Builds, health page calls the API through the new helpers. |
| **04** Security foundation | Auth per approved design: AES-256-GCM protector, token service, auth controller, frontend auth state, bearer handler, login page, policies (requires the security spec and Module 01 auth tables) | B-14, F-05, AD-11–AD-13 | Login, refresh and logout work end to end; protected-by-default verified. |
| **05+** Module 01 | Entities + EF configurations + migration per spec, stored procedures, repositories, services, controllers, list/form pages using the templates | B-05, B-06, B-10, B-13, F-11, F-12 | Per approved Module 01 specification. |

> Step 04 may be merged into Step 05 if the auth tables are part of the Module 01 specification. This is to be decided at Step 02 approval.

**Inputs needed before Step 02:** PMS SQL Server naming standard, Module 01 database specification, AES-256-GCM password design, and answers to the open items in `FarmFMS_Architecture_Decisions_v2.md`.
