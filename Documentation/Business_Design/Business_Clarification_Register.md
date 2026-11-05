# CGE Farm Monitoring System -- Business Clarification Register

  --------------------------------------------------------------------------
  Field                               Value
  ----------------------------------- --------------------------------------
  Document                            `Business_Clarification_Register.md`

  Version                             0.2 -- Revised after business
                                      clarification responses

  Date                                05-11-2026

  Status                              Working decision record; formal
                                      stakeholder/security sign-off remains
                                      required where stated

  Supersedes                          Version 0.1 dated 04-11-2026
  --------------------------------------------------------------------------

## 1. Purpose and status rules

This revision records the answers supplied for CQ-001 to CQ-046.
"Answered" means the stated point has an answer. "Partially answered"
means one or more material sub-questions remain open. An answer in this
discussion is recorded as a project business direction, not
automatically as formal CGE sign-off. Signed SRS requirements and
formally approved change requests remain the authoritative baseline.
Unverified notes, prototype-only behaviour and draft analysis do not
supersede that baseline.

## 2. Summary

-   **Answered at the question level:** CQ-001, 002, 003, 004, 005, 006,
    008, 009, 011, 012, 013, 014, 015, 017, 018, 020, 022, 023, 024,
    025, 027, 029, 032, 033, 034, 035, 037, 038, 040, 042, 044, 045,
    046.
-   **Partially answered:** CQ-007, 010, 016, 019, 021, 026, 028, 030,
    031, 036, 039, 041, 043.
-   **No response supplied:** none at the top-level CQ question; several
    sub-questions remain open as detailed below.

## 3. Cross-question contradictions and dependencies

1.  **Roles and approval routing:** CQ-001/CQ-003 establish
    Administrator, SM and ASM only; Supervisor is an ASM synonym. CQ-031
    restricts approval to the ASM's assigned SM. CQ-007 and CQ-046 must
    define assignment cardinality and resulting data visibility.
2.  **Geography:** CQ-006 establishes Province → District → Region →
    Territory. CQ-007 supports ASM assignments to Regions and
    Territories but does not settle overlap, exclusivity, effective
    dates or assignment history.
3.  **Itinerary and visit approval:** CQ-008 establishes daily plans,
    one Territory per day, with selected farms. CQ-009 limits visits to
    assigned areas. CQ-012 allows unplanned visits with post-visit
    approval, while CQ-013 says visit submission is final and has no
    general visit approval. Clarify whether CQ-012 approval is an
    exception/authorisation record, not approval of the submitted visit
    itself.
4.  **Fixed fields and questionnaires:** CQ-015 confirms fixed system
    visit fields. CQ-016--020 also require configurable questionnaire
    behaviour, conditional questions, Figma answer types, supplied
    approved forms and Admin-configured validation. Define the boundary
    between fixed visit/batch/metric fields and configurable
    questionnaire questions.
5.  **Questionnaire versioning and offline use:** CQ-016 requires a new
    version for every published change. Active-version rules, historical
    question snapshots and handling of offline/in-progress visits remain
    open and depend on CQ-024.
6.  **Batch and farm type:** CQ-021 confirms one breed per batch but
    leaves most lifecycle rules open. CQ-022 makes Farm Type a farm
    master attribute; confirm its value set and legacy mapping.
7.  **Offline and GPS:** CQ-024 selects server-wins conflict resolution,
    but does not define retry, idempotency, partial failure, sync
    triggers or multi-device behaviour. CQ-026 captures GPS at visit
    start only; CQ-027 separately requires farm GPS during mobile
    registration.
8.  **Scope and security:** CQ-034 excludes all listed out-of-SRS
    features. CQ-039 separately confirms a Breeder Farm supplier/master
    entity; keep its implementation limited to that specific decision.
    CQ-036's reversible AES-256-GCM request is conditional on security
    approval and must not be implemented before approval.
9.  **Reports and audit:** CQ-033 limits reports to the SRS catalogue;
    CQ-035 limits audit to SRS-listed events. Prototype/Proposal-only
    additions must not be included without an approved change request.

## 4. Detailed register

### Governance and roles

#### CQ-001 -- Who performs farm visits and who approves? (P1)

-   **Answer:** ASM performs field visits. SM approves itineraries,
    farms and expenses.
-   **Status:** Answered.
-   **Implication:** Use Administrator, SM and ASM roles; apply CQ-031
    approval routing.
-   **Related:** CQ-003, CQ-011, CQ-028, CQ-030, CQ-031, CQ-046.

#### CQ-002 -- Is an Analyst role required? (P2)

-   **Answer:** No. Roles are Administrator, SM and ASM only.
-   **Status:** Answered.
-   **Implication:** Do not create a separate Analyst role.
-   **Related:** CQ-001, CQ-003, CQ-046.

#### CQ-003 -- What is a "Supervisor"? (P1)

-   **Answer:** Supervisor is the same role as ASM, not a separate role.
-   **Status:** Answered.
-   **Implication:** Standardise role labels as ASM in requirements, UI,
    permissions, database reference data and reports.
-   **Related:** CQ-001, CQ-002, CQ-046.

#### CQ-004 -- Status of "approved user decisions" (P1)

-   **Answer:** Only signed SRS requirements and formally approved
    change requests are authoritative.
-   **Status:** Answered.
-   **Implication:** Unverified decision notes, draft RTM entries and
    informal notes do not supersede the SRS. Any deviation requires an
    approved change request.
-   **Related:** CQ-005 and all conflicting draft requirements.

#### CQ-005 -- Sign-off status of SRS and Proposal (P1)

-   **Answer:** Both are approved.
-   **Status:** Answered in discussion; retain documentary evidence.
-   **Open action:** Store/reference signed copies, approved versions
    and approval dates in the source register.
-   **Related:** CQ-004; all baseline requirements.

### Geography and assignment

#### CQ-006 -- Canonical geographic hierarchy (P1)

-   **Answer:** Province → District → Region → Territory.
-   **Status:** Answered.
-   **Implication:** Do not add Area, Main Route or Sub Route as
    hierarchy levels unless approved by change request.
-   **Related:** CQ-007, CQ-008, CQ-009, CQ-027, CQ-046.

#### CQ-007 -- ASM assignment level and shared territories (P1)

-   **Answer:** Assign ASMs to Regions and Territories.
-   **Status:** Partially answered.
-   **Confirmed:** Support assignments at both Region and Territory
    levels.
-   **Open:** Whether Territory assignment is mandatory when a Region is
    assigned; whether Regions/Territories may be shared by multiple
    ASMs; whether a Territory assignment must belong to an assigned
    Region; effective dates/history; reassignment and pending-approval
    behaviour.
-   **Related:** CQ-006, CQ-009, CQ-031, CQ-038, CQ-046.

#### CQ-046 -- Data visibility by role (P2)

-   **Answer:** SM sees only the ASMs assigned to that SM.
-   **Status:** Partially answered.
-   **Open:** ASM visibility across web modules; Administrator
    visibility; historical access after assignment changes; whether
    historical records follow current or historical reporting lines.
-   **Implication:** Apply hierarchy-scoped row filtering; do not infer
    unanswered ASM/Admin rules unless the SRS explicitly defines them.
-   **Related:** CQ-001, CQ-007, CQ-031, CQ-038.

### Itinerary planning

#### CQ-008 -- Planning period and unit (P1)

-   **Answer:** Daily plan; one Territory per day; selected farms
    included.
-   **Status:** Answered.
-   **Related:** CQ-006, CQ-007, CQ-009, CQ-010, CQ-011, CQ-012.

#### CQ-009 -- Unplanned itineraries outside assigned territories (P1)

-   **Answer:** Not allowed. ASMs operate only within assigned areas.
-   **Status:** Answered.
-   **Implication:** Validate farm/territory selection against the ASM's
    assignments for the relevant date, including unplanned visits.
-   **Related:** CQ-006, CQ-007, CQ-012, CQ-046.

#### CQ-010 -- Recurring itineraries (P2)

-   **Answer:** ASM defines their own recurring plans.
-   **Status:** Partially answered.
-   **Open:** Recurrence patterns, start/end dates, generation horizon,
    approval of generated plans, editing/pausing, public holidays and
    already-approved dates.
-   **Related:** CQ-008, CQ-011, CQ-012.

#### CQ-011 -- Itinerary approval, rejection, amendment and cancellation (P1)

-   **Answer:** SM approves itineraries.
-   **Status:** Partially answered.
-   **Confirmed:** Approval authority is the SM; route to the ASM's
    assigned SM under CQ-031.
-   **Open:** Rejection reason; correction/resubmission;
    amendment/cancellation permissions; re-approval after amendment;
    deadlines; self-approval; treatment of itineraries created by
    Admin/SM.
-   **Related:** CQ-001, CQ-008, CQ-010, CQ-031, CQ-032, CQ-035.

#### CQ-012 -- Is an approved itinerary required before a visit? (P1)

-   **Answer:** Unplanned visits are allowed with post-visit approval.
-   **Status:** Partially answered.
-   **Open:** Who submits/approves the post-visit request; approval
    deadline; reason/evidence; rejection consequences; reporting label;
    treatment of planned but unvisited farms.
-   **Critical dependency:** Reconcile with CQ-013. Define whether
    post-visit approval applies to an unplanned-visit
    exception/authorisation rather than the submitted visit record.
-   **Related:** CQ-008, CQ-009, CQ-011, CQ-013, CQ-031, CQ-035.

### Visits, questionnaires and batches

#### CQ-013 -- Visit approval and rejection (P1)

-   **Answer:** No general visit approval. Visit submission is final.
-   **Status:** Answered, subject to the CQ-012 distinction.
-   **Implication:** Do not implement a general SM approval/rejection
    status for submitted visits. A correction mechanism requires SRS
    support or approved change request.
-   **Related:** CQ-012, CQ-035, CQ-041.

#### CQ-014 -- Farm Monitor vs Farm Evaluation (P1)

-   **Answer:** Farm Monitoring only.
-   **Status:** Answered.
-   **Implication:** Do not implement a separate Farm Evaluation
    process, questionnaire or report without an approved change request.
    Review legacy Process fields for compatibility.
-   **Related:** CQ-015, CQ-016, CQ-019, CQ-033.

#### CQ-015 -- Fixed visit fields vs configurable questionnaire (P1)

-   **Answer:** All visit fields are fixed system fields.
-   **Status:** Partially answered.
-   **Confirmed:** Visit fields and their business meaning are
    system-defined, not arbitrary fields created by Admin.
-   **Open/dependency:** Define which fixed visit/batch/metric fields
    are separate from questionnaire questions. CQ-017--020 still require
    conditional questions, answer types, supplied forms and per-question
    Admin validation. Specify how answers relate to fixed visit data and
    calculated metrics.
-   **Related:** CQ-016, CQ-017, CQ-018, CQ-019, CQ-020, CQ-021, CQ-023.

#### CQ-016 -- Questionnaire versioning and historical responses (P1)

-   **Answer:** Create a new version on every published change.
-   **Status:** Partially answered.
-   **Confirmed:** Never overwrite a published questionnaire version in
    place.
-   **Open:** Simultaneously active versions; activation/retirement;
    offline/in-progress visit handling; editing/deleting answered
    questions in later versions; preservation/display of original
    question text/options; publishing permissions.
-   **Related:** CQ-015, CQ-017, CQ-018, CQ-019, CQ-020, CQ-024.

#### CQ-017 -- Conditional question logic (P2)

-   **Answer:** Yes, conditional questions are required.
-   **Status:** Answered.
-   **Open design detail:** Specify supported condition operators and
    evaluation rules if not defined in the approved SRS.
-   **Related:** CQ-015, CQ-016, CQ-018, CQ-020.

#### CQ-018 -- Supported answer types (P2)

-   **Answer:** Figma types: Multiple Choice, Single Choice, Dropdown,
    Checkbox, Text, Image, GPS, Rating, Number, Data Set and Signature.
-   **Status:** Answered at type-list level.
-   **Open design detail:** Validation/ranges/units, file constraints,
    GPS precision, rating scale, signature representation and repeating
    Data Set behaviour.
-   **Related:** CQ-015, CQ-016, CQ-017, CQ-020.

#### CQ-019 -- Existing questionnaires to replicate (P2)

-   **Answer:** Approved forms will be provided.
-   **Status:** Partially answered.
-   **Open action:** Obtain the forms; record owner/version; map each
    form to its applicable farm/visit context. Forms were not attached
    with this response.
-   **Related:** CQ-015, CQ-016, CQ-017, CQ-018, CQ-020.

#### CQ-020 -- Questionnaire mandatory rules and validation (P2)

-   **Answer:** Admin configures mandatory fields and validation per
    question.
-   **Status:** Answered at responsibility level.
-   **Open design detail:** Supported validation rule types/operators,
    default values, publish-time validation and impact on existing
    responses after rule changes.
-   **Related:** CQ-015, CQ-016, CQ-017, CQ-018.

#### CQ-021 -- Batch lifecycle and closure rules (P1)

-   **Answer:** One breed per batch.
-   **Status:** Partially answered.
-   **Confirmed:** Exactly one Breed per Batch.
-   **Open:** Who creates batches; batch code format; initial
    quantity/placement date; transfers/sales/culls; closure
    role/conditions/data; reopening; unclosed-batch alerts; multiple
    batches per visit.
-   **Implication:** Enforce one breed per batch. Do not infer other
    lifecycle rules from prototype mock-ups or unverified notes.
-   **Related:** CQ-015, CQ-022, CQ-023, CQ-024.

#### CQ-022 -- Farm Type (P1)

-   **Answer:** Farm Type is a farm master attribute only.
-   **Status:** Partially answered.
-   **Confirmed:** Farm Type is not separately selected per visit.
-   **Open:** Authoritative values; whether multiple types may be
    assigned to a farm; mapping of legacy Dairy/Mixed/Breeder labels;
    approval/history when type changes.
-   **Related:** CQ-015, CQ-021, CQ-023, CQ-043, CQ-044, CQ-045.

#### CQ-023 -- Calculation definitions (P1)

-   **Answer:** Use formulas defined in the SRS.
-   **Status:** Partially answered.
-   **Confirmed:** Do not use conflicting Figma or unverified formulas.
-   **Open:** Resolve any SRS ambiguity in inputs/units/rounding;
    feed-period interpretation; cumulative vs per-visit mortality;
    missing/zero values; recalculation after historical corrections.
-   **Related:** CQ-015, CQ-021, CQ-022, CQ-033.

### Offline synchronisation and GPS

#### CQ-024 -- Offline synchronisation and conflict resolution (P1)

-   **Answer:** Server wins in a synchronisation conflict.
-   **Status:** Partially answered.
-   **Confirmed:** Server version takes precedence over device version.
-   **Open:** Initial Sync download scope; Midday Sync critical updates;
    Automatic Sync triggers/interval; version/conflict detection;
    idempotency; retry/partial-failure behaviour; multi-device policy;
    client document numbering; user recovery/notification when a device
    update is rejected.
-   **Related:** CQ-016, CQ-021, CQ-025, CQ-026, CQ-027, CQ-028.

#### CQ-025 -- Day-End Sync and day lock (P2)

-   **Answer:** No day-end lock.
-   **Status:** Answered.
-   **Implication:** Do not lock a user, device or day's work after
    Day-End Sync.
-   **Open only if not specified in SRS:** completion criteria and
    whether Day-End Sync can run with no pending records.
-   **Related:** CQ-024, CQ-035.

#### CQ-026 -- GPS capture requirements (P2)

-   **Answer:** Capture GPS at visit start only.
-   **Status:** Partially answered.
-   **Confirmed:** No visit-end GPS/check-out coordinate is required by
    this decision.
-   **Open:** Accuracy threshold; unavailable/inaccurate GPS behaviour;
    allow-and-flag/manual reason; geofence; timestamp/accuracy metadata;
    GPS requirements for other entities.
-   **Related:** CQ-027, CQ-028, CQ-024.

#### CQ-027 -- Farm geolocation (P2)

-   **Answer:** GPS coordinates are required during mobile farm
    registration.
-   **Status:** Answered at capture-point level.
-   **Open design detail:** Coordinate accuracy, unavailable-GPS
    handling, later web correction, authorised editors and audit of
    changes.
-   **Implication:** Farm latitude/longitude is captured during mobile
    registration; this is separate from visit-start GPS.
-   **Related:** CQ-006, CQ-026, CQ-028, CQ-035.

### Farm, expense, notifications and reporting

#### CQ-028 -- New farm capture and approval rules (P2)

-   **Answer:** Mobile-created farms require SM approval.
-   **Status:** Partially answered.
-   **Confirmed:** Route to the relevant ASM's assigned SM.
-   **Open:** Mandatory fields; duplicate detection; rejection reason;
    correction/resubmission; web-created farm approval; Farm Code
    generation timing; number of owner contacts; approval notifications.
-   **Related:** CQ-001, CQ-006, CQ-007, CQ-027, CQ-031, CQ-032, CQ-035.

#### CQ-029 -- Dealer farms and dealer visits (P2)

-   **Answer:** One Main Farm can have unlimited Dealer Farms.
-   **Status:** Partially answered.
-   **Confirmed:** One-to-many Main Farm → Dealer Farms; no two-dealer
    maximum.
-   **Open:** Dealer visit fields; Dealer Name reference vs free text;
    Feed Volume unit; applicability of batch/Farm Type fields;
    dealer-farm territory/ASM assignment.
-   **Related:** CQ-006, CQ-007, CQ-022, CQ-034.

#### CQ-030 -- Expense rules (P2)

-   **Answer:** Approver can modify the variable expense amount.
-   **Status:** Partially answered.
-   **Confirmed:** Authorised approver may change submitted variable
    amount.
-   **Open:** Rejection reason; resubmission; cancellation; farm/visit
    linkage; predefined limits/criteria; image count; currency if not
    explicit in SRS.
-   **Approval dependency:** CQ-001/CQ-031 route approval to the ASM's
    assigned SM.
-   **Related:** CQ-001, CQ-031, CQ-032, CQ-035.

#### CQ-031 -- Approval routing across the hierarchy (P2)

-   **Answer:** Only the SM assigned to the ASM approves that ASM's
    submissions.
-   **Status:** Partially answered.
-   **Confirmed:** Not visible to all SMs by default.
-   **Open:** Admin approval on behalf; pending-item handling after
    reporting-line change; effective dates;
    reassignment/delegation/absence handling.
-   **Related:** CQ-001, CQ-007, CQ-011, CQ-012, CQ-028, CQ-030, CQ-046.

#### CQ-032 -- Notification scope and channels (P2)

-   **Answer:** In-app notifications only.
-   **Status:** Answered at channel level.
-   **Implication:** Exclude email and external channels unless approved
    by change request.
-   **Open:** Broadcast messages; event catalogue;
    upcoming-visit/unclosed-batch alerts; Admin pending-approval alerts;
    read/unread rules; retention.
-   **Related:** CQ-011, CQ-012, CQ-016, CQ-028, CQ-030, CQ-031.

#### CQ-033 -- Report catalogue, KPIs and export (P2)

-   **Answer:** SRS reports only.
-   **Status:** Answered at scope level.
-   **Implication:** Exclude reports/KPIs found only in Proposal, legacy
    database, Figma or unverified drafts.
-   **Open action:** Baseline exact SRS report names, columns, filters,
    grouping, calculations, audience and export/print formats. Do not
    infer export formats from prototype buttons.
-   **Related:** CQ-014, CQ-023, CQ-035, CQ-046.

#### CQ-034 -- Items not in SRS (P2)

-   **Answer:** None of the listed items are in scope; exclude all.
-   **Status:** Answered.
-   **Excluded:** Attendance; feedback/complaints; Company master/ERP
    code; device registration/per-device numbering; mobile maintenance
    functions; farmer visit report sharing; pop-up images; broader
    breeder-farm mapping feature.
-   **Boundary:** CQ-039 separately confirms a Breeder Farm
    supplier/master entity. Implement only that specific master
    requirement and its defined relationships, not the broader excluded
    feature.
-   **Related:** CQ-039 and any requirements previously referencing
    excluded items.

#### CQ-035 -- Audit scope and retention (P2)

-   **Answer:** Audit SRS-listed events only.
-   **Status:** Partially answered.
-   **Confirmed:** Do not expand to all data changes merely because
    Proposal/prototype says so.
-   **Open:** Confirm exact SRS event list; retention; audit-log
    permissions; whether sensitive-data access logging is included by
    SRS.
-   **Related:** CQ-004, CQ-013, CQ-024, CQ-028, CQ-030, CQ-031.

#### CQ-036 -- Reversible password storage (P2)

-   **Answer:** Reversible AES-256-GCM requested, subject to security
    approval.
-   **Status:** **Pending security approval -- implementation blocked.**
-   **Open:** Security approver; threat model; key
    generation/storage/rotation/recovery; encryption service boundaries;
    privileged access and audit; alignment with SRS "128-bit encryption"
    wording; formal approval of Password Encryption Design.
-   **Security note:** Reversible password storage is materially
    different from standard salted password hashing. This register
    records the requested business direction, not a security
    endorsement. Do not implement before review and approval.
-   **Related:** CQ-004, CQ-005, CQ-035 and security architecture
    decisions.

### User data, breeder supplier and visit lifecycle

#### CQ-037 -- User master attributes (P3)

-   **Answer:** SRS fields only.
-   **Status:** Answered.
-   **Implication:** Do not add Department, Designation, Company,
    State/Province, User Type or Group Name solely because they appear
    in Figma/legacy sources.
-   **Related:** CQ-002, CQ-003, CQ-038.

#### CQ-038 -- Role-based accounts vs persons (P2)

-   **Answer:** Transactions belong to the individual user.
-   **Status:** Partially answered.
-   **Confirmed:** Itineraries, visits and expenses retain the
    individual performer/submitter identity, not a reusable
    position-account identity.
-   **Open:** Transfer of Region/Territory assignments when a user
    leaves; replacement user's current access; historical access;
    deactivation/reassignment effective dates.
-   **Related:** CQ-007, CQ-031, CQ-046.

#### CQ-039 -- Breeder farm and breed mapping (P3)

-   **Answer:** Breeder Farm is a separate supplier/master entity.
-   **Status:** Partially answered.
-   **Confirmed:** Model as a distinct supplier/master entity, not as
    the monitored Main Farm/Dealer Farm classification.
-   **Open:** Fields/unique identifier; supplier-to-breed cardinality;
    whether a batch must reference a Breeder Farm; multiple suppliers
    per breed; history.
-   **Scope boundary:** Do not implement broader breeder-farm mapping
    excluded by CQ-034 unless required for this specific entity or
    separately approved.
-   **Related:** CQ-021, CQ-022, CQ-034.

#### CQ-040 -- Farm visit report sharing with farmers (P3)

-   **Answer:** No external sharing.
-   **Status:** Answered.
-   **Implication:** No SMS, email, public/secure links or farmer-facing
    report delivery. Reports remain internal to authorised application
    users.
-   **Related:** CQ-033, CQ-034.

#### CQ-041 -- Visit lifecycle details (P3)

-   **Answer:** Multiple visits per farm per day are allowed.
-   **Status:** Partially answered.
-   **Confirmed:** Do not enforce a one-visit-per-farm-per-day
    uniqueness constraint.
-   **Open:** Resume interrupted visits; edit saved visits before sync;
    start/end timestamps and duration; duplicate-submission protection;
    report distinction between multiple visits.
-   **Related:** CQ-013, CQ-024, CQ-026.

### Non-functional requirements and visit master data

#### CQ-042 -- Non-functional requirement clarifications (P3)

-   **Answer:** Keep SRS targets as written.
-   **Status:** Answered as baseline instruction.
-   **Implication:** Do not silently revise performance, platform,
    browser or test-coverage targets.
-   **Open:** Where an SRS target is ambiguous or not measurable
    (e.g. sizing assumptions behind concurrent-user targets), document
    the ambiguity and request clarification rather than changing the
    target.
-   **Related:** Approved SRS NFRs and infrastructure/test planning.

#### CQ-043 -- Farm management grading and mandatory images (P3)

-   **Answer:** Use Figma A/B/C grading.
-   **Status:** Partially answered.
-   **Confirmed:** Grade values A, B and C.
-   **Open:** Grade descriptions/definitions; when grading applies;
    applicable Farm Types; mandatory image Farm Types; minimum/maximum
    image count.
-   **Related:** CQ-022, CQ-033.

#### CQ-044 -- Housing type and water availability lists (P3)

-   **Answer:** Use SRS lists.
-   **Status:** Answered at source-selection level.
-   **Implication:** SRS housing and water lists are authoritative; do
    not substitute Figma/legacy lists where they differ.
-   **Open only if SRS is silent:** configurability, per-Farm-Type
    variation and handling of "Other"/free text.
-   **Related:** CQ-022, CQ-015.

#### CQ-045 -- Feed type master (P3)

-   **Answer:** Use SRS feed list.
-   **Status:** Answered at source-selection level.
-   **Implication:** Do not add MBM, MAM or prototype-only values unless
    present in approved SRS or approved change request.
-   **Open only if SRS is silent:** feed categorisation, Feed Form
    linkage and "Other" handling.
-   **Related:** CQ-015, CQ-022, CQ-023, CQ-033.

## 5. Remaining open actions

  --------------------------------------------------------------------------
  CQ                                  Outstanding action
  ----------------------------------- --------------------------------------
  CQ-005                              Retain evidence of SRS/Proposal
                                      approval, versions and dates.

  CQ-007                              Define assignment cardinality,
                                      overlap, Region/Territory relationship
                                      and assignment history.

  CQ-010                              Define recurrence patterns, generation
                                      horizon and approval of generated
                                      plans.

  CQ-011                              Define rejection, amendment,
                                      cancellation, deadlines and
                                      self-approval.

  CQ-012                              Define post-visit approval object,
                                      approver, reason, rejection and
                                      reporting; reconcile with CQ-013.

  CQ-015                              Define fixed visit fields versus
                                      configurable questionnaire questions.

  CQ-016                              Define active versions, offline
                                      compatibility, historical snapshots
                                      and publishing permissions.

  CQ-019                              Obtain approved questionnaire forms
                                      and record versions/owners.

  CQ-020                              Specify supported validation rules and
                                      publish-time validation.

  CQ-021                              Define batch creation, code, quantity
                                      movements, closure, reopening and
                                      alerts.

  CQ-022                              Confirm Farm Type values and legacy
                                      mapping.

  CQ-023                              Resolve SRS formula
                                      input/unit/rounding/recalculation
                                      ambiguities.

  CQ-024                              Specify sync triggers, retries,
                                      idempotency, partial failure and
                                      multi-device behaviour.

  CQ-026                              Define GPS accuracy, unavailable-GPS
                                      behaviour, geofence and timestamps.

  CQ-027                              Define farm coordinate
                                      correction/validation rules.

  CQ-028                              Define mobile farm fields, duplicate
                                      checks, rejection/resubmission and
                                      code generation.

  CQ-029                              Define Dealer Farm visit fields and
                                      relationships beyond Main-to-Dealer
                                      cardinality.

  CQ-030                              Define expense
                                      rejection/resubmission/cancellation,
                                      limits, evidence and linkage.

  CQ-031                              Define Admin delegation and pending
                                      approvals after reporting-line
                                      changes.

  CQ-032                              Define in-app notification events,
                                      broadcasts, read state and retention.

  CQ-033                              Extract exact SRS report catalogue and
                                      specifications.

  CQ-035                              Confirm SRS audit event list,
                                      retention and access.

  CQ-036                              Complete and approve security design
                                      before implementation.

  CQ-038                              Define assignment/access transfer when
                                      an individual user is replaced.

  CQ-039                              Define Breeder Farm fields and
                                      supplier-to-breed/batch relationships.

  CQ-041                              Define resume/edit-before-sync and
                                      visit timestamps/duration.

  CQ-043                              Define grade
                                      descriptions/applicability and image
                                      requirements.

  CQ-044                              Confirm list configurability only
                                      where SRS is silent.

  CQ-045                              Confirm feed categorisation/form
                                      linkage only where SRS is silent.

  CQ-046                              Define ASM/Admin visibility and
                                      historical access behaviour.
  --------------------------------------------------------------------------

## 6. Implementation guardrails

1.  Use Administrator, SM and ASM only; Supervisor is an ASM synonym.
2.  Use Province → District → Region → Territory.
3.  Support ASM assignment to Regions and Territories; do not assume
    exclusivity until CQ-007 is resolved.
4.  Use daily Territory itineraries with selected farms. ASMs operate
    only within assigned areas.
5.  ASM defines recurring plans; remaining recurrence rules stay open.
6.  SM approval is required for itineraries, mobile-created farms and
    expenses, routed to the ASM's assigned SM.
7.  Unplanned visits are allowed with post-visit approval; keep this
    distinct from the final submitted visit record until CQ-012/CQ-013
    are reconciled.
8.  Visit submission is final; no general visit approval workflow.
9.  Implement Farm Monitoring only, fixed system visit fields, and the
    separately confirmed questionnaire capabilities without conflating
    them.
10. Create a new questionnaire version for every published change.
11. Support conditional questions and the specified Figma answer-type
    list; obtain the approved forms.
12. Enforce one breed per batch; other batch lifecycle rules remain
    open.
13. Farm Type is a farm master attribute; use SRS formulas,
    housing/water lists and feed list.
14. Use server-wins conflict resolution and no Day-End lock.
15. Capture visit GPS at start only; capture farm GPS during mobile farm
    registration.
16. Allow one Main Farm to have unlimited Dealer Farms.
17. Approver may modify variable expense amounts.
18. In-app notifications only; SRS reports only; exclude all listed
    out-of-SRS features and external farmer report sharing.
19. Audit SRS-listed events only.
20. Reversible AES-256-GCM password storage remains blocked pending
    security approval.
21. Transactions belong to individual users. SM sees assigned ASMs only;
    other visibility details remain open.
22. Allow multiple visits to the same farm per day.
23. Preserve SRS non-functional targets; raise ambiguities rather than
    silently changing them.

## 7. Change control

Any answer that changes an approved SRS requirement or adds
functionality outside approved scope must be documented and approved
through project change control. Update this register and affected
Functional Requirements, workflow, UI mapping, role-permission,
traceability and database design documents together.

------------------------------------------------------------------------

**End of Business Clarification Register -- Version 0.2**
