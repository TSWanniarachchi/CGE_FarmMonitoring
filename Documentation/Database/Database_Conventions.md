# Farm FMS – Database Conventions

| | |
|---|---|
| Status | Decisions marked **Approved** are approved in `FarmFMS_Architecture_Decisions_v2.md` (naming updated 2026-11-04: approved prefixes; the 15-character limit removed). Items marked **Pending Approval** or **Blocked** are not decided. |
| Sources | Module 01 Database Specification v0.2 (`Farm_FMS_Module_01_Database_Specification_v0.2.xlsx`), PMS standard (`SQL Server Best Practices - Prima Management Services Pvt Ltd.pptx`), `Documentation/Architecture/FarmFMS_Architecture_Decisions_v2.md` (AD-04, AD-07, AD-08, AD-09, AD-14, AD-24) – the authoritative decision record |
| Enforced by | `Backend/tests/FarmFMS.ArchitectureTests`, `Backend/tests/FarmFMS.UnitTests` |

This document doesn't define any table, column or procedure. Those come only from the approved database specification.

---

## 1. Precedence

1. The **approved Module 01 database specification**, where it explicitly overrides the general standard.
2. The **approved Farm FMS decisions** (`FarmFMS_Architecture_Decisions_v2.md`), e.g. the object-name prefixes in §3, which replace the PMS `Cpk`/`Cun`/`Cck`/`I{u|n}{c|n}` forms.
3. The **PMS SQL Server naming standard**.
4. The conventions in this document, for what none of the above defines.

## 2. Schema (AD-24, Approved)

- `dbo` is the default and only schema. No other schema is inferred or created.
- Every Stored Procedure reference is dbo-qualified: `dbo.ScrAppUser` (C# constants and SQL scripts alike).

## 3. Identifier rules (AD-08, Approved)

**There is no custom length limit.** Names are meaningful and descriptive. The only limit is SQL Server's identifier maximum of 128 characters. Names are never truncated, and generated names are deterministic and unique. (The earlier 15-character rule is withdrawn.)

| Object | Rule | Example |
|---|---|---|
| Every identifier | PascalCase, descriptive, within SQL Server's 128-character limit | — |
| Table | Singular, as in the spec; letters/digits only; **no `Lk` prefix** for link tables (spec overrides PMS) | `AppUser`, `UserRole` |
| Column | As in the spec; letters/digits only; reference columns **`UserId`-style** (spec overrides PMS `Customer_Id`) | `UserId`, `RoleId` |
| Primary key | `PK{Table}` | `PKRoleMenuAccess` |
| Unique constraint | `UQ{Table}_{ColumnOrColumns}` | `UQUserRole_UserIdRoleId` |
| Index | `IX{Table}_{ColumnOrColumns}` | `IXLoginHistory_UserIdEventDate` |
| DEFAULT constraint | `DF{Table}_{Column}`; always named explicitly (SQL Server's generated `DF__…` names are not deterministic) | `DFAppUser_IsActive` |
| Check constraint | `CK{Table}_{Column}` | `CKLoginHistory_EventType` |
| Foreign key | **Never created** (AD-07) | — |
| Stored Procedure | `dbo.` + `Scr`/`Sre`/`Sup`/`Sde`/`Sva` + PascalCase name; never `sp_`; see §3.2 | `dbo.SreUsrPwdView` |

`{Table}` and `{Column}` are the full specification names. Multiple columns are concatenated in key order (as in the PMS example `InnOrderDetails_OrderIdCustomerId`); column order is part of the name.

In EF Core configurations, names are set explicitly (`HasName`, `HasDatabaseName`, `HasCheckConstraint`, property `SetDefaultConstraintName`) using `Infrastructure/Persistence/Naming/DbObjectNames` (`PrimaryKey`, `Unique`, `Index`, `Default`, `Check`).

### 3.1 Abbreviations (Approved)

**Approved abbreviations, complete list** (preserved; do not introduce others without documenting and approving them), held in `Infrastructure/Persistence/Naming/DbAbbreviations.cs`:

| Word | Abbreviation | Origin |
|---|---|---|
| Activity | Act | spec `LastActDate` |
| Deactivate | Deact | spec `SupUserDeact` |
| Encrypted | Enc | spec `EncPassword` |
| Hierarchy | Hier | spec `SupUsrHier` |
| History | Hist | spec `ScrLoginHist` |
| Password | Pwd | spec `SreUsrPwdView`, `PwdChangeReq` |
| Permission | Perm | spec `SvaUsrPerm` |
| Required | Req | spec `PwdChangeReq` |
| Revoke | Rev | spec `SupUserSessRev` |
| Role | Rol | approved after the readiness audit |
| Session | Sess | spec `SupUserSessRev` |
| Territory | Terr | spec `ScrUserTerr` |
| User | Usr | spec `SreUsrPwdView`, `SupUsrHier`, `SvaUsrPerm` |

**Readability comes first:** abbreviations are not applied unnecessarily.
- Key, constraint and index names use the **full** table and column names.
- The list is used where an approved rule requires it (`Usr` in new procedure names, §3.2).
- `DbObjectNames` applies it only as a fallback if a full name would exceed 128 characters. If the name still doesn't fit, the builder throws; it never truncates.
- EF Core itself truncates some over-128 identifiers (appending `~`); the architecture tests reject such names.

### 3.2 Stored Procedure names: Usr vs User (Approved)

- The Module 01 procedure names are preserved **exactly** as in specification v0.2: `ScrAppUser`, `SreAppUserById`, `SreAppUserList`, `SupAppUser`, `SvaAppUserLogin`, `SvaUsrPerm`, `ScrUserRole`, `SdeUserRole`, `ScrUserAccount`, `ScrUserTerr`, `SupUsrHier`, `ScrLoginHist`, `SupUserUnlock`, `SupUserDeact`, `SupUserSessRev`, `SreUsrPwdView`.
- **Exception:** these specification names are the only procedure names that may contain the word `User`. They are not renamed to `Usr`.
- **Newly introduced** procedure names use `Usr` (for example a new read procedure on `AppUser` would use `AppUsr`), unless an approved specification explicitly requires otherwise.
- Enforced by the architecture tests. The preserved list is compared with the workbook's "Stored Procedures" sheet, and any other procedure name (C# constants or SQL scripts) containing the word `User` fails.

### 3.3 Module 01 object names (reviewed; not created)

These are the 40 keys, constraints and indexes implied by specification v0.2 (table definitions and the *Indexes & Rules* sheet), named with `DbObjectNames`. They are names only; no database object has been created. `Backend/tests/FarmFMS.ArchitectureTests/Module01ObjectNameTests` checks that each name is generated exactly as listed, is valid, and that **no two names collide**.

| Table | Primary key | Unique constraints | Indexes | DEFAULT constraints | Check constraints |
|---|---|---|---|---|---|
| `AppUser` | `PKAppUser` | `UQAppUser_Username` | — | `DFAppUser_IsActive`, `DFAppUser_FailedLogCount`, `DFAppUser_IsLocked`, `DFAppUser_PwdChangeReq` | — |
| `Role` | `PKRole` | `UQRole_Name` | — | `DFRole_IsSystemRole`, `DFRole_IsActive` | — |
| `Permission` | `PKPermission` | `UQPermission_Name`, `UQPermission_Code` | — | `DFPermission_IsActive` | — |
| `Menu` | `PKMenu` | `UQMenu_Code` | `IXMenu_ParentMenuIdDisplayOrder` | `DFMenu_DisplayOrder`, `DFMenu_IsActive` | `CKMenu_Platform` ¹ |
| `UserRole` | `PKUserRole` | `UQUserRole_UserIdRoleId` | — | — | — |
| `RoleMenuAccess` | `PKRoleMenuAccess` | `UQRoleMenuAccess_RoleIdMenuIdPermissionId` | `IXRoleMenuAccess_RoleIdMenuId` ² | — | — |
| `UserAccount` | `PKUserAccount` | `UQUserAccount_UserId` | — | `DFUserAccount_CanAccessWeb`, `DFUserAccount_CanAccessMobile`, `DFUserAccount_IsActive` | — |
| `UserTerritory` | `PKUserTerritory` | `UQUserTerritory_UserAccountIdTerritoryId` | — | — | — |
| `UserHierarchy` | `PKUserHierarchy` | — | `IXUserHierarchy_ASMAccountId` ³ | `DFUserHierarchy_IsActive` | — |
| `LoginHistory` | `PKLoginHistory` | — | `IXLoginHistory_UserIdEventDate` | — | `CKLoginHistory_EventType` ¹ |
| `UserSession` | `PKUserSession` | `UQUserSession_TokenHash` | — | — | — |

Totals: 11 primary keys, 10 unique constraints, 4 indexes, 13 DEFAULT constraints, 2 check constraints = **40**. The longest name is `UQRoleMenuAccess_RoleIdMenuIdPermissionId` (41 characters), well within 128. No collisions.

Notes:
1. The check constraints apply only if check constraints are used for the specification's "(proposed)" code lists (`Menu.Platform`, `LoginHistory.EventType`).
2. The index on `RoleMenuAccess (RoleId, MenuId)` repeats the leading columns of the unique constraint `UQRoleMenuAccess_RoleIdMenuIdPermissionId` and may be redundant. Kept as specified; noted for review.
3. Filtered unique index (one current manager per ASM, specification *Indexes & Rules*). It is an index, so the `IX` prefix applies.
4. DEFAULT constraints for `CreatedDate` (UTC) are not listed: the specification does not say whether those columns get a DEFAULT.

## 4. Foreign keys (AD-07, Mandatory)

No physical `FOREIGN KEY` constraint in any artifact: EF model, EF migrations or SQL scripts. Logical references are validated inside Stored Procedures, which raise a registered business error (§6) when a referenced row is missing or inactive.

## 5. Stored Procedure scripts and deployment (AD-04, Approved)

- EF Core migrations manage the **schema only**. Migrations must not call `migrationBuilder.Sql(...)` and never create or alter procedures.
- Each Stored Procedure is a **separate version-controlled SQL script**:
  ```
  Database/
    StoredProcedures/
      <Module>/
        dbo.<ProcedureName>.sql
  ```
- Scripts are idempotent: `CREATE OR ALTER PROCEDURE dbo.<Name>`.
- Each script starts with a header comment (description, author, date, specification version).
- Scripts use parameters only (no dynamic SQL built from input), `SET NOCOUNT ON`, explicit transactions for multi-statement writes, `SYSUTCDATETIME()` for timestamps, and `THROW` per §6 for errors.
- Deployment order: (1) apply EF migrations; (2) run the procedure scripts. The deployment tooling and pipeline are **Pending Approval** (they come with the first procedures).

## 6. Stored Procedure error contract (AD-09, Approved)

| `THROW` number | Use | What the API client receives |
|---|---|---|
| **50000** | Generic / unexpected error | HTTP 500 with the generic message "A database error occurred. Please try again later." (`Database.Error`) |
| **50001–50999** | Known business errors. **Every number used must be registered** in `Backend/src/FarmFMS.Infrastructure/Data/StoredProcedureErrors.cs` with a code, error type and safe message | The registered message, code and HTTP status |
| 50001–50999, not registered | Programming error | HTTP 500 generic message |
| any other number | Unknown | HTTP 500 generic message |

Rules:
- **The `THROW` message text is never shown to API clients.** Write it for operators (it reaches the server log only for unregistered or unknown errors). Registered business errors are logged by number and code only.
- Business error types: Validation (400), Unauthorized (401), Forbidden (403), NotFound (404), Conflict (409). Failure and Unavailable are reserved for generic and technical errors.
- Numbers and codes are unique; the catalogue is validated when the API starts.
- SQL Server's own errors are also mapped centrally: unique violations 2601/2627 → 409, constraint violation 547 → 409, deadlock 1205 / timeout / connectivity → 503. Everything else → 500 generic.
- Business error catalogue (registered numbers): **none yet**. Entries are added only together with their approved procedure, grouped by module.
- **Pending Approval:** optional per-module sub-ranges inside 50001–50999; the result shape for write procedures (output parameter vs single-row result).

Example (illustrative only; this number is not registered):
```sql
IF EXISTS (/* duplicate check */)
    THROW 50101, N'Username already exists (operator detail; never shown to clients).', 1;
```

## 7. Audit columns and the System user (AD-14, Approved)

- `CreatedBy` / `ModifiedBy` are `INT` user IDs (specification). The API passes them as procedure parameters; clients never send them.
- Authenticated operations use the authenticated user's ID. Background / system operations use the dedicated **System user** `AppUser` record created by the controlled seed process. Its ID is configured per environment (`SystemUser:UserId`) and never hard-coded.
- A write without a defined identity is rejected by the API before any procedure is called.
- Timestamps are UTC (`SYSUTCDATETIME()`).

## 8. Data access from the API

- Only `IStoredProcedureExecutor` runs SQL. It executes Stored Procedures only (fixed command type, validated dbo-qualified PMS name), with parameters (never concatenated values).
- Connections come only from `IDbConnectionFactory`. EF Core's `FarmFmsDbContext` is never used for application data.

## 9. Verification status

The rules above are verified statically (architecture and unit tests). **Execution against a live SQL Server has not been verified.** Integration tests for procedures, transactions and `SqlException` translation are pending until a SQL Server integration test database is available.
