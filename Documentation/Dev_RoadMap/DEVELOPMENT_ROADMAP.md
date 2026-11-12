# Farm Monitoring System (FMS) – Development Roadmap

## 1. Purpose

This document defines the implementation roadmap for the Farm Monitoring System. Development will be carried out module-by-module and, within each module, page-by-page.

The workflow for every feature is:

1. Implement only the current feature/page and its required backend work.
2. Complete the UI, API, business logic, database/stored-procedure work, validation, and authorization required for that feature.
3. Run relevant build checks and automated tests.
4. Provide a handover so the user can run and manually test the feature.
5. Address feedback and repeat testing where required.
6. Mark the feature **Complete** only after implementation, required checks, and user acceptance.
7. Move to the next feature only after the current feature is Complete.

Do not develop multiple pages in parallel or move ahead while the current feature has unresolved issues.

## 2. Development rules

- Follow the approved SRS, proposal, database design, Figma designs, and confirmed decisions.
- Do not invent or silently change business requirements.
- If a dependency or conflict prevents implementation, explain the exact issue.
- Do not replace approved requirements with a preferred alternative.
- Do not use mock data instead of required real API/backend integration.
- Follow the established project architecture, naming conventions, coding standards, and technology choices.
- Do not run database migrations, apply schema changes, execute seed scripts, or perform destructive operations without explicit user approval.
- Keep existing functionality working and avoid unrelated refactoring.
- Update this roadmap as work progresses and keep statuses accurate.

## 3. Status definitions

| Status | Meaning |
|---|---|
| Not Started | Work has not started. |
| In Progress | Implementation is underway. |
| Ready for Testing | Implementation and automated checks are complete; user can manually test. |
| Changes Requested | User has reported issues or requested changes. |
| Complete | Implementation, relevant checks, and user acceptance are complete. |
| Blocked | Work cannot proceed because a dependency or decision is unresolved. |

**Do not mark a page Complete merely because code has been written or the build succeeds. The user must manually test and approve it.**

## 4. Module 01 – User & Access Management

### Implementation sequence

| Step | Page / Feature | Main testing focus | Status |
|---:|---|---|---|
| 0 | Authentication foundation | Login API, token generation/refresh, session handling, idle timeout, logout, lockout, authorization policies | Ready for Testing |
| 1 | Login Page | Input validation, invalid credentials, lockout behaviour, successful login, session handling | Not Started |
| 2 | Dashboard / Landing Page | Role-based landing experience and access restrictions | Not Started |
| 3 | User List | Search, filters, paging, sorting, authorized data visibility | Not Started |
| 4 | Create User | User creation, required fields, validation, duplicate checks, authorization | Not Started |
| 5 | User Details / Edit | Load and update user information, validation, unauthorized update prevention | Not Started |
| 6 | User Account | Process and platform access configuration | Not Started |
| 7 | Role Management | Role management rules supported by approved requirements | Not Started |
| 8 | Permission Management | Permission configuration and enforcement | Not Started |
| 9 | Menu Management | Menu configuration and platform settings | Not Started |
| 10 | User Role Assignment | Assign/remove roles and prevent unauthorized privilege escalation | Not Started |
| 11 | User Territory | Territory assignments and applicable access restrictions | Not Started |
| 12 | User Hierarchy | SM/ASM mapping, effective dates, and hierarchy rules | Not Started |
| 13 | Password Management | Admin password reset and Admin password reveal, with required permission checks and audit records | Not Started |
| 14 | Login History | Login event records, filtering, and access restrictions | Not Started |
| 15 | Audit Log | Admin/action audit records, filtering, and access restrictions | Not Started |

### Module 01 progress

- Completed pages: **0 / 15**
- Authentication foundation: **Ready for Testing** (implemented and automatically verified; awaiting the user's manual testing and approval. Migration, procedure scripts and the bootstrap are **not** applied – each needs separate user approval.)
- Current feature: **Authentication foundation**
- Next feature: **Login Page**, after the foundation is complete and verified.

This is the planned implementation order. If an approved requirement or technical dependency requires a change, document the reason before changing the order.

## 5. Per-feature implementation and handover checklist

### Before implementation
- Confirm the current page/feature scope from approved project sources.
- Identify required UI components, API endpoints, application services, stored procedures, database objects, validation rules, and authorization rules.
- Identify dependencies and unresolved requirements.
- Do not begin the next page/feature.

### During implementation
- Implement the complete vertical slice required for the current feature.
- Follow the existing architecture and conventions.
- Implement loading, empty, success, and error states where applicable.
- Apply server-side validation and authorization; do not rely only on UI restrictions.
- Add or update relevant automated tests.
- Avoid unrelated changes.

### Before handover
- Build the affected solution/projects.
- Run relevant unit, architecture, and integration tests that are available.
- Report tests that could not be run and explain why.
- Review changed files for accidental scope expansion and security issues.

### Handover report

For every feature, provide:

1. Feature/page name and implementation summary.
2. Files created and modified.
3. API endpoints and stored procedures used.
4. Database/schema changes, if any.
5. Validation and authorization behaviour.
6. Build and automated test results.
7. Run instructions and exact manual testing steps.
8. Known limitations, dependencies, or blocked checks.
9. Roadmap status and the next planned feature.

After handover, stop and wait for the user's manual testing feedback. If changes are requested, fix and retest the same feature. Mark it Complete only after the user approves it.

## 6. Claude Code master instruction

Use this instruction when starting development:

> Follow `DEVELOPMENT_ROADMAP.md` as the source of truth for implementation order and progress.
>
> Start with Module 01, Step 0: Authentication foundation. Implement only this feature and its required backend/security work. Run the relevant checks and provide the handover report defined in this document.
>
> Do not start the Login Page or any later feature until the current feature is complete and the user explicitly approves moving forward. After each feature, update this roadmap's status and progress.
>
> Do not mark a feature Complete based only on code completion or a successful build. The user must manually test and approve it.
>
> Follow approved project requirements and existing architecture. Do not change requirements, invent business rules, use mock data in place of required integrations, or perform migrations, seed operations, or destructive database actions without explicit approval.
>
> If a requirement, dependency, or source-document conflict blocks implementation, stop and report the exact issue rather than guessing.

## 7. Progress history

| Date | Module | Feature | Status | Notes |
|---|---|---|---|---|
| — | Module 01 | Roadmap prepared | Not Started | Implementation has not yet been recorded as started. |
| 2026-11-05 | Module 01 | Step 0 – Authentication foundation | In Progress | User authorized Step 0. Login-path password encryption items approved (Password_Encryption_Design.md §11.1). Specification first (G0) before database/backend work. |
| 2026-11-05 | Module 01 | Step 0 – Authentication foundation | In Progress | Draft spec written: `Documentation/Specs/Module01/Step00_Authentication_Foundation_Spec.md` (D-01..D-31, C-01..C-09). Awaiting user G0 approval and decisions. Baseline gates: build 0 warnings, unit 228/228, architecture 138/138. |
| 2026-11-12 | Module 01 | Step 0 – Authentication foundation | In Progress | G0 approved by the user (D-01..D-31, C-01..C-09; spec §15). Implementation started. |
| 2026-11-12 | Module 01 | Step 0 – Authentication foundation | Ready for Testing | Implemented: 9 tables (migration `Module01AuthenticationFoundation`, not applied), 15 Stored Procedure scripts (not executed), login/refresh/logout/me API, JWT + rotating refresh tokens with reuse detection, 30-min server-side idle timeout, 12-h absolute lifetime, lockout, AES-256-GCM verification, permission-policy mechanism, rate limiting, Development-only `bootstrap-admin`. Independent review: no Critical/High; 3 Medium + Lows fixed. Gates: build 0 warnings, unit 566/566, architecture 269/269. G7 integration Blocked (no test database). Awaiting user manual testing. |
| 2026-11-12 | Module 01 | Step 0 – Authentication foundation | Ready for Testing | Development database deployed with user approval (FORGE4 / CGE_FarmMonitoring, SQL Server 2016 SP2, compat 130, CI collation): migration `20261112032904_Module01AuthenticationFoundation` applied; 15/15 procedure scripts executed. Verified: 9 tables, 101 columns match the approved workbook, 9 PK + 9 UQ + 3 IX + 12 DF + 2 CK named per §4.2, 0 foreign keys, 0 non-dbo objects, 15 procedures, read-only procedure smoke calls OK. No data seeded; SYSTEM / first Administrator bootstrap **not run** (awaiting separate approval). |
