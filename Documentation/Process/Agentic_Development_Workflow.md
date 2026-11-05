# CGE Farm Monitoring System – Agentic Development Workflow

| | |
|---|---|
| Document | Agentic_Development_Workflow.md |
| Version | 0.1 – Draft – Pending Review |
| Date | 05-11-2026 |
| Scope | How Claude Code agents are used to build Farm FMS: roles, hand-offs, gates and security boundaries |
| Configuration | `CLAUDE.md` (root), `.claude/agents/`, `.claude/skills/`, `.claude/settings.json`, `.claude/hooks/` |

## 1. Approved decisions this workflow implements

| Area | Decision |
|---|---|
| Orchestration | The **main Claude Code session is the Lead Orchestrator**. There is no orchestrator subagent; subagents do not coordinate with each other. |
| Specifications | Module implementation specifications live in `Documentation/Specs/<Module>/`. |
| User-level skills | Kept separate; the project neither modifies nor depends on them. No `AGENTS.md`. |
| Module 01 sequence | Authentication (Step 04) **before** any admin API. No unauthenticated admin endpoints. |
| Models | Opus: Solution Architect, Code Reviewer. Sonnet: all implementation agents. |
| Git | Agents never commit or push. Only the human commits and pushes. |
| Module 01 | **Not authorized yet.** |

## 2. Agent architecture

```
Human (approver; owns commits and pushes)
  └── Main session = Lead Orchestrator  (skill: farmfms-orchestrate)
        ├── business-analyst     Sonnet   requirements, traceability, acceptance criteria   (Documentation only)
        ├── solution-architect   Opus     module implementation specifications              (Documentation only)
        ├── database-engineer    Sonnet   EF configurations/migrations, SP scripts, error catalogue
        ├── backend-developer    Sonnet   Domain/Application/repositories/services/controllers + unit tests
        ├── frontend-developer   Sonnet   Blazor/Radzen features (after frontend foundation approval)
        ├── qa-engineer          Sonnet   tests and verification reports (test files only)
        └── code-reviewer        Opus     independent, read-only review (fresh context)
```

### 2.1 Responsibilities and boundaries

| Agent | Responsible for | Must not | Writes to |
|---|---|---|---|
| Business Requirements Analyst | Requirement analysis, traceability, acceptance criteria, contradictions, clarification questions | Change code; invent requirements; treat drafts as approved | `Documentation/Requirements/`, `Documentation/Business_Design/` |
| Solution Architect | Module implementation specifications: endpoints, DTOs, service boundaries, procedure inventory, error codes, dependencies, decisions needed | Change approved ADs or database specifications (flags conflicts instead) | `Documentation/Specs/<Module>/` (and conflict notes to the Orchestrator) |
| Database Engineer | EF configurations and migrations, Stored Procedure scripts, error catalogue entries, procedure-name constants | Physical FKs; schema beyond the approved specification; applying migrations; reading secrets | `Backend/src/FarmFMS.Infrastructure/Persistence/**`, `Database/StoredProcedures/**`, `StoredProcedureErrors.cs`, `<Module>Procedures.cs` |
| Backend Developer | Domain, Application, repositories (via `IStoredProcedureExecutor`), services, DTOs, controllers, registration, unit tests | Inline SQL; EF data access; editing migrations / SQL scripts | `Backend/src/**` (except `Persistence/Migrations`, `Persistence/Configurations`), `Backend/tests/FarmFMS.UnitTests/**` |
| Frontend Developer | Blazor / Radzen features, typed API clients, UI validation | Reference Backend assemblies; work outside `Frontend/`; start before foundation approval | `Frontend/**` |
| QA Engineer | Unit, architecture and API host tests; integration tests once a test database exists; verification reports | Modify production source | `Backend/tests/**`, Frontend test projects |
| Code Reviewer | Independent review: correctness, ADs, security, performance, naming, tests | Modify any file | nothing (report only) |

Tool access is least-privilege (see each `.claude/agents/*.md`):
- The Business Analyst and Solution Architect have no shell.
- The Code Reviewer has no file-writing tools, and its shell is limited to inspection commands.
- No agent can start other agents (`Agent` is not in any agent's tools).

The "Writes to" paths are **enforced by the PreToolUse hook**, which receives the calling agent's `agent_type`, for the Edit / Write tools. Writes through the shell (e.g. a redirect) are not path-checked, so G6 review remains the backstop.

## 3. Source-of-truth order

1. Explicit decisions by the user.
2. Approved specifications and Mandatory / Approved Architecture Decisions.
3. Proposed Architecture Decisions and the Reference Mapping (deviations flagged).
4. Draft SRS and Functional Requirements (conflicts raised as clarifications).
5. Analysis documents (evidence only).

Deliverables cite FR / AD / DBG / CQ identifiers. Approved database objects are never renamed; specifications are never changed silently. Details: skill `farmfms-spec-compliance`.

## 4. Work lifecycle (one slice)

| # | Step | Owner | Exit |
|---|---|---|---|
| 1 | Requirements analysis and acceptance criteria | business-analyst | Hand-off + open CQs |
| 2 | Module implementation specification | solution-architect | **G0: human approval** |
| 3 | Schema: EF configurations, migration (not applied) | database-engineer | G1–G4 |
| 4 | Stored Procedures, error catalogue, procedure constants | database-engineer | G1–G4 |
| 5 | Repositories, services, DTOs, controllers, registration | backend-developer | G1–G3 |
| 6 | Tests from acceptance criteria (can start with step 5) | qa-engineer | G2–G3 |
| 7 | Frontend (only with a frozen API contract and foundation approval) | frontend-developer | G1 + UI checks |
| 8 | Security checks | Orchestrator + code-reviewer | G5 |
| 9 | Independent review | code-reviewer (fresh context) | G6 |
| 10 | Human review and commit | human | G8 |

### 4.1 Sequential vs parallel

**Sequential:**
- Requirements → specification approval → database → dependent procedures.
- Database schema and API contract → dependent frontend.
- Gates → review → human approval → commit.

**Parallel:**
- Independent documentation analysis and read-only research.
- Test preparation alongside implementation.
- Separate slices whose contracts are frozen, using worktree isolation for parallel writers.

**Single-writer hotspots** (only one agent at a time; the Orchestrator integrates):
- `Program.cs`;
- the `DependencyInjection.cs` files;
- `StoredProcedureErrors.cs`;
- `<Module>Procedures.cs`;
- `FarmFmsDbContext`;
- `Persistence/Migrations/`.

## 5. Hand-offs and stop rule

Every agent returns a hand-off in the format of skill `farmfms-handoff`:
1. Scope.
2. Completed work.
3. Files created / modified / deleted.
4. Commands and actual results.
5. FR / AD / DBG / CQ covered.
6. Open questions.
7. Blockers.
8. Decisions for human approval.

The Orchestrator re-runs important verifications itself.

**Agents stop and report** when:
- a requirement is unclear;
- an approved specification contradicts itself;
- a database dependency is undefined;
- a security decision is pending;
- the work would need an unapproved architecture change.

They never invent business requirements.

## 6. Quality gates

| Gate | Pass criteria | How |
|---|---|---|
| G0 Specification | Spec approved by the human; FR / AD / DB references present; no unresolved P1 clarification for the slice | Human |
| G1 Build | Release build, 0 errors, 0 warnings | `run_gates.py` |
| G2 Unit tests | All pass; new behaviour covered | `run_gates.py` + review |
| G3 Architecture tests | All pass: layers, inline SQL, FKs, naming, `dbo`, SP conventions, secrets, OpenAPI packages | `run_gates.py` |
| G4 Database review | Migration script reviewed (`dotnet ef migrations script`, generated, **not applied**); conventions; no unapproved schema change | database-engineer → code-reviewer |
| G5 Security | No credentials in source or diff; `dotnet list package --vulnerable`; security review of the change | Orchestrator + code-reviewer |
| G6 Independent review | Fresh-context reviewer; all High findings resolved | code-reviewer |
| G7 Integration tests | Only with a dedicated non-production test database; never production data | qa-engineer (**blocked**: no test database, AD-23) |
| G8 Human approval | Human reviews; only the human commits / pushes | Human |

Builds and tests use the **Release** configuration, because Visual Studio debug sessions lock the Debug output. A gate is reported as passed only with real output.

## 7. Security boundaries

| Control | Mechanism | Limitations |
|---|---|---|
| No `git commit` / `git push` / force push / history rewrite / `reset --hard` / `clean` / `checkout -- .` | `permissions.deny` in `.claude/settings.json` + PreToolUse hook `.claude/hooks/guard_bash.py` | Pattern-based: obfuscated or indirect commands (scripts, aliases, other interpreters) may not be detected. |
| No secret disclosure (`dotnet user-secrets list`, `secrets.json`, printing environment variables) | Hook + `permissions.deny` on reading `secrets.json` | Same as above; a file could still be read by a program the hook does not recognise. |
| Database changes need approval (`dotnet ef database update`, `sqlcmd`, `Invoke-Sqlcmd`, migration bundles) | Hook returns "ask"; `permissions.ask` | Approval is per command; the human must check the target database. |
| No production database operations | Hook denies database commands that mention production markers | Only obvious markers are detected; production credentials must simply not be available on developer machines. |
| Least-privilege tools per agent | Agent `tools` / `disallowedTools` frontmatter; no agent has the `Agent` tool | Tool lists are coarse (a shell can still write files). |
| Per-agent write paths; read-only reviewer | Hook checks Edit / Write by `agent_type`; the reviewer's shell is limited to inspection commands | Shell-based writes by implementation agents are not path-checked; agents are denied all database-changing commands (the main session gets "ask"). |
| Permission rules for subagents | `.claude/settings.json` | Known Claude Code issue: subagents may not reliably inherit `permissions` rules. The hook (which runs in subagents) is the primary guard. |

**Hooks and permission rules are guard rails, not a security boundary.** They reduce mistakes by well-behaved agents. The real controls are:
- credentials kept out of the repository and the shell environment;
- no production access from development machines;
- human review before commit.

**Known unresolved issue:** a SQL credential is in public Git history (commits `5ffe3f2`..`f4b75fe`). Rotating the credential and cleaning the history are human decisions and are **not** resolved by this setup.

## 8. Module 01 – planned sequence (not authorized)

| Phase | Scope | Precondition |
|---|---|---|
| P0 | Agentic infrastructure (this setup) | Done when the human approves this configuration |
| P1 | Module 01 readiness: analysis, implementation specification, dependency / decision resolution (ProcessId DBG-002, Territory, audit log DBG-008, SYSTEM user AD-14, missing procedures DBG-012, seed data DBG-014) | Explicit human authorization |
| P2 | Database implementation (EF configurations, migration, SP scripts) | G0 |
| P3 | Roles and permissions | **After P5 authentication**: no unauthenticated admin APIs |
| P4 | User and account management | After P5; AD-11 approved |
| P5 | Authentication and security (Step 04): password protection (AD-11), JWT (AD-12), sessions, authorization (AD-13) | AD-11 approval; AD-12 / AD-13 confirmed |
| P6 | Frontend Module 01 | Frontend foundation (Step 03) approval |

Admin-endpoint phases (P3, P4) are implemented only after P5 provides authentication and authorization, even though they are numbered earlier.
