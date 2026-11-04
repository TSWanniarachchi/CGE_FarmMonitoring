# Farm FMS

Farm Management System, built as **two independent solutions** that communicate only over HTTP.

```
FarmFMS/
  Backend/          ASP.NET Core Web API (.NET 10) — FarmFMS.sln
  Frontend/         Blazor Web App (.NET 10)       — FarmFMS.Web.sln
  Database/         SQL Server scripts (Stored Procedures, seed data, deployment scripts)
  Documentation/    Specifications, standards and design documents
  README.md
```

There is **no shared solution**. Each side builds, runs and deploys on its own.

## Backend — `Backend/FarmFMS.sln`

Simple Clean Architecture on Microsoft SQL Server.

| Project | Responsibility | References |
|---|---|---|
| `FarmFMS.Domain` | Entities, value objects, domain rules | — |
| `FarmFMS.Application` | Use cases, DTOs, interfaces | Domain |
| `FarmFMS.Infrastructure` | EF Core (schema/migrations), Dapper data access | Application, Domain |
| `FarmFMS.API` | Controllers, DI composition, hosting | Application, Infrastructure |

Data access:
- **EF Core Code First** — database schema and migrations only.
- **Dapper** — all application data access.
- **Stored Procedures** — all CRUD and business operations.
- Database objects follow the **PMS SQL Server naming standard**.

```bash
cd Backend
dotnet build FarmFMS.sln
dotnet run --project src/FarmFMS.API      # https://localhost:7196  (GET /health)
```

## Frontend — `Frontend/FarmFMS.Web.sln`

Blazor Web App (Interactive Server) using **Radzen Blazor** components and **Bootstrap**.
Talks to the Backend exclusively through **typed HttpClient services**; it does not reference any Backend assembly.

```bash
cd Frontend
dotnet build FarmFMS.Web.sln
dotnet run --project src/FarmFMS.Web      # https://localhost:7000
```

Backend URL: `BackendApi:BaseUrl` in `Frontend/src/FarmFMS.Web/appsettings.json`.

## Prerequisites

- .NET SDK 10.0.401 or later 10.0.x (pinned per solution via `global.json`)
- Microsoft SQL Server (local default instance assumed by the dev connection string)
- `dotnet-ef` tool for migrations: `dotnet tool install --global dotnet-ef`

## Working with Claude Code

Each solution has its own `CLAUDE.md` with architecture rules and conventions:
- `Backend/CLAUDE.md`
- `Frontend/CLAUDE.md`
