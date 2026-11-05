# Farm FMS – Module 01 Step 0: Authentication Foundation (Backend) – Implementation Specification

## 0. Document control

| | |
|---|---|
| Document | Module 01 – Step 0 – Authentication foundation (backend only) |
| Version | 0.1 |
| Date | 2026-11-05 |
| Status | **Draft – Pending Approval** (G0). Only the user changes this status. |
| Author | Solution Architect agent |
| Authorization | User authorized **Step 0 only** on 2026-11-05 (`Documentation/Dev_RoadMap/DEVELOPMENT_ROADMAP.md` §4, §7): "Implement only the authentication backend foundation required for the Login Page, including its necessary APIs, services, Stored Procedures, token handling, session handling, and security requirements. Do not develop the Login Page or any other page." |

**Sources**

| Source | Version / status | Used for |
|---|---|---|
| `Documentation/06_Database/Farm_FMS_Module_01_Database_Specification_v0.2.xlsx` | v0.2, approved column baseline | Tables, columns, procedure names, *Confirmed Decisions*, *Indexes & Rules*, *Read Me* |
| `Documentation/Architecture/FarmFMS_Architecture_Decisions_v2.md` | Per decision | AD-02, AD-04–AD-18, AD-21, AD-23, AD-24 |
| `Documentation/Architecture/FarmFMS_Reference_Mapping.md` | Proposed | B-03, B-06, B-08, B-10, B-12–B-15 |
| `Documentation/Security/Password_Encryption_Design.md` | v0.1 Draft; §11.1 login-path items approved 2026-11-05 (P-01, P-02 NFC, P-03 Option A, P-07, P-08 Development only, P-23) | Password verification |
| `Documentation/06_Database/Database_Conventions.md` | Approved per section | Object names (§3.3), error contract (§6), audit (§7) |
| `Documentation/Requirements/Software_Requirements_Specification.md` (SRS) | Draft v0.2 | FR-UM, FR-AU, NFR (supporting only) |
| `Documentation/Requirements/Functional_Requirements.md` (FRD) | Draft | FR-UM (supporting only) |
| `Documentation/Business_Design/Business_Clarification_Register.md`, `Documentation/06_Database/Database_Gap_Analysis.md` | Analysis | CQ-035, CQ-036, CQ-037, DBG-002, DBG-007, DBG-012, DBG-014 |

**Reading rule.** Everything marked **[D-nn]** is a proposal that needs the user's decision (§12). Nothing marked [D-nn] may be implemented before G0. The Module 01 specification defines procedure **names** only, not parameters or result sets; every signature in §5 is therefore a proposal (D-02).

---

## 1. Scope

### 1.1 In scope (backend only)
1. Login (username + password) with AES-256-GCM password verification (approved login-path items only).
2. JWT access token issuing and validation; opaque refresh token, stored only as a SHA-256 hash in `UserSession.TokenHash`, rotated on every refresh, with reuse detection (AD-12).
3. Session handling: 30-minute inactivity timeout enforced server-side via `UserSession.LastActDate` (spec *Confirmed Decisions*; AD-21), absolute session lifetime [D-09], concurrent sessions allowed.
4. Logout (revoke the session; `LoginHistory` EventType 3).
5. Lockout after 5 consecutive failed attempts (spec); no unlock endpoint (Admin unlock is a later step).
6. `LoginHistory` writes for success, failure and logout (spec; FRD FR-UM-016).
7. Platform access check (`UserAccount.CanAccessWeb`) on login and on every protected request (spec *Read Me* → Authorization).
8. Effective-permission resolution (`SvaUsrPerm`) and the authorization **mechanism**: secure-by-default, permission-based policies (AD-13). No business policies are defined in Step 0 (no business endpoints exist).
9. `GET api/auth/me` (current user + effective permissions) for the later Login Page / auth state (AD-21, B-14).
10. Configuration and secrets: JWT signing key and password key ring in user-secrets (Development), validated at startup (AD-17, B-15).
11. Rate limiting of login/refresh/logout; logging redaction (AD-16).
12. EF Core configurations and **one** migration for the tables Step 0 needs [D-27]; Stored Procedure scripts for §5.
13. Development bootstrap of the SYSTEM user and one initial System Administrator account [D-19, D-20] (execution only with explicit user approval).

### 1.2 Out of scope
- Login Page and any other UI; frontend auth state (AD-21 port) – Step 1 / Step 03 frontend foundation.
- Any admin API: user CRUD, unlock (`SupUserUnlock`), deactivate (`SupUserDeact`), role/menu/permission management, password reset/change/reveal (`SreUsrPwdView`), login-history query UI, audit log.
- Territory scope enforcement (spec *Read Me* requires it on protected requests; there is no territory data or territory-scoped endpoint in Step 0 – Step 11; DBG-003).
- Key rotation/re-encryption (P-06, P-09–P-11), production secret store (P-08), restricted SQL principal (P-14).
- Mobile login (CanAccessMobile) [D-13].
- SQL Server integration tests (AD-23: no test database).

### 1.3 Dependencies
| Dependency | State | Impact |
|---|---|---|
| AD-12, AD-13 (Proposed) | Need approval | Basis of this spec [D-01] |
| AD-14 SYSTEM user record + lookup procedure | **Blocked** in AD-14 | Actor for unauthenticated login writes [D-18, D-19] |
| Process master (owner: another module; DBG-002) | **Undefined** | `UserAccount.ProcessId` is NOT NULL; the bootstrap admin needs a `UserAccount` to pass the platform check [D-20] |
| Role / Permission / Menu seed data (DBG-014) | Undefined; spec has **no** create procedures for `Role`, `Permission`, `Menu`, `RoleMenuAccess` | Bootstrap admin has no roles in Step 0; permissions mechanism tested with unit tests only [D-20] |
| P-06 (password set procedure) | Pending | P-03 Option A (AAD = `AppUser.Id`) requires insert-then-set-payload; needed for the bootstrap seed [D-20] |
| SQL Server test database (AD-23) | Missing | G7 blocked; procedures verified statically only |

---

## 2. Requirements covered

Draft sources only (SRS and FRD are drafts; numbering differs between them – see C-03). The binding rules come from the Module 01 spec *Confirmed Decisions*.

| ID | Source | Requirement (abridged) | Step 0 acceptance criteria |
|---|---|---|---|
| FR-UM-001 | SRS / FRD (Confirmed) | Authenticate with username and password | AC-01, AC-02 |
| FR-UM-002, FR-UM-004, FR-UM-006 (FRD); FR-UM-002, FR-UM-006 (SRS); NFR-008 | Draft | RBAC; restrict access by permissions; validate authorization for protected operations | AC-12, AC-13, AC-14 |
| FR-UM-016 (FRD); FR-UM-008 (SRS); FR-AU-002; NFR-011 | Draft | Successful, failed and logout history | AC-08 |
| FR-UM-018 (FRD) + spec *Lockout* | Spec confirmed | Lock after 5 failed attempts; Admin unlock | AC-05, AC-06 |
| FR-UM-019 (FRD); FR-UM-010 (SRS, Pending Definition) + spec *Session timeout* + AD-21 | Spec confirmed | 30 minutes inactivity | AC-10 |
| FR-UM-020 (FRD) + spec *Deactivation* | Spec confirmed | Deactivation blocks login and active sessions | AC-03, AC-11 (blocking only; the deactivate operation itself is a later step) |
| FR-UM-022 (FRD, Needs Clarification) + spec *Platform* | Spec confirmed | CanAccessWeb / CanAccessMobile; both disabled by default | AC-04, AC-11 |
| NFR-009, NFR-012 (Pending Definition), CQ-036 | Draft | Credential protection per approved design | AC-02, AC-15, AC-16 |
| Spec *Concurrent sessions: Allowed*, *MFA: Not required* | Spec confirmed | No session cap, no MFA | AC-01 |

---

## 3. Design overview

### 3.1 Token and session model (AD-12, AD-21) [D-07, D-08, D-09, D-10]
- **Access token:** JWT, HS256, lifetime 10 minutes [D-09, D-23]. Claims: `sub` (AppUser.Id), `sid` (UserSession.Id), `name` (Username), `jti`, `iat`, `nbf`, `exp`, `iss`, `aud`. **No permission claims** [D-11].
- **Refresh token:** 32 random bytes (CSPRNG), sent to the client once as base64url (43 chars). The database stores only `SHA-256(raw 32 bytes)` in `UserSession.TokenHash` (VARBINARY(32), unique) [D-24].
- **One `UserSession` row = one refresh token.** Rotation revokes the presented row (`RevokedReason = 'Rotated'`) and inserts a new row that **inherits `ExpiresDate` and `LastActDate`** [D-07]. A presented token whose row is `Rotated` is **reuse** → all the user's sessions are revoked (`SupUserSessRev`, all-sessions mode).
- **`ExpiresDate`** = absolute session end (login time + absolute lifetime) [D-10].
- **Idle timeout (30 min, spec):** a session is idle-expired when `LastActDate <= now − 30 min`. `LastActDate` is advanced only by **authenticated API requests** (per-request validation, throttled to once per 60 s [D-08]), not by refresh – so a client that refreshes in the background cannot keep an idle session alive.
- **Per-request validation:** after JWT validation, the API calls `SvaUsrSess` with `sid`/`sub`; any non-valid status → 401. This enforces revocation (logout, reuse, future deactivation), idle timeout, absolute expiry, user active/unlocked and platform access on **every** protected request (spec *Read Me* → Authorization).
- All session time comparisons use the database clock (`SYSUTCDATETIME()`); JWT times use `TimeProvider` (UTC) (AD-14).

### 3.2 Login flow (`POST api/auth/login`)
1. Model validation (400). Normalize username [D-21].
2. `SvaAppUserLogin(@Username)` – read-only credential retrieval [D-03].
3. No row → `ScrLoginHist(UserId NULL, Failed, 'UnknownUser')` → **401** `Auth.InvalidCredentials`.
4. `IsActive = 0` → history `Failed/'Inactive'` → 401. No decryption (design §6.2.3).
5. `IsLocked = 1` → history `Failed/'Locked'` → 401 [D-12]. No decryption, **not counted**.
6. `IPasswordProtector.Verify(password, payload, userId)`:
   - `IntegrityFailure` (tag mismatch / invalid lengths) or `UnknownKeyVersion` → history `Failed/'CredentialError'`, **not counted** (P-23), security event logged at Error with user id + KeyVersion only (alert hook, §8.6) → 401.
   - `Mismatch` → in one unit of work: `SupAppUsrLoginFailure` (+1; locks at 5) and `ScrLoginHist(Failed/'InvalidPassword')` → 401. If the account became locked, log `AccountLocked` at Warning.
7. `Match` → platform check: no `UserAccount`, `UserAccount.IsActive = 0` or `CanAccessWeb = 0` → history `Failed/'PlatformDenied'` → **403** `Auth.PlatformAccessDenied` [D-13]; not counted.
8. Success, in one unit of work (AD-05 `IUnitOfWork`): `SupAppUsrLoginSuccess` (reset count, LastLoginDate; 0 rows → roll back → 401), `ScrUsrSess` (new session, `@NewId`, `@ExpiresDate` output), `ScrLoginHist(Success)`. Commit.
9. Issue the access token; return `TokenResponse` (200) with `Cache-Control: no-store`.

### 3.3 Refresh flow (`POST api/auth/refresh`)
1. Decode token (base64url → exactly 32 bytes) else 401. Hash.
2. `SreUsrSessByTokenHash` → no row → 401 (Warning, no user id).
3. `RevokedReason = 'Rotated'` → **reuse**: `SupUserSessRev(UserId, NULL, 'RefreshTokenReuse')` → Warning `RefreshTokenReuse` → 401.
4. Revoked for another reason → 401.
5. Absolute-expired or idle-expired → `SupUserSessRev(UserId, SessionId, 'Expired' | 'IdleTimeout')` → 401.
6. User inactive / locked / no web access → `SupUserSessRev(…, 'AccountState')` → 401.
7. New token; `ScrUsrSessRotate` (guarded; `THROW 50001` if the row is no longer active – concurrent use is **treated as reuse**, step 3) [D-07].
8. Issue access token with the new `sid`; 200 `TokenResponse`. No `LoginHistory` event (spec event list has no refresh type).

### 3.4 Logout (`POST api/auth/logout`)
1. Decode/hash; invalid, unknown or already revoked → **204** (no oracle; no history).
2. Otherwise, in one unit of work: `SupUserSessRev(UserId, SessionId, 'Logout')` + `ScrLoginHist(UserId, Logout)` → 204.

### 3.5 Protected request
`JwtBearer` validates signature, issuer, audience, lifetime (skew 30 s) → `OnTokenValidated` → `ISessionValidationService.ValidateAsync(sid, sub)` → `SvaUsrSess` → status ≠ 0 → `context.Fail(...)` → 401 ProblemDetails. Permission policies then evaluate `SvaUsrPerm` (memoized per request) [D-11].

---

## 4. Data model (exactly per spec v0.2; no new objects)

### 4.1 Tables in the Step 0 migration [D-27]
Recommended minimal set (9 of 11): `AppUser`, `UserAccount`, `UserSession`, `LoginHistory` (login/session) and `Role`, `Permission`, `Menu`, `UserRole`, `RoleMenuAccess` (needed by `SvaUsrPerm`). Deferred: `UserTerritory`, `UserHierarchy` (Steps 11–12; depend on Module 02 Territory, DBG-003/DBG-004).
Migration name: `Module01AuthenticationFoundation`. Created by the database-engineer; **applied only by the user** (CLAUDE.md hard rule).

Columns, types, nullability and defaults are copied from the spec sheets without change (e.g. `UserSession.TokenHash VARBINARY(32) NOT NULL`, `AppUser.EncPassword VARBINARY(512)`, `PasswordNonce VARBINARY(12)`, `PasswordTag VARBINARY(16)`, `KeyVersion NVARCHAR(50)`, `LoginHistory.EventType TINYINT`, `IPAddress NVARCHAR(45)`, `DeviceInfo NVARCHAR(500)`). `CreatedDate` columns get **no** DEFAULT (spec silent; `Database_Conventions.md` §3.3 note 4) – procedures set `SYSUTCDATETIME()`.

### 4.2 Object names (from `Database_Conventions.md` §3.3; no others)

| Table | PK | Unique | Index | DEFAULT | Check |
|---|---|---|---|---|---|
| AppUser | PKAppUser | UQAppUser_Username | – | DFAppUser_IsActive (1), DFAppUser_FailedLogCount (0), DFAppUser_IsLocked (0), DFAppUser_PwdChangeReq (0) | – |
| UserAccount | PKUserAccount | UQUserAccount_UserId | – | DFUserAccount_CanAccessWeb (0), DFUserAccount_CanAccessMobile (0), DFUserAccount_IsActive (1) | – |
| UserSession | PKUserSession | UQUserSession_TokenHash | – | – | – |
| LoginHistory | PKLoginHistory | – | IXLoginHistory_UserIdEventDate | – | CKLoginHistory_EventType [D-16] |
| Role | PKRole | UQRole_Name | – | DFRole_IsSystemRole (0), DFRole_IsActive (1) | – |
| Permission | PKPermission | UQPermission_Name, UQPermission_Code | – | DFPermission_IsActive (1) | – |
| Menu | PKMenu | UQMenu_Code | IXMenu_ParentMenuIdDisplayOrder | DFMenu_DisplayOrder (0), DFMenu_IsActive (1) | CKMenu_Platform [D-16] |
| UserRole | PKUserRole | UQUserRole_UserIdRoleId | – | – | – |
| RoleMenuAccess | PKRoleMenuAccess | UQRoleMenuAccess_RoleIdMenuIdPermissionId | IXRoleMenuAccess_RoleIdMenuId | – | – |

No FKs, no navigations (AD-07). `dbo` only (AD-24). Names built with `DbObjectNames`.
**Observation (no change made):** the spec defines no index on `UserSession.UserId`; "revoke all sessions of a user" scans `UserSession`. Raised for the spec owner (C-08).

### 4.3 Domain entities (EF schema only)
`Domain/Module01/<Feature>/`: `AppUser`, `UserAccount`, `UserSession`, `LoginHistory`, `Role`, `Permission`, `Menu`, `UserRole`, `RoleMenuAccess`, all deriving from `AuditableEntity` (every listed table has the four audit columns). Plain POCOs, no navigations. Field lengths in one constants class `Module01FieldLengths` (Domain) shared by EF configurations and request validation (B-10). Configurations: `Infrastructure/Persistence/Configurations/Module01/<Table>Configuration.cs`.

---

## 5. Stored Procedure inventory

All scripts: `Database/StoredProcedures/Module01/dbo.<Name>.sql`, `CREATE OR ALTER`, header block, `SET NOCOUNT ON; SET XACT_ABORT ON;`, parameters only, `SYSUTCDATETIME()`, no FK/REFERENCES. C# constants in `Infrastructure/Repositories/Module01/Module01Procedures.cs` (hotspot, single writer). Writes return `@NewId` / `@RowsAffected` via the executor (AD-09). Binary parameters typed explicitly (`DbType.Binary`, exact size).

### 5.1 Specification procedures used in Step 0 (names preserved exactly; signatures proposed – D-02)

| Procedure | Op | Parameters | Result | Business errors |
|---|---|---|---|---|
| `dbo.SvaAppUserLogin` | Validate (read-only) [D-03] | `@Username NVARCHAR(100)` | 0/1 row: `UserId, Username, FirstName, LastName, EncPassword, PasswordNonce, PasswordTag, KeyVersion, IsActive, IsLocked, FailedLogCount, PwdChangeReq, UserAccountId (NULL), AccountIsActive (NULL), CanAccessWeb (NULL), CanAccessMobile (NULL)` (AppUser LEFT JOIN UserAccount on UserId) | none (unknown → 0 rows) |
| `dbo.SvaUsrPerm` | Validate (read-only) | `@UserId INT`, `@Platform TINYINT` [D-11] | n rows, DISTINCT: `MenuId, MenuCode, PermissionId, PermissionCode` – union over `UserRole` → `Role` (IsActive=1) → `RoleMenuAccess` → `Menu` (IsActive=1, Platform matches) → `Permission` (IsActive=1); empty when the user is inactive or locked | none |
| `dbo.ScrLoginHist` | Create (append-only) | `@UserId INT NULL, @Username NVARCHAR(100), @EventType TINYINT, @IPAddress NVARCHAR(45) NULL, @DeviceInfo NVARCHAR(500) NULL, @FailureReason NVARCHAR(250) NULL, @CreatedBy INT` | `@NewId` | 50003 (EventType ∉ {1,2,3}); 50004 (`@UserId` not NULL and no AppUser row) |
| `dbo.SupUserSessRev` | Update | `@UserId INT, @SessionId INT NULL` (NULL = all non-revoked sessions of the user), `@RevokedReason NVARCHAR(250), @ModifiedBy INT` | `@RowsAffected` (0 = nothing to revoke; not an error) | none |
| `dbo.SreAppUserById` | Read | `@Id INT` | 0/1 row: `Id, Username, FirstName, LastName, Email, MobileNo, IsActive, IsLocked, LastLoginDate, PwdChangeReq, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate` – **never** password columns (spec) | none |

`ScrLoginHist` sets `EventDate = CreatedDate = SYSUTCDATETIME()`; `ModifiedBy/ModifiedDate` stay NULL; no update/delete procedure exists for `LoginHistory` (spec *Audit history*). `SupUserSessRev` sets `RevokedDate`, `RevokedReason`, `ModifiedBy`, `ModifiedDate` only where `RevokedDate IS NULL`.

### 5.2 New procedures (spec gap – DBG-012). Each is **Decision needed**; names follow AD-08 (prefix + descriptive PascalCase, `Usr` not `User`, approved abbreviations only)

| # | Proposed name | Op | Parameters | Result | Errors | Decision |
|---|---|---|---|---|---|---|
| N1 | `dbo.ScrUsrSess` | Create session at login | `@UserId INT, @TokenHash VARBINARY(32), @AbsoluteLifetimeMinutes INT, @IPAddress NVARCHAR(45) NULL, @DeviceInfo NVARCHAR(500) NULL, @CreatedBy INT`, `@ExpiresDate DATETIME2 OUTPUT` | `@NewId` (= `sid`) | 50002 (user missing, inactive or locked); duplicate hash → 2627 → 409 (negligible) | D-04 |
| N2 | `dbo.SupAppUsrLoginSuccess` | Reset failure count, set `LastLoginDate` | `@UserId INT` | `@RowsAffected` (0 = user no longer active/unlocked → caller rolls back) | none | D-05 |
| N3 | `dbo.SupAppUsrLoginFailure` | Atomic `FailedLogCount + 1`; lock at threshold (`IsLocked = 1`, `LockedDate = now`) | `@UserId INT, @MaxFailedAttempts INT`, `@IsLocked BIT OUTPUT` | `@RowsAffected` | none | D-05, D-06 |
| N4 | `dbo.SreUsrSessByTokenHash` | Read session by token hash | `@TokenHash VARBINARY(32)` | 0/1 row: `SessionId, UserId, CreatedDate, LastActDate, ExpiresDate, RevokedDate, RevokedReason, Username, FirstName, LastName, IsActive, IsLocked, PwdChangeReq, AccountIsActive, CanAccessWeb, CanAccessMobile`, plus computed `IsExpired BIT`, `IsIdle BIT` (from `@IdleTimeoutMinutes INT` and the DB clock) | none | D-07 |
| N5 | `dbo.ScrUsrSessRotate` | Rotate: revoke current row (`'Rotated'`) and insert successor inheriting `UserId`, `ExpiresDate`, `LastActDate` – one transaction, `UPDLOCK` | `@CurrentSessionId INT, @NewTokenHash VARBINARY(32), @IdleTimeoutMinutes INT, @Platform TINYINT, @IPAddress NVARCHAR(45) NULL, @DeviceInfo NVARCHAR(500) NULL, @CreatedBy INT` | `@NewId` | 50001 (current row revoked, expired, idle, or user/account/platform no longer valid) | D-07 |
| N6 | `dbo.SvaUsrSess` | Per-request validation; revokes on idle/absolute expiry; throttled touch of `LastActDate` | `@SessionId INT, @UserId INT, @IdleTimeoutMinutes INT, @TouchIntervalSeconds INT, @Platform TINYINT, @ModifiedBy INT` | single row `SessionStatus TINYINT`: 0 Valid, 1 NotFound/UserMismatch, 2 Revoked, 3 Expired, 4 Idle, 5 UserInactive, 6 UserLocked, 7 PlatformDenied | none (status row, not THROW, so the expiry revocation is committed) | D-08 |
| N7 | `dbo.SupAppUsrPwd` | Set encrypted payload (`EncPassword`, `PasswordNonce`, `PasswordTag`, `KeyVersion`, `PwdChangeReq`) – **Step 0: bootstrap seed only, no endpoint** | `@UserId INT, @EncPassword VARBINARY(512), @PasswordNonce VARBINARY(12), @PasswordTag VARBINARY(16), @KeyVersion NVARCHAR(50), @PwdChangeReq BIT, @ModifiedBy INT` | `@RowsAffected` | 50004 | D-20 (P-06 gap) |
| N8 | `dbo.SreAppUsrSystemId` | Resolve the SYSTEM user Id by its configured username (AD-14) | `@Username NVARCHAR(100)` | scalar `Id` or NULL | none | D-19 |

Notes: N2/N3 do **not** touch `ModifiedBy/ModifiedDate` (login bookkeeping is recorded in `LoginHistory`; avoids marking profiles as edited on every login) [D-05]. N3 is a single `UPDATE … WHERE Id = @UserId AND IsActive = 1 AND IsLocked = 0`, so concurrent failures are counted exactly. N6 uses the `Sva` prefix although it writes `LastActDate` / revocation (validate-and-touch in one round trip) [D-08].

### 5.3 Spec procedures needed only by the bootstrap seed [D-20]
`dbo.ScrAppUser` (`@Username, @FirstName, @LastName NULL, @Email NULL, @MobileNo NULL, @EncPassword, @PasswordNonce, @PasswordTag, @KeyVersion, @PwdChangeReq, @CreatedBy` → `@NewId`; THROW on duplicate username) and `dbo.ScrUserAccount` (`@UserId, @ProcessId, @CanAccessWeb, @CanAccessMobile, @CreatedBy` → `@NewId`; one account per user). Their full rules (validation of `ProcessId` against the Process master) belong to Steps 4/6; Step 0 must not pre-empt them beyond the seed. **ProcessId validation is impossible while the Process master is undefined (DBG-002).**

### 5.4 Business error catalogue entries (Module 01 range 50001–50099; block 50001–50019 reserved for Step 0) [D-28]

| No. | Code | Type | Safe message | Raised by |
|---|---|---|---|---|
| 50001 | `Auth.SessionInvalid` | Unauthorized | Your session is no longer valid. Please sign in again. | N5 |
| 50002 | `Auth.UserNotActive` | Unauthorized | Invalid username or password. | N1 |
| 50003 | `LoginHistory.EventTypeInvalid` | Validation | The login event type is not valid. | ScrLoginHist |
| 50004 | `User.NotFound` | NotFound | The user was not found. | ScrLoginHist, N7 |

Registered in `StoredProcedureErrors.cs` (hotspot) under `StoredProcedureErrorRange.Module01`. Service-level errors (no SQL): `Auth.InvalidCredentials` (401, "Invalid username or password."), `Auth.PlatformAccessDenied` (403, "Your account does not have access to this application.") [D-13], `Auth.InvalidRefreshToken` (401, "Your session is no longer valid. Please sign in again.") in `Application/Module01/Authentication/AuthErrors.cs`.

---

## 6. Application layer (`FarmFMS.Application/Module01/…`) – no crypto, JWT or SQL (AD-02)

### 6.1 Interfaces implemented in Infrastructure
| Interface | Purpose |
|---|---|
| `IPasswordProtector` | `PasswordVerificationResult Verify(string password, EncryptedPassword payload, int userId)`; `EncryptedPassword Protect(string password, int userId)` (seed only in Step 0). Result: `Match`, `Mismatch`, `IntegrityFailure`, `UnknownKeyVersion`. |
| `IAccessTokenIssuer` | `AccessToken Issue(int userId, int sessionId, string username)` → token + `ExpiresUtc`. |
| `ISessionTokenFactory` | `SessionToken Create()` (raw base64url + 32-byte hash); `bool TryHash(string raw, out byte[] hash)` (rejects anything not decoding to 32 bytes). |
| `ILoginRepository` | `SvaAppUserLogin`, N2, N3 |
| `IUserSessionRepository` | N1, N4, N5, N6, `SupUserSessRev` |
| `ILoginHistoryRepository` | `ScrLoginHist` |
| `IUserPermissionRepository` | `SvaUsrPerm` |
| `IAppUserReadRepository` | `SreAppUserById` |

Repositories return read models (AD-10): `LoginCredentialReadModel`, `SessionReadModel`, `SessionValidationStatus` (enum mirroring N6), `EffectivePermissionReadModel (MenuCode, PermissionCode)`, `AppUserProfileReadModel`. One method per procedure; `CancellationToken` passed through (B-06).

### 6.2 Services
| Service | Responsibility |
|---|---|
| `IAuthenticationService` / `AuthenticationService` | Login, refresh, logout per §3.2–3.4; returns `Result<TokenResponse>` / `Result`; uses `IUnitOfWork` for multi-procedure writes; `TimeProvider`; `ILogger<T>`. |
| `ISessionValidationService` | Per-request `SvaUsrSess`; maps status → `Result`. |
| `IPermissionService` | Scoped, memoized `SvaUsrPerm` per request; `HasPermission(menuCode, permissionCode)`. |
| `ICurrentUserProfileService` | `/me`: `SreAppUserById` + `IPermissionService`. |

Business constants from the spec live in `Application/Module01/Authentication/SessionPolicy.cs`: `IdleTimeout = 30 min`, `MaxFailedLoginAttempts = 5` (spec *Confirmed Decisions*; not configurable) [D-06]. Technical values (token lifetimes, touch interval) come from options (§9).

### 6.3 Audit actor (AD-14) [D-18]
Login-path writes happen before authentication, so `IAuditActorService` cannot supply the actor from the request. Proposed explicit exception, documented in code:
- Success (N1, `ScrLoginHist` Success), refresh (N5), logout (`SupUserSessRev`, `ScrLoginHist` Logout): actor = the **verified user's own Id** (password or refresh token proven).
- Failed login (`ScrLoginHist` Failed, unknown or known user), reuse/expiry revocations, N6 revocations: actor = **System user**, inside `IAuditActorService.BeginSystemOperation("Login")` / `("SessionExpiry")` (AD-14 "explicitly authorized as a system use case").
- N2/N3 take no actor (§5.2 note).

### 6.4 DTOs (AD-10; `Application/Module01/Authentication/`)

| DTO | Fields and validation |
|---|---|
| `LoginRequest` | `Username` `[Required] [StringLength(100)]` (spec `NVARCHAR(100)`); `Password` `[Required] [StringLength(128)]` – input bound only; follows the P-04 recommendation and must follow P-04 when decided. No complexity validation on login (policy applies when a password is set). |
| `RefreshRequest`, `LogoutRequest` | `RefreshToken` `[Required] [StringLength(43, MinimumLength = 43)]` |
| `TokenResponse` | `AccessToken`, `AccessTokenExpiresUtc` (DateTime UTC), `RefreshToken`, `SessionExpiresUtc` (= `ExpiresDate`), `IdleTimeoutMinutes` (30), `TokenType` ("Bearer"), `PasswordChangeRequired` (bool, from `PwdChangeReq`) [D-15], `User` (`AuthenticatedUserDto`: `Id`, `Username`, `FirstName`, `LastName`) |
| `CurrentUserResponse` | `Id`, `Username`, `FirstName`, `LastName`, `Email`, `PasswordChangeRequired`, `Permissions: [{ MenuCode, PermissionCode }]` |
| `ClientContext` (internal) | `IPAddress` (≤ 45), `DeviceInfo` (User-Agent, truncated to 500) [D-26] |

The contract property names above are the single contract for the future frontend (AD-01 drift note: `AccessTokenExpiresUtc`).

---

## 7. Infrastructure

### 7.1 Password protector (AD-11; design §2, §4.3, §6.2; approved P-02, P-03, P-07, P-08 dev, P-23)
- `AesGcmPasswordProtector` (`Infrastructure/Security/`) using `System.Security.Cryptography.AesGcm`, 32-byte key, 12-byte nonce, 16-byte tag.
- Plaintext = UTF-8 of `password.Normalize(NormalizationForm.FormC)` (P-02).
- AAD (P-03 Option A) byte layout [D-22]: `UTF8("FarmFMS.Password.v1") ‖ 0x00 ‖ UTF8(KeyVersion) ‖ 0x00 ‖ BigEndianInt32(AppUser.Id)`.
- Before decrypt: nonce = 12, tag = 16, ciphertext 1–512 bytes, KeyVersion known in the ring; else `IntegrityFailure` / `UnknownKeyVersion`. Never falls back to another key.
- Compare with `CryptographicOperations.FixedTimeEquals`; zero plaintext buffers (`CryptographicOperations.ZeroMemory`) after use. Exceptions never contain payload bytes.
- `PasswordKeyRing` (singleton): loaded from `PasswordEncryptionOptions`; keys held as `byte[]`, zeroed on dispose; startup validation per design §4.3 (active version present, every key exactly 32 bytes, valid base64, version label format [D-22]).

### 7.2 Token services
- `JwtAccessTokenIssuer` using `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler` (Infrastructure package), HS256, key from `JwtOptions.SigningKey` (≥ 32 bytes decoded) [D-23].
- `SessionTokenFactory`: `RandomNumberGenerator.GetBytes(32)`, `Base64Url`, `SHA256.HashData` over the 32 raw bytes [D-24].

### 7.3 Repositories
`Infrastructure/Repositories/Module01/{Login,UserSession,LoginHistory,UserPermission,AppUserRead}Repository.cs`, inject `IStoredProcedureExecutor` only; registered in `AddInfrastructure()` (hotspot).

### 7.4 Bootstrap seed (Development only) [D-19, D-20]
An explicitly invoked command, never at startup (B-03: "No seeding from a configured password"; RouteNexa `AuthSeeder` not carried over). The initial password is entered interactively, never read from configuration or logged. One unit of work: create SYSTEM user → create admin (`ScrAppUser` with a random placeholder payload) → `Protect(password, newId)` → `SupAppUsrPwd` → `ScrUserAccount` (`CanAccessWeb = 1`). **Running it is a database change and requires explicit user approval for that command.**

---

## 8. API

### 8.1 Endpoints (`Controllers/Module01/AuthController.cs`, route `api/auth` [D-29]; inherits `ApiControllerBase`; no try/catch; `CancellationToken` on every action)

| Verb / route | Auth | Rate limit | Request | Success | Errors |
|---|---|---|---|---|---|
| `POST api/auth/login` | `[AllowAnonymous]` | `login` | `LoginRequest` | 200 `TokenResponse` | 400 ValidationProblemDetails; 401 `Auth.InvalidCredentials`; 403 `Auth.PlatformAccessDenied` [D-13]; 429; 500/503 generic |
| `POST api/auth/refresh` | `[AllowAnonymous]` | `refresh` | `RefreshRequest` | 200 `TokenResponse` | 400; 401 `Auth.InvalidRefreshToken`; 429; 500/503 |
| `POST api/auth/logout` | `[AllowAnonymous]` (AD-13) | `refresh` | `LogoutRequest` | 204 (always for well-formed input) | 400; 429 |
| `GET api/auth/me` | default policy (authenticated + valid session) | – | – | 200 `CurrentUserResponse` | 401 |

All token responses: `Cache-Control: no-store`, `Pragma: no-cache`. No admin endpoint exists in Step 0.

### 8.2 Authentication and authorization setup (`API/Common/Security/`; Program.cs and `AddApi()` are hotspots)
- `AddAuthentication(JwtBearer)` (package `Microsoft.AspNetCore.Authentication.JwtBearer`): `MapInboundClaims = false`, `NameClaimType = "name"`, validate issuer/audience/lifetime/signature, `ClockSkew = 30 s`, `OnTokenValidated` → `ISessionValidationService`.
- `MapControllers().RequireAuthorization()` (AD-13). `[AllowAnonymous]` allowlist: login, refresh, logout, `/health`, `/health/ready`; Development-only `/openapi/v1.json` and `/swagger` are outside the controller convention.
- Permission policies: `PermissionRequirement(menuCode, permissionCode)` + `PermissionAuthorizationHandler` (uses `IPermissionService`). Named policies registered with `AddAuthorizationBuilder().AddPolicy(...)` from a `Policies` constants class **as each later step defines them**; naming `Perm.{MenuCode}.{PermissionCode}` [D-11]. Step 0 registers none (Menu/Permission codes are not defined, DBG-014).
- Pipeline order: `UseExceptionHandler` → `UseStatusCodePages` → (Dev OpenAPI/Swagger) → `UseHttpsRedirection` → `UseHsts` (non-Development) → `UseForwardedHeaders` [D-26] → `UseHttpLogging` → `UseRateLimiter` → `UseAuthentication` → `UseAuthorization` → endpoints.
- `CurrentUserService` already reads `sub`; add `SessionId` (`sid`) to `ICurrentUserService` (no other change).

### 8.3 Rate limiting [D-26]
Built-in `Microsoft.AspNetCore.RateLimiting`: `login` fixed window 10 requests / 1 min per client IP; `refresh` 30 / 1 min per client IP; rejection 429 ProblemDetails with `Retry-After`. Lockout (spec) remains the per-account control.

### 8.4 Client IP and device [D-26]
The web frontend is Blazor **Interactive Server**: the browser never calls the API; the Blazor server does. Without forwarding, every login shows the Blazor server's IP/User-Agent and one per-IP limit is shared by all users. Proposed: `ForwardedHeaders` (`X-Forwarded-For`) trusted **only** from configured `KnownProxies` (the Blazor server); the frontend forwards the browser IP and User-Agent when it is built.

### 8.5 OpenAPI
Development only (AD-18). Add a Bearer security scheme via a document transformer so Swagger UI can call `/api/auth/me`.

### 8.6 Logging (AD-16; design §9)
| Event | Level | Fields allowed |
|---|---|---|
| LoginSucceeded | Information | UserId, SessionId |
| LoginFailed | Warning | Reason code, UserId or masked username (`StringExtensions.Mask`) |
| AccountLocked | Warning | UserId |
| CredentialIntegrityFailure (P-23 alert hook) | Error | UserId, KeyVersion, outcome |
| RefreshTokenReuse | Warning | UserId, SessionId, revoked count |
| SessionRejected | Information (idle/expired) / Warning (others) | UserId, SessionId, status |
| AuthorizationDenied | Warning | UserId, policy name |

Never logged: passwords, tokens (access/refresh), token hashes, payload bytes, keys, request/response bodies (HTTP logging already excludes bodies and headers). Alert routing (P-22) does not exist yet; the Error-level event is the hook.

### 8.7 Frontend impact
None in Step 0 (frontend foundation not approved). Constraint for AD-21: the frontend must not refresh tokens on a timer while the user is idle; refresh only when an API call needs a token.

---

## 9. Configuration and secrets (AD-17, B-15)

| Section | Keys | Where | Validation (on start) |
|---|---|---|---|
| `Jwt` | `Issuer`, `Audience`, `AccessTokenLifetimeMinutes` (10), `ClockSkewSeconds` (30) | `appsettings.json` | required; lifetime 1–30 (must not exceed the idle timeout); skew 0–60 |
| `Jwt:SigningKey` | base64, ≥ 32 bytes | **user-secrets** (Dev) | decodes; ≥ 32 bytes |
| `Session` | `AbsoluteLifetimeHours` (12 [D-09]), `ActivityTouchIntervalSeconds` (60 [D-08]) | `appsettings.json` | 1–24; 0–300 |
| `PasswordEncryption` | `ActiveKeyVersion`, `Keys:{version}` (base64 32 bytes) | **user-secrets** (Dev, P-08) | per §7.1 |
| `RateLimiting` | `Login:PermitLimit/WindowSeconds`, `Refresh:…` | `appsettings.json` | > 0 |
| `ForwardedHeaders` | `KnownProxies[]` | `appsettings.{Env}.json` | valid IPs |
| `SystemUser` | `UserId` (existing bridge) or `Username` = `SYSTEM` [D-19] | user-secrets / env | existing |

Committed `appsettings*.json` contain no key values (existing `ConfigurationSecretsTests`). Unit tests generate random throwaway keys at runtime via environment variables (as for the connection string). **Non-Development environments:** key custody and secret store are not approved (P-08, P-13) → Step 0 is Development-only [D-31]. Single SQL connection `ConnectionStrings:FarmFMS`; restricted principal pending P-14 [D-25].

---

## 10. Tests

### 10.1 Unit tests (`FarmFMS.UnitTests`)
- **Protector:** round trip; distinct nonces for equal passwords; AAD binding (other `userId` → `IntegrityFailure`); tampered tag/ciphertext → `IntegrityFailure`; wrong lengths → `IntegrityFailure`; unknown KeyVersion → `UnknownKeyVersion` (no fallback); NFC: composed and decomposed forms verify equally; key ring validation (missing active, wrong length, bad base64, bad label) fails startup.
- **Tokens:** issued JWT validates with the API parameters; claims `sub`, `sid`, `name`; expired (beyond skew), wrong issuer/audience/signature rejected; refresh token is 43 chars base64url, hash = SHA-256 of raw bytes, malformed input rejected; `JwtOptions` validation (short key fails startup).
- **AuthenticationService** (fake repositories, fake `TimeProvider`, fake protector): unknown, inactive, locked (no count, no decrypt), mismatch (count + history), 5th mismatch locks + `AccountLocked` log, integrity failure not counted + Error log (P-23), platform denied (403, not counted), success (UoW commit: N2, N1, history Success; correct actor per §6.3), N2 returns 0 → rollback → 401; username normalization [D-21]; refresh: valid rotation, unknown, reuse → revoke-all, concurrent rotation (50001) → revoke-all, expired, idle, account state; logout: revokes + history Logout; unknown/revoked token → 204 without writes.
- **SessionValidationService / PermissionService / PermissionAuthorizationHandler:** status mapping; per-request memoization; allow/deny.
- **API host** (`WebApplicationFactory`, fakes): `/api/auth/me` without token → 401 ProblemDetails; with token whose session status ≠ 0 → 401; a test-only controller without `[AllowAnonymous]` → 401 (secure by default); login/refresh/logout reachable anonymously; 429 after the limit; `Cache-Control: no-store` on token responses; 401 bodies contain only the generic message; **log capture contains no password, refresh token, access token or hash** in any auth flow.
- **Catalogue:** new 5.4 entries pass the existing range/uniqueness checks.

### 10.2 Architecture tests (`FarmFMS.ArchitectureTests`)
- Application references no `Microsoft.IdentityModel.*`, `System.IdentityModel.Tokens.Jwt`, JwtBearer or crypto implementation (extend `LayerDependencyTests`).
- `[AllowAnonymous]` appears only on the allowlisted actions (reflection over controllers).
- `Module01Procedures` constants valid and dbo-qualified; new names contain no `User`; spec names unchanged (existing tests).
- `Database/StoredProcedures/Module01/*.sql`: no FK/REFERENCES, valid names (existing).
- EF model: the 9 tables, columns and the §4.2 names generated exactly (existing `Module01ObjectNameTests` + model rules); no FKs/navigations; no `migrationBuilder.Sql`.

### 10.3 Integration tests
**Blocked (AD-23, G7):** no SQL Server test database. Pending cases: each procedure in §5 (incl. concurrent `SupAppUsrLoginFailure`, `ScrUsrSessRotate` race, `SvaUsrSess` idle revocation), real `SqlException` → catalogue mapping. Live behaviour is **not verified** until then.

### 10.4 Manual test prerequisites (user, Development)
Each needs explicit user approval: apply the migration; run the procedure scripts; set user-secrets (`Jwt:SigningKey`, `PasswordEncryption:*`); run the bootstrap seed. Then via Swagger UI: login, `/me`, refresh, reuse of the old refresh token (all sessions revoked), logout, 5 wrong passwords (locked; correct password still fails), idle > 30 min (401).

---

## 11. Acceptance criteria

| AC | Criterion |
|---|---|
| AC-01 | Valid credentials for an active, unlocked user with an active `UserAccount` and `CanAccessWeb = 1` → 200 with access token, refresh token, expiries; a `UserSession` row with only the SHA-256 hash; `LoginHistory` EventType 1; `FailedLogCount = 0`, `LastLoginDate` set. Multiple concurrent sessions allowed. |
| AC-02 | Unknown username, inactive user, wrong password and locked account all return the same 401 body; unknown/inactive/locked never decrypt. |
| AC-03 | Inactive user cannot log in, refresh, or use an existing access token (next request → 401). |
| AC-04 | Correct password without web access (no account, inactive account, `CanAccessWeb = 0`) → 403 [D-13]; not counted; history Failed. |
| AC-05 | The 5th consecutive wrong password sets `IsLocked = 1`, `LockedDate`; further attempts (even correct) fail and are not counted. |
| AC-06 | Tag-mismatch/unknown-key failures are not counted, are logged at Error, history Failed `CredentialError` (P-23). |
| AC-07 | Refresh rotates: old row revoked `Rotated`, new row with inherited `ExpiresDate`/`LastActDate`; new tokens returned. |
| AC-08 | `LoginHistory` rows for every success, failure and logout with the §12 D-17 reason codes; no update/delete path exists. |
| AC-09 | Presenting a rotated refresh token revokes **all** the user's sessions and returns 401; Warning logged. |
| AC-10 | No authenticated request for 30 min → next API request and next refresh return 401 and the session is revoked `IdleTimeout`. Background refreshes do not extend the idle window. |
| AC-11 | Revoked, expired or platform-denied sessions are rejected on every protected request. |
| AC-12 | Every controller endpoint requires authentication unless on the allowlist (test-proven). |
| AC-13 | `/api/auth/me` returns the profile (no password fields) and the effective permissions from `SvaUsrPerm`. |
| AC-14 | A permission policy allows/denies based on `SvaUsrPerm` (unit/host tests). |
| AC-15 | API fails to start when the JWT key or password key ring is missing or invalid. |
| AC-16 | No password, token, hash, key or payload appears in logs or ProblemDetails (test-proven). |
| AC-17 | Login/refresh return 429 when the rate limit is exceeded. |
| AC-18 | Release build, unit and architecture tests pass (G-gates); integration tests reported as Blocked. |

---

## 12. Decisions needed (approve in one pass; P1 blocks G0)

| ID | P | Decision | Source / reason | Recommendation |
|---|---|---|---|---|
| D-01 | P1 | Approve AD-12 (tokens) and AD-13 (authorization) – both **Proposed** – as the basis of Step 0, plus the `Jwt`/`PasswordEncryption`/`Session` section names (AD-17 "to be defined") | AD index; stop rule (security decision pending) | Approve as specified here |
| D-02 | P1 | Procedure parameters/result sets for spec procedures (§5.1, §5.3) – the spec lists names only | Spec *Stored Procedures* sheet | Approve the signatures in §5.1 |
| D-03 | P1 | `SvaAppUserLogin` is **read-only** credential retrieval; outcomes are written by N1–N3 + `ScrLoginHist` | Spec: "Restricted credential retrieval; API handles AES decrypt" – decryption is outside SQL, so one procedure cannot complete the workflow | Approve |
| D-04 | P1 | Add **N1 `ScrUsrSess`** (no spec procedure creates a `UserSession` row) | DBG-012 | Approve; spec owner adds it to the workbook |
| D-05 | P1 | Add **N2 `SupAppUsrLoginSuccess`** and **N3 `SupAppUsrLoginFailure`**; neither updates `ModifiedBy/ModifiedDate` | AD-12 "lockout and failure counts handled in stored procedures"; no spec procedure | Approve both |
| D-06 | P1 | Lockout threshold: spec value 5 as Application constant passed to N3 (`@MaxFailedAttempts`), not configurable; "consecutive" (spec `FailedLogCount` description); success resets; attempts on a locked account are not counted | Spec *Lockout*, AppUser sheet | Approve |
| D-07 | P1 | Rotation model: **new `UserSession` row per refresh**, old row `RevokedReason = 'Rotated'`; successor inherits `ExpiresDate` and `LastActDate`; `CreatedDate` = rotation time; concurrent use of one token treated as reuse (revoke all). Adds **N4 `SreUsrSessByTokenHash`** and **N5 `ScrUsrSessRotate`**. `RevokedReason` codes: `Logout`, `Rotated`, `RefreshTokenReuse`, `IdleTimeout`, `Expired`, `AccountState`. Consequence: ~1 row per refresh; no retention/cleanup procedure exists (future spec item) | `UserSession` has no "replaced by"/family column; reuse detection (AD-12) is impossible with in-place hash replacement | Approve (alternative: in-place rotation **without** reuse detection – contradicts AD-12) |
| D-08 | P1 | Per-request session validation with **N6 `SvaUsrSess`** (status row 0–7; revokes idle/expired; touches `LastActDate` at most every 60 s → idle precision ±60 s); `Sva` prefix although it writes | Spec *Read Me*: validate platform access on every protected request; AD-21: enforce idle timeout server-side | Approve (alternative: check only at refresh – idle window becomes imprecise and revocation is delayed up to the access-token lifetime) |
| D-09 | P1 | Lifetimes: access token **10 min**; idle **30 min** (spec); absolute session **12 h** | No source defines access or absolute lifetimes (AD-12 "per approved security requirements") | Approve 10 min / 12 h, or give values |
| D-10 | P2 | `UserSession.ExpiresDate` = absolute end (idle derived from `LastActDate`) | Spec: "Expiry" (undefined) | Approve |
| D-11 | P1 | Permissions resolved **per request** via `SvaUsrPerm` (not JWT claims); result = (MenuCode, PermissionCode); policy names `Perm.{MenuCode}.{PermissionCode}`; `SvaUsrPerm` filters `Menu.Platform` by `@Platform` (1 Web, 3 Both). Existence of a `RoleMenuAccess` row = Allow (see C-04) | Spec "Union of Allow permissions" but no Allow/Deny column; AD-13 named policies | Approve (alternative: claims in token – smaller DB load, stale up to 10 min, large tokens) |
| D-12 | P2 | Locked account at login returns the **generic** 401 (no "account locked" message) | AD-12 lists generic for unknown/inactive/wrong password; locked not listed; Step 1 tests "lockout behaviour" | Generic (prevents username enumeration); Admin sees lock state later |
| D-13 | P1 | Platform: Step 0 treats every login as **Web** (no platform field; `CanAccessWeb` checked; mobile login decided with the mobile app). Correct password without web access → **403** `Auth.PlatformAccessDenied`, not counted | Spec *Platform*; client-declared platform is untrusted | Approve |
| D-14 | P2 | User with no active role may log in (empty permission set) | Spec silent | Approve (no invented rule) |
| D-15 | P2 | `PwdChangeReq = 1`: Step 0 only returns `PasswordChangeRequired = true`; no enforcement until a password-change procedure exists (P-06) | Spec column; P-06 pending | Approve |
| D-16 | P2 | Confirm `LoginHistory.EventType` 1=Success, 2=Failed, 3=Logout and `Menu.Platform` 1=Web, 2=Mobile, 3=Both (spec "(proposed)") and create `CKLoginHistory_EventType` / `CKMenu_Platform` | DBG-007; Conventions §3.3 note 1 | Confirm; create both check constraints |
| D-17 | P2 | `LoginHistory.FailureReason` codes: `UnknownUser`, `Inactive`, `Locked`, `InvalidPassword`, `CredentialError`, `PlatformDenied` | Spec column only | Approve |
| D-18 | P1 | Audit actor for login-path writes (§6.3): self-Id for success/refresh/logout; System user for failures and expiry revocations | AD-14 (no request user before login); Spec *LoginHistory.CreatedBy*: "define convention" | Approve |
| D-19 | P1 | SYSTEM user: an `AppUser` `Username = 'SYSTEM'`, `IsActive = 0` (can never log in), random unknown payload, no `UserAccount`, no roles; resolved through **N8 `SreAppUsrSystemId`** (replaces the `SystemUser:UserId` bridge) | AD-14 Blocked; spec *Bootstrap* | Approve N8 + record (alternative: keep the configured-Id bridge in Step 0) |
| D-20 | P1 (for manual testing) | Bootstrap seed: Development-only explicit command (§7.4); needs **N7 `SupAppUsrPwd`** (P-06 gap, forced by P-03 Option A: the Id must exist before encryption), `ScrAppUser`, `ScrUserAccount`; SYSTEM row `CreatedBy = 0` (sentinel, documented), admin `CreatedBy = SYSTEM.Id`; **ProcessId for the admin's `UserAccount` must be supplied by the owner** (Process master undefined, DBG-002); no roles seeded (no Role/Permission/Menu procedures in spec, DBG-014) | Spec *Bootstrap*; DBG-002, DBG-014; P-06 | Approve command + N7; owner provides a ProcessId value or decides to defer the account requirement |
| D-21 | P1 | Username policy: trim; NFC; case-**insensitive** match relying on a CI database collation (deployment check), stored as entered; no lower-casing | Spec *Indexes & Rules*: "enforce normalized/case policy consistently" (policy not given) | Approve |
| D-22 | P2 | AAD byte layout (§7.1); `KeyVersion` label format `^[A-Za-z0-9-]{1,50}$`, e.g. `dev-20261105`; key ring config shape | P-03/P-07 approved in principle; exact form not defined | Approve |
| D-23 | P2 | JWT: HS256, ≥ 256-bit key, issuer `FarmFMS.API`, audience `FarmFMS`, skew 30 s, claims per §3.1 | AD-12 | Approve |
| D-24 | P2 | Refresh token: 32 random bytes, base64url; hash = SHA-256 of raw bytes | AD-12; spec VARBINARY(32) | Approve |
| D-25 | P2 | Step 0 uses the single connection; restricted EXECUTE-only principal deferred (P-14) | Design §5.2 | Defer P-14 to before any non-Development deployment |
| D-26 | P1 | Rate limits (login 10/min/IP, refresh 30/min/IP) and client-IP source: trust `X-Forwarded-For` only from configured Blazor server proxies | Blazor Server calls the API server-to-server (§8.4) | Approve |
| D-27 | P2 | Step 0 migration tables: 9 (§4.1) vs all 11 | Minimal scope | 9 tables |
| D-28 | P2 | Error numbers 50001–50004 and reserved block 50001–50019 (§5.4) | AD-09 | Approve |
| D-29 | P2 | Route `api/auth/...` (module segment `auth`) and folders `Module01/Authentication` (deviates from B-14 `Auth/*` folders) | AD-18, AD-03 | Approve |
| D-30 | P3 | Idle/expiry/reuse revocations produce **no** `LoginHistory` event (only `RevokedReason`) | Spec event list: success/failed/logout only | Approve |
| D-31 | P1 | Step 0 is Development-only; any other environment waits for P-08 and P-13 | Design §11.1 (P-08 Development only) | Approve |

---

## 13. Conflicts and open items (not resolved here)

| ID | Conflict | Where | Owner |
|---|---|---|---|
| C-01 | Task brief asked for "≤ 15 chars" procedure names; AD-08 (Approved) removed that limit (spec *Read Me* still says 15 – AD remaining item 1, DBG-005). This spec follows AD-08 (descriptive names). | AD-08; spec *Read Me* | User / spec owner |
| C-02 | `Business_Clarification_Register.md` CQ-036 still says "implementation blocked", and `Functional_Requirements_Analysis.md` §"Security" says "Do not implement password reversibility", while design §11.1 records the user's login-path approval. | CQ-036; FRA | Business analyst to update the register |
| C-03 | FR-UM numbering differs between SRS (FR-UM-001…010) and FRD (FR-UM-001…022); e.g. FR-UM-008 means different things. | SRS §3.1; FRD | Business analyst |
| C-04 | Spec says "Union of **Allow** permissions" but `RoleMenuAccess` has no Allow/Deny column. Assumed: row present = Allow (D-11). | Spec *Stored Procedures* (SvaUsrPerm) | Spec owner |
| C-05 | `Database_Conventions.md` §6 example uses `THROW 50101` (Module 02 range) for a username error (illustrative only). | Conventions §6 | Doc owner |
| C-06 | Spec *Read Me* requires territory-scope validation on every protected request; not possible in Step 0 (no territory data); deferred to Step 11. | Spec *Read Me* | User |
| C-07 | SRS NFR-012 / FR-UM-010 (session policy "Pending Definition") vs spec *Confirmed Decisions* (30 min). Spec followed. | SRS | Business analyst |
| C-08 | No index on `UserSession.UserId` in the spec (revoke-all scans). No object added. | Spec *Indexes & Rules* | Spec owner |
| C-09 | Process master undefined but `UserAccount.ProcessId` NOT NULL (blocks the bootstrap admin's platform access). | DBG-002 | Business / spec owner |

---

## 14. Traceability

| Requirement | Procedures | Endpoint | Tests / AC |
|---|---|---|---|
| FR-UM-001 | SvaAppUserLogin, N1, N2, ScrLoginHist | POST api/auth/login | AuthenticationService login tests; AC-01, AC-02 |
| FR-UM-018 / spec Lockout | N3, ScrLoginHist | POST api/auth/login | lockout tests; AC-05, AC-06 |
| FR-UM-019 / spec Session timeout / AD-21 | N6, N4, N5, SupUserSessRev | all protected; POST api/auth/refresh | session validation + refresh tests; AC-10 |
| AD-12 rotation / reuse | N4, N5, SupUserSessRev | POST api/auth/refresh | AC-07, AC-09 |
| FR-UM-016 / FR-AU-002 / NFR-011 | ScrLoginHist | login, logout | AC-08 |
| FR-UM-020 (blocking) | SvaAppUserLogin, N6 | login, protected | AC-03, AC-11 |
| FR-UM-022 / spec Platform | SvaAppUserLogin, N5, N6 | login, refresh, protected | AC-04, AC-11 |
| FR-UM-002/004/006, NFR-008, AD-13 | SvaUsrPerm | GET api/auth/me; policy mechanism | AC-12–AC-14 |
| NFR-009/012, AD-11, design §11.1 | – (Infrastructure) | login | protector tests; AC-15, AC-16 |
| AD-16, design §9 | – | all auth | log-capture tests; AC-16 |
| B-03 rate limiting | – | login, refresh, logout | AC-17 |
| Logout (spec Login history) | N4, SupUserSessRev, ScrLoginHist | POST api/auth/logout | logout tests; AC-08 |
