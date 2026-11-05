---
name: farmfms-spec-compliance
description: Farm FMS source-of-truth order, document status index, citation rules and the clarification procedure. Use whenever analysing requirements, writing a specification, or deciding whether something may be implemented — before treating any document as a requirement.
---

# Farm FMS – Specification compliance

## Source-of-truth order
1. Explicit user decisions in the session.
2. Approved specifications and Mandatory / Approved ADs.
3. Proposed ADs and the Reference Mapping — follow, but flag deviations.
4. Draft SRS / Functional Requirements — conflicts become clarification questions.
5. Analysis documents — evidence only, never requirements on their own.

## Document index (check the status line in each file before relying on it)
| Document | Path | Status |
|---|---|---|
| Architecture Decisions (authoritative) | `Documentation/Architecture/FarmFMS_Architecture_Decisions_v2.md` | Per decision (Mandatory / Approved / Proposed / Pending / Blocked) |
| Reference Mapping | `Documentation/Architecture/FarmFMS_Reference_Mapping.md` | Proposed |
| Module 01 DB specification | `Documentation/06_Database/Farm_FMS_Module_01_Database_Specification_v0.2.xlsx` | Approved column baseline |
| Database conventions | `Documentation/06_Database/Database_Conventions.md` | Approved (per section) |
| Module 02 DB specification | `Documentation/06_Database/Farm_FMS_Module_02_Database_Specification.md` | Draft v0.1 |
| SRS | `Documentation/Requirements/Software_Requirements_Specification.md` | Draft v0.2 – pending stakeholder approval |
| Functional requirements | `Documentation/Requirements/Functional_Requirements.md`, `Functional_Requirements_Analysis.md` | Draft / analysis |
| Clarifications, traceability, roles, workflows, UI mapping | `Documentation/Business_Design/` | Analysis |
| Password design | `Documentation/Security/Password_Encryption_Design.md` | Draft – Pending Approval (AD-11 blocked) |

## Rules
- Cite identifiers: FR-xx-nn, AD-nn, DBG-nnn, CQ-nnn (and spec sheet/section names) in every deliverable.
- Never rename approved database objects (tables, columns, procedure names — Module 01 names are preserved exactly, incl. `User`).
- Never edit an approved specification or AD to make an implementation fit. Record the conflict instead.
- Never treat a draft or analysis document as approved.
- Never invent business requirements, roles, fields, statuses or rules.

## When something is missing or contradictory
1. Stop the affected work.
2. Record: what, where (document + section), why it blocks, options **only if a source documents them**, suggested owner.
3. Return it in the hand-off under "Open questions / Decisions requiring human approval". The business-analyst may add it to `Documentation/Business_Design/Business_Clarification_Register.md` as a new CQ when asked.
