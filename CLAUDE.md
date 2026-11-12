# CGE Farm Monitoring System (Farm FMS) — CLAUDE.md

**Farm FMS = Farm Monitoring System** (never "Farm Management System").
Breeder farm (livestock / poultry) monitoring for Ceylon Grain Elevators PLC, built by Prima Management Services.
The web application comes first; a mobile application follows later and uses the same backend API.

## Repository map

| Path | Content | Instructions |
|---|---|---|
| `Backend/` | `FarmFMS.sln`: .NET 10 Web API, Clean Architecture (Domain, Application, Infrastructure, API) + tests | `Backend/CLAUDE.md` |
| `Frontend/` | `FarmFMS.Web.sln`: Blazor Web App .NET 10, Interactive Server, Radzen + Bootstrap | `Frontend/CLAUDE.md` |
| `Database/` | Version-controlled Stored Procedure scripts (`StoredProcedures/<Module>/dbo.<Name>.sql`) | `Database/README.md` |
| `Documentation/Architecture/` | `FarmFMS_Architecture_Decisions_v2.md` (**authoritative AD record**), `FarmFMS_Reference_Mapping.md` | |
| `Documentation/06_Database/` | `Database_Conventions.md`, Module database specifications | |
| `Documentation/Requirements/`, `Business_Design/`, `Project_Overview/` | SRS, functional requirements, clarification register, traceability, role matrix | |
| `Documentation/Specs/<Module>/` | Module implementation specifications (written by the Solution Architect) | |
| `Documentation/Process/Agentic_Development_Workflow.md` | Agent team, gates, hand-offs, security boundaries | |

Architecture in one line: Dapper through Stored Procedures for **all** application data access; EF Core Code First **only** for schema and migrations; no physical foreign keys; `dbo` only; the Frontend talks to the Backend over HTTP only. Details: AD v2 and `Backend/CLAUDE.md`.

## Source of truth (in this order)

1. Explicit decisions by the user in the session.
2. Approved specifications (Module 01 database specification v0.2, `Database_Conventions.md`) and Mandatory / Approved ADs.
3. Proposed ADs and the Reference Mapping: follow them and flag any deviation.
4. Draft SRS v0.2 and Functional Requirements: **drafts, not approved requirements**. Raise conflicts as clarification questions.
5. Analysis documents (traceability matrix, gap analyses, workflow analysis): supporting evidence only.

Cite FR / AD / DBG / CQ identifiers in deliverables. Never rename approved database objects. Never silently change a specification. **Never invent business requirements.** Use the `farmfms-spec-compliance` skill.

## Hard rules (every session and every agent)

- **Git:** never run `git commit`, `git push`, force pushes, history rewrites (`filter-branch`, `filter-repo`, `rebase`, `reset --hard`) or anything that discards uncommitted work. Only the user commits and pushes.
- **Secrets:** never read, print or copy credentials, connection strings or user-secrets (`dotnet user-secrets list`, `secrets.json`). Credentials live in user-secrets / environment variables, never in `appsettings*.json`.
- **Database:** never run commands that change a database (`dotnet ef database update`, `sqlcmd`, migration bundles, scripts) without explicit user approval for that command. Never touch production.
- **Stop and ask** when a requirement is unclear, an approved specification contradicts itself, a database dependency is undefined, a security decision is pending, or a change needs an unapproved architecture decision.
- Preserve existing user changes. Do not modify application source code outside an authorized task.

## Agent team

The **main session is the Lead Orchestrator** (skill `farmfms-orchestrate`). Subagents never talk to each other; all coordination, integration and verification happens in the main session.

| Agent | Use for | Model |
|---|---|---|
| `business-analyst` | Requirements, traceability, acceptance criteria, clarification questions (documentation only) | Sonnet |
| `solution-architect` | Module implementation specifications in `Documentation/Specs/<Module>/` | Opus |
| `database-engineer` | EF configurations / migrations, Stored Procedure scripts, error catalogue entries | Sonnet |
| `backend-developer` | Domain / Application / repositories / services / controllers + unit tests | Sonnet |
| `frontend-developer` | Blazor / Radzen features, typed API clients (only after frontend foundation approval) | Sonnet |
| `qa-engineer` | Tests and verification reports (test files only) | Sonnet |
| `code-reviewer` | Independent, read-only, severity-ranked review | Opus |

Every agent returns a hand-off in the `farmfms-handoff` format. Shared hotspots have one writer at a time: `Program.cs`, `DependencyInjection.cs` files, `StoredProcedureErrors.cs`, `<Module>Procedures.cs`, `FarmFmsDbContext`, `Migrations/`.

## Quality gates

Run `python .claude/skills/farmfms-quality-gates/run_gates.py` (Release build + unit + architecture tests). Use the Release configuration: Visual Studio locks the Debug output.

```bash
dotnet build Backend/FarmFMS.sln -c Release
dotnet test  Backend/FarmFMS.sln -c Release --no-build
```

Gates G0–G8 are defined in the `farmfms-quality-gates` skill. A gate passes only with real command output.

## Current status (12-11-2026)

- Step 02 / 02.1 backend foundation implemented; Step 02 final approval pending (AD v2, remaining items).
- Module 01 is authorized **step by step** per `Documentation/Dev_RoadMap/DEVELOPMENT_ROADMAP.md`. Step 0 (authentication foundation,
  G0 approved 2026-11-12) is implemented and Ready for Testing; the Login Page and later steps are **not** started. No unauthenticated admin endpoints.
- Frontend feature work waits for frontend foundation (Step 03) approval.
- **Known unresolved security issue:** a SQL credential is in public Git history (commits `5ffe3f2`..`f4b75fe`). It is not resolved; do not claim otherwise.
