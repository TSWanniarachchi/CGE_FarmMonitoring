---
name: farmfms-stored-procedure
description: Rules and template for Farm FMS Stored Procedure scripts, error catalogue entries and procedure-name constants (AD-04, AD-06, AD-07, AD-08, AD-09, AD-24, Database_Conventions.md). Use when the database-engineer writes or reviews a procedure, and when the code-reviewer checks gate G4.
---

# Farm FMS – Stored Procedures

## Location and naming
- One file per procedure: `Database/StoredProcedures/<Module>/dbo.<ProcedureName>.sql`.
- Name: `dbo.` + prefix `Scr` (create) / `Sre` (read) / `Sup` (update) / `Sde` (delete) / `Sva` (validate) + PascalCase; ≤ 128 chars.
- Module 01 names are preserved **exactly** as in specification v0.2 (only these may contain `User`). New names use `Usr`, not `User`.
- C# constants: `public const string X = "dbo.Name";` in `static class <Module>Procedures` (Infrastructure/Repositories/<Module>/).

## Script template
```sql
/*
 * Description  : <what the procedure does>
 * Author       : <name>
 * Created Date : <dd-MM-yyyy>
 * Specification: <document + version + sheet/section>
 */
CREATE OR ALTER PROCEDURE dbo.ScrExample
    @Name        NVARCHAR(100),
    @CreatedBy   INT,
    @NewId       INT OUTPUT            -- insert: @NewId; update/delete: @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Logical reference checks (no physical FKs, AD-07); business rule failures:
    -- THROW <registered number in the module range>, N'<operator text, never shown to clients>', 1;

    BEGIN TRANSACTION;
        INSERT INTO dbo.Example (Name, CreatedBy, CreatedDate)
        VALUES (@Name, @CreatedBy, SYSUTCDATETIME());
        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;
END
```

## Rules
- Parameters only; **no dynamic SQL** built from input; no `SELECT` rows for write metadata (AD-09 output parameters: `@NewId`, `@RowsAffected`; the executor adds them — callers use `ExecuteForNewIdAsync` / `ExecuteForRowsAffectedAsync`).
- `SET NOCOUNT ON`; explicit transaction for multi-statement writes; UTC via `SYSUTCDATETIME()`.
- No `FOREIGN KEY` / `REFERENCES`; validate referenced rows (and active state where the spec says) inside the procedure; check dependents before delete / deactivate.
- Errors: `THROW 50000` generic; business errors in the module range (Module 01 50001–50099, Module 02 50100–50199, Module 03 50200–50299, shared 50300–50999), each **registered** in `StoredProcedureErrors.cs` with `new(StoredProcedureErrorRange.ModuleNN, number, Error.<Type>(code, safeMessage))`. Allowed types: Validation, NotFound, Conflict, Unauthorized, Forbidden.
- Never return password ciphertext except through the dedicated restricted procedure defined by the spec; never log parameter values.
- Deployment: EF migrations first, then scripts — executed only by the user / approved process. Agents never run scripts against a database.

## Checked automatically
Architecture tests check every `*.sql` under `Database/` for FKs and procedure names, and C# procedure constants for naming and the `User` rule.
