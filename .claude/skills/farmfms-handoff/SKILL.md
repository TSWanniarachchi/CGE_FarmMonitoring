---
name: farmfms-handoff
description: Required structured hand-off format every Farm FMS agent returns to the Lead Orchestrator at the end of a task (scope, work, files, commands with real results, IDs covered, questions, blockers, decisions). Use when finishing or stopping any delegated Farm FMS task.
---

# Farm FMS – Hand-off note

Return exactly these sections (write "None" when empty). Report facts only; never paste secrets, connection strings or credential values.

```markdown
## Hand-off: <agent> – <task title>
1. **Task scope** – what was asked; authorization / spec reference.
2. **Completed work** – what was done (and what was deliberately not done).
3. **Files** – Created: … · Modified: … · Deleted: … (repo-relative paths)
4. **Commands and actual results** – each command run + key output line
   (e.g. `dotnet test … -c Release` → `Passed: 230, Failed: 0`). "Not run" if not run.
5. **IDs covered** – FR-…, AD-…, DBG-…, CQ-…, spec sections.
6. **Open questions** – with document + section that raised them.
7. **Blockers** – what stopped work and why (stop rule).
8. **Decisions requiring human approval** – each with options only if a source documents them.
```

Status words: **Done (verified)** only with command output; **Done (not verified)** when nothing could be run; **Stopped** when the stop rule applied.
