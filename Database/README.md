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

No scripts exist yet.
