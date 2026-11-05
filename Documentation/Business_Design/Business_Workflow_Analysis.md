# CGE Farm Monitoring System – Business Workflow Analysis

| | |
|---|---|
| Document | Business_Workflow_Analysis.md |
| Version | 0.2 – Updated with clarification responses |
| Date | 05-11-2026 |
| Status | **Analysis only – pending stakeholder validation and formal sign-off.** User clarification responses are applied as working decisions; unresolved workflow details remain open. |
| Sources | S1–S10 as defined in [Functional_Requirements_Analysis §1.1](Functional_Requirements_Analysis.md#11-source-register) |

## 0. Reading guide

**Delivery sequence:** Phase 1 – Web first; Future Phase – Mobile later. Mobile workflows are documented for the future mobile delivery and are not reinterpreted as Phase 1 web screens.


Each workflow lists:
1. **Documented flow:** steps stated in a source, with citation.
2. **Prototype evidence (S5 / S6):** design intent only.
3. **Missing steps**, **unclear transitions**, **validations** and **approval rules.**
4. **Related:** FR / CQ / DBG / UI IDs.

**States** list only values found in a source. A state model is **not proposed** where the sources do not define one.

| # | Workflow | Updated position | Remaining key dependencies |
|---|---|---|---|
| WF-01 | User Management | Roles confirmed: Admin, SM, ASM; Supervisor = ASM | Security approval for reversible encryption; confirm user-field/schema alignment |
| WF-02 | Area and Hierarchy Management | Province → District → Region → Territory; ASM assigned Regions and Territories | Assignment cardinality, overlap and reassignment history |
| WF-03 | Farm Management | Mobile-created farms require SM approval; GPS mandatory at mobile registration; Main Farm may have unlimited Dealer Farms | Web-created farm approval; rejection/resubmission; farm/dealer data rules |
| WF-04 | Itinerary Planning and Approval | Daily plan, one Territory/day; ASM defines recurring plans; SM approves | Recurrence generation details; post-visit approval handling for unplanned visits |
| WF-05 | Farm Visits | Final submission; multiple visits per farm/day; GPS at visit start only | Fixed visit fields vs configurable questionnaire; immutable submission/correction process |
| WF-06 | Batch Monitoring | One breed per batch; SRS formulas retained | Batch closure/reopening and remaining lifecycle rules |
| WF-07 | Visit Questionnaires | Version on every published change; conditional questions; approved form pack expected | Fixed-field boundary; form content and validation details |
| WF-08 | Expense Management | Approver may modify variable amount; only assigned ASM's SM approves | Eligibility/limits, rejection/resubmission details |
| WF-09 | Reporting and Analytics | SRS-listed reports only; Farm Monitoring only | Exact report layouts/filters where SRS is silent |
| WF-10 | Audit and Notifications | SRS audit events only; in-app notifications only | Read/unread lifecycle and retention where SRS is silent |
| WF-11 | Offline Data Capture and Synchronization | Server wins conflicts; no day-end lock | Retry/idempotency, conflict logging and dependency order |


---

## 0.1 Clarification decisions applied

The following are the working business decisions used in this version:

- **Roles and approvals:** Administrator, SM and ASM only; Supervisor = ASM. SM approves itineraries, mobile-created farms and expenses. Expense approval is by the ASM's assigned SM only (CQ-001–003, CQ-031).
- **Geography:** Province → District → Region → Territory. ASMs are assigned Regions and Territories and can operate only within assigned areas (CQ-006–007, CQ-009).
- **Itineraries:** daily, one Territory per day, selected farms; ASM defines recurring plans and SM approves. Unplanned visits are allowed with post-visit approval (CQ-008, CQ-010–012).
- **Visits:** submitted visit is final; multiple visits per farm/day are allowed; GPS is captured at visit start only. Unplanned-visit approval is a separate approval outcome, not a visit-edit workflow (CQ-012–013, CQ-026, CQ-041).
- **Farm:** Farm Monitoring only. Farm Type is a farm-master attribute. Mobile farm registration requires GPS and SM approval. One Main Farm may have unlimited Dealer Farms (CQ-014, CQ-022, CQ-027–029).
- **Questionnaire:** fixed system visit fields coexist with a configurable questionnaire; each published change creates a new version; conditional questions are required; Admin configures mandatory status and validation; approved forms are expected (CQ-015–020).
- **Batch:** one breed per batch; retain SRS calculation formulas (CQ-021, CQ-023).
- **Expenses:** approver may modify variable amount; only assigned ASM's SM approves (CQ-030–031).
- **Reports, audit and notifications:** SRS reports only; SRS audit events only; in-app notifications only; out-of-SRS features excluded (CQ-032–035).
- **Offline:** server wins conflicts; no day-end lock (CQ-024–025).
- **Other:** reversible AES-256-GCM remains subject to security approval; transactions belong to individual users; Breeder Farm is a separate supplier/master entity; no external sharing; retain SRS NFR targets; use Figma A/B/C grading and SRS lists (CQ-036–046).

These decisions are based on clarification responses and are not a substitute for formal stakeholder sign-off. Items not answered in detail remain open; no additional workflow states are invented.

---

## WF-01 User Management

**Documented flow**
1. The Administrator creates a user: Name, Role, Email, Contact Number, Department, User Id, Active Status (S1 p.5).
2. The Administrator assigns the supported role(s). The clarified business roles are Administrator, Sales Manager (SM) and Area Sales Manager (ASM); Supervisor is a synonym for ASM, not a separate role (CQ-001–003).
3. The Administrator creates the user's account: one per user, one Process, platform flags Web/Mobile (both disabled by default), territories (S3).
4. The user logs in with Username + Password (S3). Rules:
   - 5 failed attempts lock the account, and only an Administrator can unlock it.
   - The session times out after 30 minutes of inactivity.
   - Concurrent sessions are allowed.
   - Every login, failed login and logout is recorded (S3).
5. The Administrator can edit, activate or deactivate a user (S1 p.4). Deactivation blocks login, revokes sessions and preserves history (S3).
6. Reversible AES-256-GCM was requested subject to security approval (CQ-036). This is not yet approved for implementation; password storage must not be made reversible without an explicit security-approved design.

**States (documented):** User: Active / Inactive (S1 p.5); Locked (S3 `IsLocked`).

**Prototype evidence:**
- S5 separates people from reassignable role-based accounts (SM-001 / ASM-001) and keeps reassignment history.
- S5 user Type: Internal / Field / External.
- Users created on the S5 screen cannot log in (prototype defect).

**Missing steps**
- First-login password change: S3 has `PwdChangeReq`, but the trigger is not defined.
- Password reset flow (Administrator-initiated or self-service).
- The clarified scope is Farm Monitoring only; do not create a Farm Evaluation process (CQ-014).

**Unclear transitions:** Reactivating a locked or deactivated user, and whether sessions are restored.

**Validations:**
- Documented: unique username; password complexity (S3).
- Not documented: email format and uniqueness; contact number format.

**Approval rules:** none; administration is direct.

**Related:** FR-UM-01..13 · CQ-001, CQ-036, CQ-037, CQ-038 · DBG-001..014 · UI-W-01, UI-W-04..08, UI-M-01, UI-M-15.

---

## WF-02 Area and Hierarchy Management

**Documented flow**
1. The Administrator maintains the geography hierarchy: Province → District → Region → Territory (CQ-006).
2. The Administrator maps multiple ASMs under a Sales Manager (two levels) (S1 p.6). S3 adds: one current manager per ASM, effective dates, no overlaps, and the old row is closed when a new row is added.
3. The Administrator assigns ASMs to Regions and Territories (CQ-007). The detailed assignment cardinality, overlap and inheritance rules remain open.
4. The system restricts ASMs to their assigned Regions and Territories (S1 p.6).

**Prototype evidence:**
- S5 Area Demarcation: Area → Main Route → Sub Route, with farms mapped to Sub Routes and an "Auto-Sequence" toggle that is not explained.
- S5 Reporting Hierarchy: SM account → ASM accounts, soft delete, no effective dates.
- No S5 screen assigns territories to ASMs. Main Route / Sub Route structures are not part of the clarified SRS baseline and are excluded (CQ-034).

**Missing steps**
- Region / Territory maintenance screens (S5 has Area / Route only).
- ASM territory assignment screen and rules.
- Handling of farms, itineraries, batches and pending approvals when an ASM is reassigned or replaced; assignment history and effective dates (CQ-007).

**Unclear transitions**
- Reassigning a territory: what happens to open itineraries and batches?
- Hierarchy change: who approves items that were already pending?

**Validations:** an ASM has exactly one current SM; dates do not overlap (S3). Whether multiple ASMs may share a Territory, and whether Region assignment automatically includes child Territories, remain open (CQ-007).

**Approval rules:** none.

**Related:** FR-AH-01..06 · CQ-006, CQ-007, CQ-031, CQ-046 · DBG-003, DBG-020..023 · UI-W-05, UI-W-06, UI-W-10.

---

## WF-03 Farm Management (including New Farm Approval)

**Documented flow**
1. **Web creation:** Admin or SM captures the farm master data listed in S1 p.6. Approval for web-created farms is not confirmed by the clarification response and remains open.
2. **Mobile creation:** the ASM captures farm and farmer details for a new farm or dealer farm discovered during visits (S1 p.5, p.7). GPS coordinates are mandatory during mobile farm registration (CQ-027).
3. The mobile-created farm syncs to the web and enters **New Farm Approval** (S1 p.7).
4. The SM assigned to that ASM reviews and approves it (CQ-028, CQ-031).
5. On approval (S1 p.7):
   - the farm becomes active;
   - the assigned ASM can plan and record visits;
   - visit data capture is enabled.
6. The ASM is notified of the outcome; SMs see pending approvals (S1 p.14).
7. Farm visit history and historical data can be viewed (S1 p.6).

**States (documented):**
- S1: Active Status. Approval implies a pending state, which is not named.
- S6: Pending Synchronization → Submitted → Approved / Rejected.

**Prototype evidence:**
- S5 Approve New Farm is a placeholder screen.
- S6 duplicate warning checks only local records and can be overridden with "Proceed Anyway".
- S6 capture lacks Farm Type, GPS, Region and Territory.
- S5 farm form lacks Farm Type and GPS.

**Missing steps**
- **Rejection:** reason, notification content, and whether the farm can be corrected and resubmitted (FR-FM-08).
- Duplicate detection before approval (FR-FM-12).
- Farm Code assignment point (DBG-053), if not already specified in the SRS.
- Dealer association step: one Main Farm may have unlimited Dealer Farms (CQ-029); remaining relationship fields/rules are open.
- Farm deactivation impact on open batches and itineraries.

**Unclear transitions**
- Can the ASM edit a submitted, unapproved farm? Rejection reason and correction/resubmission rules remain unspecified.
- Can an approver modify the data before approving?

**Validations:** SRS mandatory fields and mobile-registration GPS requirement apply; remaining farm validation rules and rejection behaviour are open.

**Approval rules:**
- Mandatory for mobile-created farms.
- Clarified approver: the SM assigned to the ASM (CQ-028, CQ-031).
- The approval must route to the ASM's assigned SM (CQ-031).

**Related:** FR-FM-01..12 · CQ-022, CQ-027, CQ-028, CQ-029, CQ-031 · DBG-050..055 · UI-W-11, UI-W-12, UI-W-25, UI-M-03..05.

---

## WF-04 Itinerary Planning and Approval

**Documented flow**
1. The ASM defines and plans a **daily** itinerary, selecting **one Territory per day** and the required farms (CQ-008, CQ-010).
2. The ASM schedules **multiple farm visits** within that territory (S1 p.7).
3. Unplanned visits are allowed, but ASM activity remains restricted to assigned areas (CQ-009, CQ-012). An unplanned visit requires post-visit approval; it is not permission to visit an unassigned area.
4. The ASM defines their own recurring plans; the SM approves them (CQ-010–011). The recurrence pattern and generation rules remain open.
5. The SM approves the ASM's itinerary (CQ-001, CQ-011). Approval routing is to the SM assigned to that ASM; other administrator intervention rules remain subject to the SRS.
6. If the ASM has no itinerary, an Administrator or SM creates and assigns one (S1 p.7).
7. The ASM is notified of the approval outcome; SMs see pending itinerary approvals (S1 p.14).

**States (documented):**
- S1: none named.
- S5: draft / approved / rejected, with no submit step and no reject action.
- S6: Pending / Approved / Rejected.

**Prototype evidence:**
- S5: a weekly grid of Sub Route × day per ASM. Approved plans and past days are locked. ASMs can approve their own plans.
- S6: one Sub Route per day, with no farm selection, and plans can be edited or deleted after approval.
- S5 Visit Amendment is a placeholder.

**Missing steps**
- An explicit **submit** step.
- **Rejection** with reason and resubmission (FR-IP-08).
- **Amendment** and **cancellation** of approved itineraries (FR-IP-09; S4 "Amend current date Visit Plan").
- Approval deadline (e.g., before the planned date).
- Link from the approved itinerary to visit capture (FR-IP-10).
- Handling of planned farms that were not visited.

**Unclear transitions**
- Daily planning, one Territory per day, with selected farms is confirmed (CQ-008).
- Whether an itinerary assigned by an SM needs approval.
- The post-visit approval record, timing, and exception criteria for unplanned visits (CQ-012).
- Recurring plan patterns, generation horizon, editing/cancellation and whether approval is per series or per occurrence (CQ-010–011).

**Validations:**
- Documented: one territory per day (S1).
- Implied: farms must be approved and active (S1 p.7); territory within assignment (S1 p.6, but see CQ-009).
- S6 adds "Only one plan allowed per day".

**Approval rules:**
- Approver: the ASM's assigned SM.
- The assigned-SM approval route is confirmed. Detailed rejection/resubmission and amendment behaviour remain open.

**Related:** FR-IP-01..10 · CQ-008..012, CQ-031 · DBG-060 · UI-W-17, UI-W-26, UI-M-06, UI-M-07.

---

## WF-05 Farm Visits

**Documented flow (S1 p.9–11)**
1. The ASM starts a visit for a planned or unplanned farm (S1 p.7; S2 p.4).
2. **Visit Initialization:**
   - select Visit Type: Farm or Dealer;
   - if Dealer: Dealer Name, Used Feed Type, Feed Volume;
   - the system captures GPS at visit start only (CQ-026);
   - Farm Type is read from the farm master; it is not selected as a visit-level field (CQ-022).
3. **Batch selection or creation** (see WF-06).
4. **Farm-type specific data** (S1 p.10).
5. **Common data:**
   - Feed Type and Feed Form;
   - Housing Type;
   - Water Availability.
6. **Additional inputs:**
   - Farm Management Grading (if applicable);
   - Complaints & Observations;
   - Recommendations & Comments;
   - Images.
7. Mandatory validations are enforced; data is stored for reporting (S1 p.11).
8. The visit is saved offline and synced (WF-11).

**States (documented):**
- S1: none.
- S6: "in progress – Not synced"; history shows "Completed".
- S5: visit status `completed`; reports show visits "Approved / Pending".

**Prototype evidence:**
- S6 capture is a fixed form, not a questionnaire.
- S6 creates one visit record per batch.
- S6 does not save the Dealer fields.
- S6 "Continue Visit" does not resume the saved data.
- S6 GPS falls back to fake coordinates.

**Missing steps**
- Check-in / check-out (visit start and end time, GPS at both points) (CQ-026, CQ-041).
- A submitted visit is final and cannot be edited (CQ-013).
- Correction process.
- Post-visit approval for an unplanned visit is required (CQ-012); it is a separate approval outcome and does not imply editing the submitted visit.
- External sharing with farmers is excluded (CQ-040).
- Farm Monitoring only; Farm Evaluation is excluded (CQ-014).

**Unclear transitions**
- Whether an unsubmitted draft can be resumed across sessions remains unspecified.
- Multiple visits to the same farm on the same day are allowed (CQ-041).
- Dealer visit → whether batch and farm-type steps still apply (CQ-029).

**Validations**
- Documented: GPS captured; mandatory fields "where applicable" (S1).
- Needed but undefined:
  - which fields are mandatory per farm type;
  - numeric ranges (e.g., current batch size ≤ initial size? mortality ≤ size?);
  - image count per farm type (S2 p.5);
  - GPS accuracy and distance policy; GPS is captured at visit start only (CQ-026);
  - "Other" requires text (documented for Feed Type and Housing Type, S1 p.11).

**Approval rules:** submitted visits are final and are not subject to a general visit approval workflow (CQ-013). Unplanned visits require a separate post-visit approval (CQ-012).

**Related:** FR-VC-01..19 · CQ-012..015, CQ-026, CQ-029, CQ-040, CQ-041, CQ-043 · DBG-061, DBG-062 · UI-M-08, UI-M-09, UI-M-04, UI-W-12.

---

## WF-06 Batch Monitoring

**Documented flow**
1. During a visit, the ASM selects an **existing open batch** or **creates a new batch**: Batch Start Date, Initial Batch Size and one Breed (CQ-021).
2. Multiple batches per farm are supported. New batches can be created without closing existing ones (S1 p.11).
3. Previously entered batch data is available for selection and update (S1 p.11), carried forward across visits (S2 p.5).
4. The system calculates (S1 p.12):
   - age from the start date (Broiler days, Layer weeks, Dairy months);
   - FCR;
   - mortality.
   These update when data changes.
5. Batches are closed (S2 p.5 "creation, update, and closure"). Active and closed batches are kept per farm (S2 p.5).
6. Batch-level reports show lifecycle status and performance across visits (S1 p.13).

**States (documented):**
- S1: "open".
- S2: active / closed.
- S5 and S6: `active` / `closed`.

**Prototype evidence:**
- S6 has two incompatible batch screens:
  - Farm Details: breed, quantity, placement date; close with no data.
  - Visit: multiple breeds, initial size, start date, running cumulative feed and mortality.
- S5 shows closed-batch metrics (Final FCR, Final Weight, Duration, Close Date) as read-only mock data.
- S5 batch number format `B2026-001`.

**Missing steps**
- **Closure:** who closes, when, closure data and reopen rules remain open; CQ-021 only confirms one breed per batch.
- Quantity adjustments other than mortality.
- Batch identifier.
- Alerts for unclosed batches are excluded unless included in the SRS (CQ-032, CQ-034).

**Unclear transitions**
- Can a batch be created outside a visit (S6 Farm Details does)?
- Is a closed batch excluded from selection at the next visit (implied by "open batch", S1 p.9)?

**Validations:** inputs and formulas are inconsistent across sources (CQ-023):
- Use the SRS formulas as written (CQ-023); any missing formula inputs/units in the SRS remain open;
- feed inputs for Broiler;
- cumulative vs per-visit mortality;
- age unit for Mixed if not explicitly defined in the SRS.

**Approval rules:** none documented.

**Related:** FR-VC-06, FR-BM-01..09 · CQ-021, CQ-023, CQ-032 · DBG-040..042 · UI-M-04, UI-M-09, UI-W-12.

---

## WF-07 Visit Questionnaires

**Documented flow**
1. An authorised user (Administrator; SM per S1 p.8 "e.g.") defines questionnaires on the web (S1 p.8):
   - questions customised by farm type and operational need;
   - conditional dependencies between questions;
   - per question: Admin-configured mandatory status and validation; conditional questions; supported answer types are Multiple Choice, Single Choice, Dropdown, Checkbox, Text, Image, GPS, Rating, Number, Data Set and Signature (CQ-017–020).
2. Every published questionnaire change creates a new version (CQ-016). Changes take effect without redeployment (S1 p.8).
3. Questionnaires become available on mobile; the ASM completes the relevant questionnaire at every visit (S1 p.8).
4. The existing questionnaire structure is replicated (S1 p.9).

**States (documented):**
- S1: none.
- S5: Active flag only.

**Prototype evidence:**
- S5 builder: Questionnaire → Sections → Questions → Options, with 11 answer types.
- S5: no versioning ("Duplicate" only), hard delete, no farm-type assignment, no conditional logic.
- S6: no questionnaire at capture; sectioned questionnaire shown in history.

**Missing steps**
- Approved questionnaire forms are expected from the business (CQ-019); versioning is required on every published change (CQ-016). Offline-device migration behaviour remains open.
- Assignment to farm type, visit type and process (CQ-020, CQ-014).
- Retirement of questions that already have answers.

**Unclear transitions**
- The visit data fields are fixed system fields, while configurable questionnaire content is also required. The boundary and relationship between these two layers must be clarified before implementation (CQ-015).
- Conditional questions are required; exact dependency operators and evaluation behaviour remain open (CQ-017).
- Behaviour when a new version is published while a visit is in progress offline.

**Validations:** mandatory answers enforced; options required for choice types (implied). Admin configures mandatory fields and validation per question; detailed validation rules depend on the approved form pack (CQ-020).

**Approval rules:** no separate questionnaire-publishing approver was confirmed. Every publish must create a new version (CQ-016); publishing permissions beyond Admin configuration remain open.

**Related:** FR-VQ-01..08 · CQ-014..020 · DBG-030..034 · UI-W-16, UI-M-09.

---

## WF-08 Expense Management

**Documented flow (S1 p.13)**
1. On the web, expense types are defined as **Fixed** (predefined amount) or **Variable**, each with an *image proof mandatory* flag.
2. On mobile, the ASM records an expense during field visits:
   - Fixed: the amount is auto-filled and not editable.
   - Variable: the ASM enters the amount.
   - An image is captured if the type requires one.
3. Expenses sync to the web and enter **Expense Approval**.
4. The SM assigned to the ASM reviews, then **approves or rejects** (CQ-031). For Variable expenses the approver may **modify or confirm the reimbursable amount**.
5. The ASM is notified of the outcome; SMs see pending expenses (S1 p.14).
6. Financial processing and reimbursement are out of scope (S2 p.7).

**States (documented):**
- S1: approve / reject (implied pending).
- S5: pending / approved / rejected / cancelled.
- S6: offline (Draft) → pending → approved / rejected, with a rejection reason.

**Prototype evidence:**
- S5 enforces a rejection reason but does not enforce image-required or allow amount edits.
- S6 has no image capture.
- Currency: LKR on expense screens, `$` on dashboards.

**Missing steps**
- Rejection reason.
- Resubmission or cancellation.
- Duplicate or limit checks ("predefined criteria", S1 p.5).
- Expense eligibility/limits and any required link to a visit or farm (CQ-030).

**Unclear transitions**
- Rejected → resubmitted?
- Pending → cancelled by the ASM?
- Approved → final and immutable?

**Validations:**
- Documented: Fixed amount locked; image mandatory per type.
- Undefined: limits, frequency (e.g., one daily allowance per day), date range (back-dating).

**Approval rules:**
- Approver: only the SM assigned to the ASM (CQ-031).
- Single level (no multi-level approval in any source).
- Variable amount modification by the approver is confirmed (CQ-030).
- Approval must be performed by the ASM's assigned SM; administrator approval is not included in the clarified rule (CQ-031).

**Related:** FR-EX-01..09 · CQ-030, CQ-031 · DBG-063, DBG-064 · UI-W-15, UI-W-18, UI-W-19, UI-M-02, UI-M-10.

---

## WF-09 Reporting and Analytics

**Documented flow:** An authorised user selects a report and filters, then views the report or dashboard (S1 p.4–5, p.13–14). Report categories:
- Farm-Level (S1 p.13);
- Batch-Level (S1 p.13);
- Supervisor and Area-Based (S1 p.14).

The ASM accesses reports on the web (S1 p.5).

**Additional evidence (not in current approved scope unless also present in S1)**
- **S2 p.6:**
  - ASM-wise and area-wise reporting;
  - feed type comparison;
  - output vs input dashboards;
  - map-based visualization of visits with visit sequence;
  - filters by Date, Farm Type and Feed Type.
- **S2 p.9:** Reporting Parameter Management.
- **S4:** nine named legacy reports:
  - Visit Summary, Visit Detail, Visit Plan, Expense, Feedback;
  - Farm Monitor Visit and Farm Evaluation (DevExpress "required format");
  - Sync Status, Sync History.
- **S5:** all report menus render one generic page; Excel / PDF export buttons are present but do nothing.

**Scope decision:** only reports listed in the SRS are included (CQ-033). Farm Evaluation is excluded; Farm Monitoring only (CQ-014). Proposal and legacy-only reports are not automatically in scope.

**Missing steps:**
- the report catalogue with columns and filters for the SRS-listed reports;
- KPI definitions for dashboards;
- export and print rights;
- the exact formats for SRS-listed reports where not defined. Farm Evaluation report is excluded.

**Unclear transitions:** n/a.

**Validations:** data scope by role (CQ-046).

**Approval rules:** none.

**Related:** FR-RP-01..08 · CQ-003, CQ-014, CQ-033, CQ-046 · UI-W-02, UI-W-03, UI-W-22, UI-M-02.

---

## WF-10 Audit and Notifications

**Audit – documented flow (S1 p.14, p.17)**
1. The system writes an audit record for:
   - user creation, modification, login and logout;
   - farm creation and modification;
   - approvals;
   - role-based access changes;
   - all login attempts and access to sensitive data where specified by the SRS.
2. Each record contains User, Date & Time, Action, Module, Previous Value and Updated Value.
3. Audit logs are read-only; authorised users (Administrator, S1 p.4) view them.

**Notifications – documented flow (S1 p.14–15)**
1. A system event creates an in-app notification.
2. **ASM (mobile)** receives the outcome of:
   - Visit Plan Approvals;
   - New Farm Approvals;
   - Expense Approvals.
3. **SM (web)** receives:
   - Pending Visit Plan Approvals;
   - Pending New Farm Approvals;
   - Pending Expense Approvals.
4. Users view their notification history.

**Additional evidence (excluded unless separately stated in the SRS)**
- **S2:** email gateway (p.9); pop-up images (p.5); alerts for upcoming visits and unclosed flocks (p.14).
- **S5:** Administrator broadcast notifications.
  - Statuses: Draft / Scheduled / Active / Expired.
  - Priorities: Low / Medium / High.
  - Targets: account types or individual users.
  - Response may be required; read and response counts are tracked.
- **S6:** response types: yes-no, accept-reject, acknowledge, options, text.

**Missing steps**
- Audit retention and archival remain unspecified. Audit event scope is restricted to the SRS-listed events (CQ-035).
- Notifications: in-app only (CQ-032). Email, SMS, broadcast messages and proposal/prototype-only alerts are excluded. Read/unread semantics and retention remain unspecified where the SRS is silent.

**Unclear transitions:** notification read / unread lifecycle.

**Validations:** audit records immutable (S1 p.14); do not expand event coverage beyond the SRS list (CQ-035).

**Approval rules:** n/a.

**Related:** FR-AU-01..06, FR-NT-01..07 · CQ-032, CQ-035 · DBG-008, DBG-065, DBG-072 · UI-W-21, UI-W-23, UI-M-12.

---

## WF-11 Offline Data Capture and Synchronization

**Documented flow (S1 p.15)**
1. **Initial Sync** (user-initiated, start of day): download the latest master **and transactional** data.
2. Offline capture of visits, farms, itineraries and expenses *[A: S1 does not enumerate the offline entities]*.
3. **Midday Sync** (user-initiated): upload new data and retrieve critical updates; also used as a forced sync.
4. **Automatic Sync** (background): triggered by network availability, time intervals or system events; automatic sync when connectivity returns.
5. **Day-End Sync** (user-initiated): upload all pending transactions so that daily activities are "properly completed or paused".
6. Offline GPS may have reduced accuracy.

**States:**
- S6 record statuses: "Pending Synchronization" → "Submitted".
- S6 Special Sync outcomes: success / partial / failed.

**Prototype evidence**
- S6 syncs only visits and expenses. Itineraries, feedback, new farms, notification responses, batches and attendance stay on the device.
- Offline mode in S6 is inferred from pending items, not from network state.
- S6 Maintenance includes Special Sync ("Reconciling records…"), Send DB, Delete DB and app update (mandatory / optional).
- S5 per-device numbering prefixes imply document numbers generated on the device.

**Missing steps**
- **Conflict detection and resolution**: server wins when the same data conflicts between device and server (CQ-024). Detailed conflict logging and retry behaviour remain open.
- Idempotent upload and retry of failed items.
- Handling of master data changed while offline: farm deactivated, itinerary changed, questionnaire republished, batch closed elsewhere.
- Day-end lock and Administrator unlock are excluded; no day-end lock is required (CQ-025).
- Warning or block on logout or Delete DB while unsynced data exists (S6 warns in text only).

**Unclear transitions**
- The SRS Day-End Sync remains, but it does not lock the user's day (CQ-025). The meaning of "completed or paused" in the SRS wording remains to be clarified.
- Whether automatic sync also downloads data.
- The sync order of dependent records, e.g., a new farm and its first visit, or a new batch and its visit. This must respect server-wins conflict handling.

**Conflict rule:** server wins (CQ-024). Retry, duplicate prevention/idempotency, conflict audit and server-side validation details remain to be specified without changing the confirmed winner rule.

**Approval rules:** none.

**Related:** FR-OF-01..08 · CQ-024, CQ-025, CQ-026, CQ-034 · DBG-070, DBG-071 · UI-M-13, UI-M-14.
