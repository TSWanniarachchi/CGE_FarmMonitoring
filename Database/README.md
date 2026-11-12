# Database

Version-controlled SQL scripts for Farm FMS.

```
Database/
  StoredProcedures/
    <Module>/
      dbo.<ProcedureName>.sql     # one CREATE OR ALTER PROCEDURE per file
```

- **Schema** (tables, keys, indexes, constraints) is owned by EF Core migrations in `Backend/src/FarmFMS.Infrastructure`.
- **Stored Procedures** live here as separate scripts. They are **never** deployed through EF migrations.
- Deployment order: apply EF migrations, then run the procedure scripts.
- No physical `FOREIGN KEY` constraints in any script.
- Names follow the Module 01 specification and the approved conventions: `dbo` schema, Scr/Sre/Sup/Sde/Sva prefixes, descriptive names (no custom length limit; SQL Server's 128-character maximum).
- Errors: `THROW 50000` for generic errors, `THROW 50001–50999` for business errors registered in `Backend/src/FarmFMS.Infrastructure/Data/StoredProcedureErrors.cs`.

Full rules: `Documentation/06_Database/Database_Conventions.md`. The architecture tests (`Backend/tests/FarmFMS.ArchitectureTests`) check every `*.sql` file here for foreign keys and invalid procedure names.

## Module 01 – Step 0 (Authentication foundation)

Scripts: `StoredProcedures/Module01/` (15 procedures; contract in `Documentation/Specs/Module01/Step00_Authentication_Foundation_Spec.md` §5).

Deployment order (Development only, D-31). **Every step is a database change and needs explicit user approval for that command:**

1. Apply the EF Core migration `Module01AuthenticationFoundation` (`dotnet ef database update ...`).
2. Run every script in `StoredProcedures/Module01/` (each is `CREATE OR ALTER`, so re-running is safe).
3. Run the Development-only bootstrap (SYSTEM user + first Administrator) – see `Backend/CLAUDE.md`.

Requirements and limits:
- The database must use a **case-insensitive collation** (username matching relies on it, D-21).
- The scripts have only been checked statically (architecture tests). They have **not** been executed against SQL Server: no test database exists (AD-23, gate G7 blocked).
