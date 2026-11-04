# RouteNexa Web – Reference Review

| | |
|---|---|
| Step | 01 – Reference Analysis |
| Reference | RouteNexa Web (`frontend.zip`) |
| Reviewed | 2026-11-04 |
| Purpose | Identify reusable Blazor/Radzen coding patterns for the Farm FMS Frontend. No RouteNexa branding, business screens, credentials or API keys are carried over. |

> Paths in this document are relative to the root of the RouteNexa frontend archive (`frontend/`).
> RouteNexa Web was reviewed read-only from an extracted copy; the original archive was not modified.

---

## 1. Project structure

Single project `RouteNexa.Web.csproj` (`net10.0`, Blazor Web App, global **Interactive Server** render mode). Packages: `Radzen.Blazor 12.1.2`, `bootstrap 5.3.8` (NuGet), `System.IdentityModel.Tokens.Jwt 8.23.0`.

```
Components/            App.razor, Routes.razor, _Imports.razor
  Layout/              MainLayout, LoginLayout, NavMenu, ReconnectModal
  Pages/               template leftovers (Counter, Weather, Home, Index), Error, NotFound
Extensions/            ApiServiceCollectionExtensions.cs   (typed HttpClient registration)
Features/
  Authentication/      Login.razor, AuthApiService, RouteNexaAuthStateProvider, BearerTokenHandler, DTOs
  Common/
    Components/        AuditInfo, PageHeader/, Location/LocationPicker
    Dialogs/           CrudFormBase, CrudFormType(+Extensions), DialogServiceExtensions, ICrudForm
    Dtos/              AuditableDto
    Extensions/        DataGridExtensions, TooltipServiceExtensions
    Http/              ApiException, ApiJson, HttpClientExtensions, HttpResponseMessageExtensions, QueryString
    Notifications/     NotificationServiceExtensions
  <Area>/<Feature>/
    Components/        <Feature>Form.razor        (dialog form)
    Models/            <Feature>Dto, <Feature>Request
    Pages/             <Feature>List.razor        (routable grid page)
    Services/          <Feature>ApiService.cs     (typed HttpClient)
wwwroot/               app.css, main.css, theme/(pms_ui.css, theme.css, popup.css), lib/, js/, resources/
```

**Assessment:** the **vertical-slice `Features/<Area>/<Feature>/{Components,Models,Pages,Services}`** layout and the `Features/Common` toolkit are the most valuable things in this project. Reuse both.

**Issues:** template leftovers (`Counter`, `Weather`); the `.csproj` lists empty `<Folder>` entries for folders that don't exist (`Optimization\Plan\...`); there are two `_Imports.razor` files with overlapping content, and both apply `@attribute [Authorize]`.

---

## 2. Program.cs

What it does:
1. `AddRazorComponents().AddInteractiveServerComponents()`.
2. `AddRadzenComponents()` (registers Dialog, Notification, Tooltip and ContextMenu services).
3. `AddApiServices(configuration)` (typed clients, §4).
4. `AddAuthorization()`, `AddCascadingAuthenticationState()`, custom `RouteNexaAuthStateProvider` registered both as itself and as `AuthenticationStateProvider`.
5. `CircuitServicesAccessor` (singleton) + `ServicesAccessorCircuitHandler` (scoped `CircuitHandler`) + `BearerTokenHandler` (transient). See §6.
6. A named client `"ApiAnon"` with no bearer handler, used only for login, refresh and logout.
7. Pipeline: exception handler + HSTS (non-Development), `UseStatusCodePagesWithReExecute("/not-found")`, HTTPS redirect, `UseAuthorization`, `UseAntiforgery`, `MapStaticAssets`, `MapRazorComponents<App>().AddInteractiveServerRenderMode().AllowAnonymous()`.

**Reuse:** the registration order; a separate unauthenticated client for auth endpoints (prevents refresh recursion); `UseStatusCodePagesWithReExecute`.

**Redesign:** `ApiSettings:BaseUrl` is read twice (in `Program.cs` and in `AddApiServices`) with no URL validation and no timeout. Farm FMS already has the better pattern: `BackendApiOptions` with `ValidateOnStart()` (`FarmFMS.Web/Services/Api/ApiClientServiceCollectionExtensions.cs`). Keep the Farm FMS options class and extend it.

---

## 3. Radzen and Bootstrap configuration

| | RouteNexa | Farm FMS (current scaffold) |
|---|---|---|
| Radzen theme | `<RadzenTheme Theme="material" @rendermode="InteractiveServer" />`, the **full** Material theme | `material-base.css`, the "base" variant designed to coexist with Bootstrap |
| Bootstrap | NuGet `bootstrap` package **and** `wwwroot/lib/bootstrap` link | `wwwroot/lib/bootstrap` only |
| Load order | scoped styles → Bootstrap → app.css → 3 theme files → main.css | Bootstrap → Radzen base → app.css → scoped styles |
| Radzen JS | `_content/Radzen.Blazor/Radzen.Blazor.js` (not fingerprinted) | `@Assets[...]` (fingerprinted) |

**Assessment:**
- RouteNexa loads the full Radzen theme on top of Bootstrap plus four custom CSS files, so styles compete. The scoped-styles bundle loads before Bootstrap, so Bootstrap can override component-scoped rules.
- The **usage split is good and should be reused**: Radzen for data-heavy widgets (DataGrid, forms, dialogs, notifications, layout shell); Bootstrap for the grid system and utilities (`row g-3`, `col-12 col-md-5`, `d-flex gap-2`).
- Keep the Farm FMS `material-base` approach. Introduce a single Farm FMS theme file with CSS custom properties (e.g. `--primary-700`, used by `PageHeader.razor.css`) rather than three overlapping theme files.

---

## 4. API HttpClient registration (`Extensions/ApiServiceCollectionExtensions.cs`)

What it does: one `AddHttpClient<TService>(client => client.BaseAddress = baseAddress).AddHttpMessageHandler<BearerTokenHandler>()` per feature.

**Reuse:** typed clients per backend resource; bearer handler attached per client.

**Issues:**
- `PlanApiService` is registered **without** `BearerTokenHandler`. Because the API applies `RequireAuthorization()` to every controller, all Plan calls will fail with 401.
- No timeout, no resilience (retry/circuit breaker), no options validation.
- Each line repeats the base-address lambda.

**Farm FMS:** keep `ApiClientServiceCollectionExtensions.AddBackendApiClients` and add a private helper `AddBackendClient<T>()` that applies `ConfigureBackendClient` + `BearerTokenHandler` in one call, so no client can forget the handler. Optionally add `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`) for idempotent GETs only (decision pending).

---

## 5. API service classes (`Features/<Area>/<Feature>/Services/<Feature>ApiService.cs`)

Example: `Features/Master/FuelTypes/Services/FuelTypeApiService.cs`.

- `const string Endpoint = "api/fuel-types"`, constructor-injected `HttpClient` with a null guard.
- Methods `GetAllAsync`, `GetActiveAsync`, `GetByIdAsync` (returns `null` on 404 via `catch (ApiException) when (StatusCode == NotFound)`), `CreateAsync`, `UpdateAsync`. All take a `CancellationToken`.
- All transport is through **`Features/Common/Http/HttpClientExtensions.cs`** (`GetJsonAsync`, `GetListAsync`, `PostJsonAsync`, `PutJsonAsync`, `DeleteOrThrowAsync`). Each helper calls **`HttpResponseMessageExtensions.EnsureSuccessOrThrowAsync`**, which parses `ProblemDetails`/`ValidationProblemDetails` (`title`, `detail`, `message`, `errors`), plain JSON strings and plain text into **`ApiException`** (status code, title, detail, field errors, and a `UserMessage` with friendly fallbacks per status).
- `ApiJson.Options` = `JsonSerializerDefaults.Web`. `QueryString` builds invariant-culture query strings (dates `yyyy-MM-dd`, skips nulls, `AddIfPositive`).

**Assessment:** **excellent pattern; reuse almost as-is.** It turns every non-success response into one exception type, so pages need only `catch (Exception ex) → NotifyError(ex)`.

**Adapt for Farm FMS:**
- Once the Farm FMS backend returns RFC 7807 consistently (Backend review §11), the parser can drop the `message` and plain-string fallbacks. Keeping them for robustness is cheap.
- Add `traceId` extraction to `ApiException` so users can quote it to support.
- Older services (e.g. `CustomerApiService`) don't use the helpers. Farm FMS uses the helpers everywhere.

---

## 6. Authentication services (`Features/Authentication/`)

| File | Role |
|---|---|
| `AuthApiService.cs` | login / refresh / logout over the anonymous client; maps 401 and network failures to user-friendly messages |
| `RouteNexaAuthStateProvider.cs` | custom `AuthenticationStateProvider`: keeps the token pair in **`ProtectedLocalStorage`** (Data-Protection encrypted), lazily loads the session, refreshes when < 60 s remain using a `SemaphoreSlim` (single-flight), clears on refused refresh, builds the `ClaimsPrincipal` from JWT claims (`name`/`role`), handles prerender (`InvalidOperationException`) and stale keys (`CryptographicException`) |
| `BearerTokenHandler.cs` | `DelegatingHandler` that attaches the bearer token. Contains `CircuitServicesAccessor` + `ServicesAccessorCircuitHandler`, the documented pattern for reaching circuit-scoped services from `IHttpClientFactory` handler scopes |
| `Login.razor` | `[AllowAnonymous]`, `LoginLayout`, Radzen form, show/hide password |
| `Components/Routes.razor` | `AuthorizeRouteView`; `<NotAuthorized>` navigates to `login` |

**Reuse:** the whole architecture: anonymous auth client; circuit services accessor; single-flight refresh; protected storage; prerender-safe loading; and `@attribute [Authorize]` in `_Imports.razor` with `[AllowAnonymous]` only on Login.

**Bugs and risks found (fix in Farm FMS):**
1. **Contract mismatch:** the frontend `AuthResponse.AccessTokenExpiresDateTime` doesn't match the backend `AccessTokenExpiresUtc`. The value is always `DateTime.MinValue`, `IsFresh` is always false, so **every API call triggers a refresh and rotates the refresh token**. Farm FMS should share one documented contract and add a contract test.
2. `IsFresh` compares against `DateTime.Now` (local). Use UTC (`DateTimeOffset.UtcNow` / `TimeProvider`).
3. `BearerTokenHandler` contains a `Console.WriteLine` diagnostic marked "temporary" that logs every request URI. Use `ILogger<T>` at Debug, or remove it.
4. `ProtectedLocalStorage` persists the **refresh token** in the browser across sessions (encrypted, but replayable from the same server's Data-Protection keys). Decide per the Farm FMS security requirements: `ProtectedSessionStorage` (per tab) vs local, and an idle timeout.
5. `<NotAuthorized>` calls `Navigation.NavigateTo` during render. Prefer a small `RedirectToLogin` component that navigates in `OnInitialized`, with a `returnUrl`.
6. Authorization is "signed-in only". No `AuthorizeView Roles/Policy` usage. Farm FMS will need policy-based UI trimming that mirrors the API policies.
7. Data-Protection keys aren't configured to persist, so app restarts or multiple instances invalidate stored sessions (the `CryptographicException` path). Configure `AddDataProtection().PersistKeysTo…` for non-dev environments.

---

## 7. CrudFormBase and dialogs (`Features/Common/Dialogs/`)

- **`CrudFormBase<TRequest>`**: base component for every master add/edit/view dialog.
  - Parameters `FormType` (required; enforced in `SetParametersAsync`, so it throws with a helpful message if not opened via the extension) and `Id`.
  - `Request` model, `IsAddMode` / `IsEditMode` / `IsViewMode`.
  - **Dirty tracking** by JSON snapshot (`TakeSnapshot()`, `IsDirty`).
  - `CloseAsync(saved)` asks "Discard changes?" when there are unsaved edits, with a re-entrancy guard. `OnKeyDownAsync` handles Escape with the same check.
- **`ICrudForm`**: typed parameter names (no magic strings).
- **`CrudFormType`** + `ToTitle(entityName)` ("Add Fuel Type" / "Edit …" / "View …") + `IsReadOnly()`.
- **`DialogServiceExtensions.OpenCrudFormAsync<TForm>(entityName, formType, id, width)`**: opens with standard options (600 px, max 95vw/90vh, not draggable, no close X, no Esc/overlay close, so the form controls closing) and returns `true` only after a successful save. It also has a **legacy overload** for forms that don't inherit the base yet (Customer, Depot).

**Assessment:** **reuse.** This is a well-designed, consistent CRUD dialog contract.

**Adapt:**
- Drop the legacy overload. Every Farm FMS form inherits `CrudFormBase`.
- Generalise `Id` (`int?`) if Farm FMS keys aren't `int` (follow the Module 01 spec; `long`/`Guid` would need a generic key parameter).
- Add `OnSaveAsync` / `LoadAsync` template hooks so forms don't repeat the `IsSaving`/`IsLoading` boilerplate (every RouteNexa form re-implements it).
- Map `ApiException.Errors` (server validation) back onto form fields, not only into a notification.

---

## 8. PageHeader (`Features/Common/Components/PageHeader/`)

`PageHeader.razor` + scoped CSS + `PageHeaderBreadcrumb` record `(Text, Path?, Icon?)`. It renders `<PageTitle>` as `"{Title} · RouteNexa"`, the title (`h3`), an optional description and a right-aligned breadcrumb (last item marked `aria-current="page"`). It is responsive (the breadcrumb moves above the title on mobile).

**Reuse.** Changes: replace the hard-coded `AppName = "RouteNexa"` with a Farm FMS app-name option; replace hard-coded colours in the CSS with theme variables; the CSS defines `.page-title` but the markup uses `main-text`, so align them; namespace mismatch (file in `Components/PageHeader/`, namespace `…Common.Components`), so pick one.

---

## 9. DataGridExtensions (`Features/Common/Extensions/DataGridExtensions.cs`)

Null-safe extensions on `RadzenDataGrid<T>?`: `HasActiveFilters`, `ClearAllFiltersAsync(reload)`, `HasActiveSorting`, `ClearSortingAsync`, `ResetAsync` (filters + sorting + column state + first page) and `GetFilteredCount`.

**Reuse as-is.** It pairs with the list-page pattern (§10), where the Actions column filter cell hosts a "Clear" button.

---

## 10. List page pattern (`Features/Master/FuelTypes/Pages/FuelTypeList.razor`)

Standard structure every Farm FMS list page should follow:
1. `PageHeader` with description and breadcrumbs.
2. `RadzenCard` → `RadzenDataGrid` with filtering (`SimpleWithMenu`), multi-sort, reorder/resize, paging (10, page-size options, summary).
3. `HeaderTemplate` toolbar: Refresh (icon + tooltip) and a primary "Add …" button.
4. **Three-state `EmptyTemplate`**: not loaded (offer Refresh) / no data yet (offer Add) / no filter matches (offer Clear filters).
5. Status column with a dropdown `FilterTemplate` (Active/Inactive/All) and status chip.
6. Actions column: View / Edit icon buttons with `aria-label` and tooltips, plus Clear in the filter cell.
7. Code: `IsLoading` guard against double clicks; `HasLoaded` flag; `try/catch → NotificationService.NotifyError(ex, "Failed to …")`; reload after `OpenCrudFormAsync` returns `true`.

Form pattern (`FuelTypeForm.razor`): `@inherits CrudFormBase<TRequest>`, `RadzenTemplateForm` + `RadzenFormField` (Outlined) + `RadzenDataAnnotationValidator` (`Popup`), Bootstrap rows, a character counter, `RadzenSwitch` with status chip, `AuditInfo` in Edit/View, a footer with Cancel/Close + Save (`IsBusy`), focus on the first field after load, and a warning on invalid submit.

**Reuse both as the Farm FMS page templates.** Remove the no-op `catch (Exception) { throw; }` blocks.

---

## 11. Other common components and helpers

| Item | Location | Verdict |
|---|---|---|
| `NotificationServiceExtensions` (`NotifySuccess/Info/Warning/Error`, exception overload mapping `ApiException`/`HttpRequestException`/`TaskCanceledException` to friendly text) | `Features/Common/Notifications/` | **Reuse as-is.** Change the `_ => ex.Message` fallback to a generic message (don't surface raw exception text). |
| `TooltipServiceExtensions.ShowTooltip` | `Features/Common/Extensions/` | Reuse |
| `AuditInfo.razor` (created/modified by and when, invariant `dd/MM/yyyy HH:mm`) | `Features/Common/Components/` | Reuse. Convert UTC to local display time explicitly. |
| `AuditableDto` | `Features/Common/Dtos/` | Reuse as the base for Farm FMS response models |
| `LocationPicker.razor` / `PlanMap.razor` (Google Maps) | `Features/Common/Components/Location/`, `Features/Optimization/` | **Do not reuse.** RouteNexa-specific, and depends on a committed API key. |
| `MainLayout.razor` (RadzenLayout/Header/Sidebar/Body/Footer, sidebar toggle, logout, notifications offcanvas) | `Components/Layout/` | Reuse the **structure** only; replace RouteNexa branding/SVG/footer text |
| `LoginLayout.razor`, `Login.razor` | | Reuse the structure; Farm FMS branding and copy |

---

## 12. Exception handling (frontend)

- Server: `UseExceptionHandler("/Error")` in non-Development; `/not-found` re-execute.
- API: everything funnels into `ApiException` (§5). Pages catch and notify.
- **Missing:** no `<ErrorBoundary>` around `@Body`, so an unhandled exception in a component kills the circuit and shows the generic reconnect UI. There's no `ILogger` usage in the frontend.

**Farm FMS:** wrap `@Body` in an `ErrorBoundary` with a friendly recovery UI; log via `ILogger<T>`; keep the `ApiException` + `NotifyError` funnel.

---

## 13. Configuration and security findings

- **`appsettings.json` contains a Google Maps API key in plain text, plus a second key in a comment.** The values were deliberately **not** reproduced anywhere in the Farm FMS documentation. RouteNexa's owners should rotate and restrict these keys. Farm FMS must not hold third-party keys in committed configuration.
- `ApiSettings:BaseUrl` is fine to commit (not secret), but Farm FMS uses its own section name `BackendApi` (already scaffolded).

---

## 14. Summary: reuse vs redesign

| Pattern | Verdict |
|---|---|
| `Features/<Area>/<Feature>/{Components,Models,Pages,Services}` layout | **Reuse** |
| `Common/Http` (ApiException, HttpClientExtensions, EnsureSuccessOrThrowAsync, ApiJson, QueryString) | **Reuse** (+ traceId) |
| Typed API services | **Reuse**, always through the helpers |
| HttpClient registration | **Adapt**: Farm FMS options + helper that always adds the bearer handler |
| Auth state provider, circuit accessor, bearer handler, anon client | **Reuse architecture**, fix contract/UTC bugs, decide storage, remove Console logging |
| CrudFormBase / ICrudForm / CrudFormType / DialogServiceExtensions | **Reuse**; drop legacy overload; add load/save hooks |
| PageHeader, DataGridExtensions, Notification/Tooltip extensions, AuditInfo | **Reuse** (de-brand, theme variables) |
| List page + form page templates | **Reuse** as Farm FMS templates |
| Radzen full theme + 4 CSS files | **Redesign**: keep Farm FMS `material-base` + one theme file |
| Google Maps components, RouteNexa features, branding, keys | **Do not copy** |
