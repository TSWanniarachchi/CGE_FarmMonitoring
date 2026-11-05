---
name: farmfms-orchestrate
description: Lead Orchestrator runbook for Farm FMS. Use in the main session before delegating any Farm FMS work to the project agents (business-analyst, solution-architect, database-engineer, backend-developer, frontend-developer, qa-engineer, code-reviewer) — plans the slice, sequences agents, manages single-writer hotspots, verifies hand-offs and gates.
---

# Farm FMS – Lead Orchestrator runbook

The main session is the Lead Orchestrator. Subagents cannot talk to each other and cannot start other agents; every hand-off returns here.

## 1. Before anything starts
1. **Authorization:** confirm the user explicitly authorized this module / slice. Module 01 is **not authorized** until the user says so. Authentication (Step 04) comes before any admin API.
2. **Baseline:** run `git status` and note uncommitted user changes (never discard or overwrite them). Run the gates (`farmfms-quality-gates`) to record the starting state.
3. **Specification:** implementation work needs an approved spec in `Documentation/Specs/<Module>/` (G0). Without one, start at step 1 of the lifecycle.

## 2. Slice lifecycle and delegation
| # | Agent | Give it | Expect back |
|---|---|---|---|
| 1 | `business-analyst` | Module / feature, FR IDs, relevant SRS sections | Acceptance criteria, traceability updates, CQs |
| 2 | `solution-architect` | Analyst hand-off, approved DB spec, ADs | Spec in `Documentation/Specs/<Module>/` → **user approval (G0)** |
| 3 | `database-engineer` | Approved spec sections | EF configurations, migration (not applied), SP scripts, catalogue entries |
| 4 | `backend-developer` | Approved spec + database hand-off | Repositories, services, DTOs, controllers, unit tests |
| 5 | `qa-engineer` | Acceptance criteria (can run alongside 4) | Tests + verification report |
| 6 | `frontend-developer` | Frozen API contract (only after frontend foundation approval) | UI slice |
| 7 | you | — | Gates G1–G5 with `run_gates.py`; fix loop via the owning agent |
| 8 | `code-reviewer` | Diff scope + spec (fresh context) | Severity-ranked findings (G6) |
| 9 | user | Gate + review summary | Approval; the user commits |

Delegation prompt must include: scope, authorization reference, spec path and sections, files the agent may touch, hotspot permission (yes/no), required hand-off (`farmfms-handoff`).

## 3. Parallel work
- Allowed: independent read-only research, documentation analysis, test preparation, separate slices with frozen contracts.
- Parallel **writers** use worktree isolation (`isolation: "worktree"` on the Agent call) and never share a hotspot.
- **Single-writer hotspots** (one agent at a time; you integrate): `Program.cs`, `*/DependencyInjection.cs`, `StoredProcedureErrors.cs`, `<Module>Procedures.cs`, `FarmFmsDbContext.cs`, `Persistence/Migrations/`.

## 4. Verify, don't trust
- Re-run `python .claude/skills/farmfms-quality-gates/run_gates.py` after every implementation hand-off.
- Check the diff (`git diff --stat`, `git status`) against the files the agent reported.
- A gate is "passed" only with real output in this session.

## 5. Stop and escalate to the user
Unclear requirement · contradictory approved spec · undefined database dependency · pending security decision · unapproved architecture change · any request to commit, push, apply a migration, touch a database or read secrets.

## 6. Never
Commit, push, rewrite history, apply migrations or run database scripts without explicit approval for that command; read or print secrets; let two agents write the same hotspot; report success without evidence.
