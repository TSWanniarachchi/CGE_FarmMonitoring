# FarmFMS Frontend — CLAUDE.md

Blazor Web App (.NET 10), Interactive Server render mode (global), Radzen Blazor + Bootstrap.
This solution (`FarmFMS.Web.sln`) is fully independent of the Backend.

> **Project rules first:** the root `../CLAUDE.md` defines the source-of-truth order, the hard rules (no commits / pushes,
> no secrets) and the agent team. Frontend work is done by the `frontend-developer` agent (writes only under `Frontend/`)
> against a frozen API contract, **only after the frontend foundation (Step 03) is approved**. Build gate:
> `python .claude/skills/farmfms-quality-gates/run_gates.py --frontend --build-only` from the repository root (Release).

## Build / run

```bash
dotnet build FarmFMS.Web.sln
dotnet run --project src/FarmFMS.Web            # http://localhost:5240, https://localhost:7000
```

The Backend API must be running for data to load. Its URL is configured in `appsettings.json`:

```json
"BackendApi": { "BaseUrl": "https://localhost:7196", "TimeoutSeconds": 30 }
```

Override per environment with `BackendApi__BaseUrl`.

## Hard rules

- **Never reference Backend projects or assemblies** (`FarmFMS.Domain`, `.Application`, `.Infrastructure`, `.API`).
- **Communicate with the Backend through HTTP APIs only**, via typed HttpClient services.
- No direct database access from the Frontend (no EF, Dapper, SqlClient).
- Frontend DTOs/models are defined in this project (`Models/`) and mirror the API contracts.

## Structure

```
src/FarmFMS.Web/
  Components/
    App.razor              # Bootstrap + Radzen theme CSS, Radzen JS
    Routes.razor
    _Imports.razor         # includes Radzen, Radzen.Blazor, FarmFMS.Web.Services.Api
    Layout/                # MainLayout (hosts <RadzenComponents />), NavMenu
    Pages/                 # routable pages, one folder per module as modules are added
  Services/Api/            # typed HttpClients + BackendApiOptions + registration
  wwwroot/                 # app.css, lib/bootstrap
```

## Typed HttpClient pattern

1. Create `Services/Api/<Resource>ApiClient.cs` with a primary constructor taking `HttpClient`.
2. Use relative URIs (e.g. `"api/farms"`) — the base address comes from `BackendApiOptions`.
3. Register it in `ApiClientServiceCollectionExtensions.AddBackendApiClients`:
   `services.AddHttpClient<XyzApiClient>(ConfigureBackendClient);`
4. Inject into pages/components with `@inject XyzApiClient Api`.

`SystemHealthApiClient` (calls `GET /health`) is the reference example.

## UI conventions

- Use Radzen components for data-heavy UI (DataGrid, forms, dialogs, notifications); use Bootstrap for layout/grid/utilities.
- Theme: `material-base.css` — the Radzen "base" variant is designed to coexist with Bootstrap. Change theme only in `App.razor`.
- `DialogService`, `NotificationService`, `TooltipService`, `ContextMenuService` are registered via `AddRadzenComponents()`.

## Current status

Step 01 (scaffolding) only. No authentication or business modules exist yet.
