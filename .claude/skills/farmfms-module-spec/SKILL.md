---
name: farmfms-module-spec
description: Template and rules for a Farm FMS module implementation specification in Documentation/Specs/<Module>/. Use when the solution-architect prepares or updates the specification that gate G0 approves before any database or backend work.
---

# Farm FMS – Module implementation specification

**Location:** `Documentation/Specs/<Module>/<Module>_Implementation_Specification.md` (e.g. `Documentation/Specs/Module01/Module01_Implementation_Specification.md`). One file per module; large modules may add `<Module>_<Slice>.md` files linked from it.
Status starts as **Draft – Pending Approval**; only the user changes it to Approved (G0).

## Required sections
1. **Document control** – version, date, status, sources with versions (DB spec, ADs, SRS sections).
2. **Scope** – slices in / out; authorization reference; dependencies on other modules.
3. **Requirements covered** – FR IDs → acceptance criteria (from business-analyst); draft-source items marked.
4. **Data model** – tables / columns exactly as the approved DB spec (no new objects); key / constraint / index names from `Database_Conventions.md`.
5. **Stored Procedure inventory** – name (preserved or new per `Usr` rule), operation, parameters, result (`@NewId` / `@RowsAffected` per AD-09), business errors (number in the module range + code + type + safe message).
6. **Application layer** – services, interfaces, requests / DTOs / read models (AD-10), validation, audit actor use (AD-14).
7. **API** – routes `api/<module>/<resource>` (AD-18), verbs, status codes, authorization policy per endpoint (AD-13); **no unauthenticated admin endpoint**.
8. **Frontend impact** – screens and API clients (only if frontend foundation is approved).
9. **Tests** – unit / architecture / API host / integration (G7 blocked until a test DB exists).
10. **Decisions required and open CQs** – each with source, impact and owner; a slice with an open P1 item cannot pass G0.
11. **Traceability** – FR ↔ procedure ↔ endpoint ↔ test.

## Rules
Follow `farmfms-spec-compliance`. Do not change approved specifications or ADs; list conflicts in section 10.
