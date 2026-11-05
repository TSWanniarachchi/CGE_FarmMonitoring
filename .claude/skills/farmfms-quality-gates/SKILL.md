---
name: farmfms-quality-gates
description: Farm FMS quality gates G0–G8 with exact commands and pass criteria. Use before reporting any implementation, review or hand-off as complete, and to record a baseline before work starts. Runs the Release build, unit tests and architecture tests via run_gates.py.
---

# Farm FMS – Quality gates

A gate passes **only with real output from this session**. Use the Release configuration (Visual Studio locks Debug output).

## Automated: G1–G3
```bash
python .claude/skills/farmfms-quality-gates/run_gates.py              # Backend build + unit + architecture tests
python .claude/skills/farmfms-quality-gates/run_gates.py --frontend   # also build the Frontend solution
python .claude/skills/farmfms-quality-gates/run_gates.py --build-only
```
Underlying commands (the only ones the runner executes):
`dotnet build Backend/FarmFMS.sln -c Release -nologo` · `dotnet test Backend/FarmFMS.sln -c Release --no-build -nologo`.
Exit code 0 = all executed gates passed. The runner masks credential-like text and never touches databases.

| Gate | Pass criteria |
|---|---|
| G1 Build | 0 errors **and** 0 warnings (Release) |
| G2 Unit tests | `FarmFMS.UnitTests`: 0 failed; new behaviour has tests (checked at review) |
| G3 Architecture tests | `FarmFMS.ArchitectureTests`: 0 failed (layers, inline SQL, FKs, naming, `dbo`, SP conventions, secrets in settings, OpenAPI packages) |

## Manual / review gates
| Gate | How | Pass criteria |
|---|---|---|
| G0 Specification | User approves `Documentation/Specs/<Module>/…` | FR / AD / DB references present; no open P1 CQ for the slice |
| G4 Database | `dotnet ef migrations script --idempotent --project Backend/src/FarmFMS.Infrastructure --startup-project Backend/src/FarmFMS.API --output <scratch>.sql` (generates SQL only; **never** `database update`) | Matches the approved spec and `Database_Conventions.md` names; no FKs, no `migrationBuilder.Sql`; SP scripts follow `farmfms-stored-procedure` |
| G5 Security | `git diff` reviewed for credentials (report counts/paths, never values); `dotnet list Backend/FarmFMS.sln package --vulnerable --include-transitive`; security review of the change | No credentials; no unaddressed vulnerable package; no High security finding |
| G6 Review | `code-reviewer` agent, fresh context | All High findings resolved |
| G7 Integration | Only with a dedicated non-production test database (AD-23) | **Blocked** until one exists; never production data |
| G8 Human | User reviews the diff and gate report | Only the user commits and pushes |

## Reporting
State each gate as PASS / FAIL / NOT RUN / BLOCKED with the command and the key output line (e.g. `0 Warning(s) 0 Error(s)`, `Passed: 228, Failed: 0`).
