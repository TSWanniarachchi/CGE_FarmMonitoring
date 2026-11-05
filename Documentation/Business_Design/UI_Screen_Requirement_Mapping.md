# CGE Farm Monitoring System -- UI Screen Requirement Mapping

  ----------------------------------- ----------------------------------------
  Document                            `UI_Screen_Requirement_Mapping.md`

  Version                             **0.2 -- Updated analysis draft**

  Date                                05-11-2026

  Status                              **Analysis only.** This mapping does not
                                      approve a UI design or change the SRS.
                                      No frontend code was changed.

  Inputs                              S5 Figma web prototype (24
                                      screens/components including 2
                                      placeholders); S6 Figma mobile prototype
                                      (15 screens); S1--S10 source register in
                                      `Functional_Requirements_Analysis.md`;
                                      recorded clarification decisions
                                      CQ-001--CQ-046.

  Related                             `Functional_Requirements_Analysis.md`,
                                      `Business_Workflow_Analysis.md`,
                                      `Role_Permission_Matrix.md`,
                                      `Business_Clarification_Register.md`

  Caveat                              S1 p.16 says mock-ups are illustrative
                                      and may not reflect the final design.
                                      Prototypes are client-side mocks with
                                      hard-coded data. API operations below
                                      are requirements-derived, not evidence
                                      of existing APIs.
  ----------------------------------- ----------------------------------------

## 1. Purpose and mapping rules

This document maps the available Figma screens to the business workflows
and functional requirements. It identifies prototype coverage, required
UI capabilities, access mismatches and missing screens.

-   **SRS requirement** takes precedence over prototype behaviour.
-   **Confirmed decision** means a recorded answer in CQ-001--CQ-046.
-   **Prototype-only** means a feature is visible in Figma but is not
    confirmed as an in-scope requirement.
-   **Missing** means the required workflow has no suitable screen or
    the prototype screen is only a placeholder.
-   The product scope is **Farm Monitoring only**. Farm Evaluation and
    other excluded functions must not be designed into this release.
-   Current roles are **Administrator, Sales Manager (SM), and Area
    Sales Manager (ASM)**. Supervisor is the same role as ASM. Analyst
    and Finance Analyst are deferred.
-   Web is the first delivery phase. Mobile is a later phase; the
    mapping records mobile requirements for the planned mobile
    application.

## 2. Web screens (S5)

Column notes: - **Prototype role** records the Figma menu/access
mapping, not an approved authorization rule. - **Required role**
reflects the current role decisions and SRS responsibilities. -
**Disposition** indicates whether the screen should be retained,
redesigned, excluded or confirmed.

  ----------------------------------------------------------------------------------------------------------------------------------------------------
  ID          Prototype screen          Module /    Required role        Required UI / API capability             Version 0.2 finding and disposition
                                        workflow                                                                  
  ----------- ------------------------- ----------- -------------------- ---------------------------------------- ------------------------------------
  UI-W-01     LoginScreen -- Farm       UM / WF-01  All roles            Authenticate; return user profile,       Retain. Enforce platform access and
              Monitoring System                                          platform access and effective            authentication server-side. Show
                                                                         permissions                              clear failed-login and
                                                                                                                  locked-account messages. Preserve S3
                                                                                                                  lockout and 30-minute timeout
                                                                                                                  requirements.

  UI-W-02     AdminDashboard -- System  RP / WF-09  Administrator        Display only SRS-defined                 Redesign. Prototype KPIs are not
              Administrator Dashboard                                    report/dashboard information and         approved; use SRS reports only
                                                                         authorized audit activity                (CQ-033). Remove unsupported KPI
                                                                                                                  cards and quick links.

  UI-W-03     ASMDashboard -- Area      RP / WF-04, **Sales Manager**    SM view of pending approvals and         Reassign the approval dashboard
              Sales Manager Dashboard   WF-08,      for                  assigned ASM activity; ASM access to     concept to SM. ASM must not approve
                                        WF-09       approval/oversight   authorized reports                       itineraries or expenses. SM sees
                                                    functions; ASM only                                           only assigned ASMs (CQ-046).
                                                    for SRS reports                                               

  UI-W-04     UserManagement -- System  UM / WF-01  Administrator        List/search users; create/update;        Retain and redesign. Remove extra
              User Management                                            activate/deactivate; assign the three    roles. Use only SRS user fields
                                                                         current roles; configure platform access (CQ-037). Do not expose password
                                                                         as specified                             reveal. Multiple-role policy needs
                                                                                                                  confirmation before implementing
                                                                                                                  combined roles.

  UI-W-05     UserAccountAssignment --  UM / AH /   Administrator        Manage user/account assignments only     Review carefully. Do not assume the
              User Account Management   WF-01,                           where supported by approved requirements prototype's
                                        WF-02                                                                     position-account/reassignment model
                                                                                                                  is a required feature. Transactions
                                                                                                                  belong to individual users (CQ-038);
                                                                                                                  this does not by itself establish a
                                                                                                                  separate account-assignment
                                                                                                                  workflow.

  UI-W-06     ReportingHierarchy --     AH / WF-02  Administrator; SM    Maintain/view SM--ASM reporting          Redesign. SM visibility is
              Reporting Hierarchy                   may view own         relationship and assignment scope        restricted to assigned ASMs
                                                    assigned ASMs                                                 (CQ-046). Approval routing must
                                                                                                                  resolve to the ASM's assigned SM.
                                                                                                                  Assignment cardinality, effective
                                                                                                                  dates and history remain open.

  UI-W-07     MenuManagement -- Menu    UM / WF-01  Administrator        Maintain menu metadata only if required  Retain only if aligned with S3
              Management                                                 by the approved security design          security schema. Ensure menu
                                                                                                                  routes/platform values correspond to
                                                                                                                  the actual application navigation.

  UI-W-08     RoleMenuAssignment --     UM / WF-01  Administrator        Configure role × menu ×                  Redesign. Current roles only.
              Menu & Role Access                                         View/Create/Edit/Delete/Approve/Export   Enforce permissions in backend/API,
              Configuration                                              permissions as supported by S3           not merely in the sidebar. Keep
                                                                                                                  data-scope checks separate from
                                                                                                                  function permissions.

  UI-W-09     CompanyMaster -- Company  XT          ---                  None established                         Exclude from current scope unless an
              Master                                                                                              SRS requirement is identified. Do
                                                                                                                  not add Company CRUD based solely on
                                                                                                                  prototype content.

  UI-W-10     AreaManagement -- Area /  AH / WF-02  Administrator; ASM   Maintain geographic hierarchy and ASM    Redesign using **Province → District
              Main Route / Sub Route                views assigned areas assignments                              → Region → Territory** (CQ-006).
                                                                                                                  Replace Area/Route labels. ASM is
                                                                                                                  assigned to Regions and Territories
                                                                                                                  (CQ-007). Do not assume the
                                                                                                                  prototype's auto-sequence or
                                                                                                                  many-to-many farm mapping.

  UI-W-11     FarmMaster -- Farm Master FM / WF-03  Administrator; SM as Search/view farms; maintain permitted    Redesign. Farm Type is a farm master
              Management                            permitted by SRS;    farm master data; manage Main Farm /     attribute, not visit-level (CQ-022).
                                                    ASM within assigned  Dealer Farm relationship                 Mobile-created farms require SM
                                                    scope                                                         approval (CQ-028, CQ-031). One Main
                                                                                                                  Farm may have unlimited Dealer Farms
                                                                                                                  (CQ-029). Include
                                                                                                                  Province/District/Region/Territory
                                                                                                                  and required farm GPS data where
                                                                                                                  specified. Exact CRUD by role
                                                                                                                  remains to be detailed.

  UI-W-12     FarmerProfile -- Farmer   FM / BM /   Administrator; SM    View farm profile, batches and           Retain as a detail view, but align
              Profile                   VC / WF-03, for assigned ASM     historical visits/questionnaire          fields with SRS. Breeder Farm is a
                                        WF-05,      data; ASM within     responses                                separate supplier/master entity
                                        WF-06       assigned scope                                                (CQ-039). Do not mix supplier
                                                                                                                  identity with Farm Type. Avoid
                                                                                                                  unsupported visit types, categories
                                                                                                                  and metrics.

  UI-W-13     BreederManagement --      MD          Administrator,       Maintain Breeder Farm supplier/master    Redesign. Breeder Farm is a separate
              Breeder Farm & Breed                  subject to SRS       records and breed reference data as      supplier/master entity (CQ-039). One
              Management                            master-data          supported by SRS                         breed per batch (CQ-021). Exact
                                                    permissions                                                   supplier fields and CRUD are still
                                                                                                                  to be confirmed from SRS.

  UI-W-14     StandardMaster -- Farm    MD          Administrator for    Maintain SRS-defined lookup lists        Redesign from SRS lists. Farm Type
              Type / Feed Form /                    configuration; ASM                                            belongs to Farm master (CQ-022). Use
              Housing / Water                       consumes published                                            SRS housing/water lists (CQ-044). Do
                                                    reference data                                                not use prototype seed values where
                                                                                                                  they conflict with SRS.

  UI-W-15     FeedExpenseMaster -- Feed MD / EX /   Administrator for    Maintain SRS feed list and expense       Redesign from SRS. Use SRS feed list
              Type / Expense Type       WF-08       reference-data       types/criteria                           (CQ-045). Expense approver may
                                                    maintenance; ASM                                              modify variable amount (CQ-030); do
                                                    uses applicable                                               not infer other expense rules from
                                                    lists                                                         the prototype.

  UI-W-16     QuestionnaireManagement   VQ / WF-07  Administrator        Configure fixed questionnaire            Major redesign required. Conditional
              -- Questionnaire Master /                                  definitions, questions, answer types,    questions required (CQ-017). Use
              Builder                                                    conditional logic, validation,           Figma answer types: Multiple/Single
                                                                         publish/version                          Choice, Dropdown, Checkbox, Text,
                                                                                                                  Image, GPS, Rating, Number, Data
                                                                                                                  Set, Signature (CQ-018). Admin
                                                                                                                  configures mandatory/validation per
                                                                                                                  question (CQ-020). Every publish
                                                                                                                  creates a new version (CQ-016).
                                                                                                                  Approved forms will be supplied
                                                                                                                  (CQ-019).

  UI-W-17     VisitPlanApproval --      IP / WF-04  **Sales Manager**    SM reviews/approves assigned ASM         Redesign. SM approval confirmed
              Visit Schedule & Approval             approves; ASM        itinerary; ASM defines and submits       (CQ-011). Daily plan is one
                                                    creates own plan     daily/recurring plans through the        Territory with selected farms
                                                                         appropriate workflow                     (CQ-008). ASM defines recurring
                                                                                                                  plans (CQ-010). Remove ASM
                                                                                                                  self-approval. Do not use weekly
                                                                                                                  sub-route grid as the governing
                                                                                                                  rule.

  UI-W-18     ExpenseApproval --        EX / WF-08  **Sales Manager**    List assigned ASM expenses; inspect      Redesign. Only assigned ASM's SM
              Expense Approval                      approves assigned    evidence; approve/reject; modify         approves (CQ-031). Approver may
                                                    ASM expenses         variable amount where permitted          modify variable amount (CQ-030). Do
                                                                                                                  not grant ASM approval or
                                                                                                                  cross-hierarchy visibility. Exact
                                                                                                                  rejection/resubmission states remain
                                                                                                                  to be defined from SRS.

  UI-W-19     UserWiseExpenses -- User  EX / WF-08  ASM submits on       SM views assigned ASM expense            Redesign. Do not provide web
              Wise Expenses                         mobile; SM approves  submissions and approval status          expense-entry workflow for ASM
                                                    on web                                                        unless SRS explicitly requires it.
                                                                                                                  Transactions belong to individual
                                                                                                                  users (CQ-038).

  UI-W-20     FeedbackManagement --     XT          ---                  None established                         Exclude. Feedback is not in the
              Feedback Management                                                                                 current SRS scope (CQ-034).

  UI-W-21     NotificationManagement -- NT / WF-10  System-generated     In-app notification inbox/history for    Redesign. In-app only (CQ-032). Do
              Notification Management /             recipients: SM and   SRS-listed events                        not implement broadcast or response
              NotificationBell                      ASM as specified in                                           workflows unless explicitly listed
                                                    SRS                                                           in SRS. SM receives relevant
                                                                                                                  pending-approval notifications; ASM
                                                                                                                  receives approval outcomes.

  UI-W-22     Reports -- Reports &      RP / WF-09  Administrator, SM,   Provide only SRS-listed reports, with    Redesign. SRS reports only (CQ-033).
              Analytics                             ASM according to SRS role-appropriate filters and data scope  SM data limited to assigned ASMs;
                                                    role/data scope                                               ASM reports restricted to permitted
                                                                                                                  scope. Remove prototype-only report
                                                                                                                  menus and unsupported KPIs.

  UI-W-23     AuditLogs -- Audit Logs   AU / WF-10  Administrator; other View SRS-defined audit events and fields Retain only SRS-listed events
                                                    access only if SRS                                            (CQ-035). Do not add login/logout or
                                                    specifies                                                     other events unless SRS requires
                                                                                                                  them.

  UI-W-24     SettingsConfiguration --  UM / OF /   Administrator        Provide only settings required by SRS    Redesign or exclude unsupported
              Settings & Configuration  WF-11                                                                     device configuration, IMEI and
                                                                                                                  numbering features. Do not treat
                                                                                                                  prototype settings as requirements.

  UI-W-25     Placeholder -- Approve    FM / WF-03  **Assigned SM**      List/view mobile-created farm            Required screen. Mobile farm
              New Farm                                                   submissions; approve/reject according to registration requires SM approval
                                                                         approved workflow                        (CQ-028); approver is the ASM's
                                                                                                                  assigned SM (CQ-031). Define
                                                                                                                  rejection outcome/message from SRS;
                                                                                                                  do not invent extra approval roles.

  UI-W-26     Placeholder -- Visit      IP          ---                  None                                     Exclude. Visit submission is final
              Amendment                                                                                           (CQ-013). No post-submission visit
                                                                                                                  amendment workflow.
  ----------------------------------------------------------------------------------------------------------------------------------------------------

## 3. Mobile screens (S6)

All mobile field-operation screens map to the **ASM** role. The
prototype's `supervisor` auth label is treated as ASM, not as a separate
role (CQ-003). Mobile is a later delivery phase; this section remains a
requirements/design reference.

  ---------------------------------------------------------------------------------------------------
  ID             Prototype screen         Module /       Required UI / API      Version 0.2 finding
                                          workflow       capability             and disposition
  -------------- ------------------------ -------------- ---------------------- ---------------------
  UI-M-01        LoginScreen -- Welcome   UM / WF-01     Authenticate; enforce  Retain. Show useful
                 Back                                    mobile platform        validation and
                                                         access; return profile authentication
                                                         and permissions        errors. Apply
                                                                                security requirements
                                                                                from S3.

  UI-M-02        DashboardScreen --       RP / EX /      Show today's itinerary Redesign. Do not
                 Dashboard (+ Add         WF-05, WF-08,  and SRS-authorized     display unapproved
                 Expense)                 WF-09          summary; provide       KPIs. Expense
                                                         expense submission     transaction belongs
                                                         entry point if         to the individual ASM
                                                         supported              (CQ-038). Add expense
                                                                                evidence capture only
                                                                                as required by SRS.

  UI-M-03        FarmsScreen -- Farm      FM / WF-03     List farms within      Retain and enforce
                 Management                              ASM's assigned         assigned-area
                                                         Regions/Territories;   filtering (CQ-009).
                                                         open farm details      

  UI-M-04        FarmDetailsScreen --     FM / BM / VC / View farm master,      Redesign. One breed
                 Farm Info / Batches /    WF-03, WF-05,  batches and visit      per batch (CQ-021).
                 Visits                   WF-06          history; create/update Farm Type is a farm
                                                         batch only as allowed  master attribute
                                                         by SRS                 (CQ-022). Use SRS
                                                                                fields and lists;
                                                                                avoid conflating
                                                                                fixed visit fields
                                                                                with questionnaire
                                                                                responses.

  UI-M-05        NewFarmDiscoveryScreen   FM / WF-03     Register new farm;     Required and
                 -- New Farm Discoveries                 capture required farm  redesign. Farm GPS is
                                                         details and GPS;       mandatory during
                                                         submit for SM          mobile registration
                                                         approval; view         (CQ-027).
                                                         submission status      Mobile-created farms
                                                                                require approval by
                                                                                assigned SM (CQ-028,
                                                                                CQ-031). Main Farm
                                                                                can have unlimited
                                                                                Dealer Farms
                                                                                (CQ-029).

  UI-M-06        VisitPlanningScreen --   IP / WF-04     Create/select          Rework or merge into
                 Plan Visit (orphan)                     itinerary for the day  the final itinerary
                                                                                experience. Daily
                                                                                itinerary is one
                                                                                Territory with
                                                                                selected farms
                                                                                (CQ-008).

  UI-M-07        VisitPlanScreen -- Visit IP / WF-04     ASM creates daily and  Major redesign.
                 Plan                                    recurring plans;       Include Territory and
                                                         submit for SM          selected farms. ASM
                                                         approval; view         defines recurring
                                                         approval outcome       plans; SM approves
                                                                                (CQ-010, CQ-011). No
                                                                                editing of a
                                                                                submitted visit is
                                                                                implied.

  UI-M-08        VisitsScreen -- Farm     VC / WF-05     Show today's planned   Redesign. Multiple
                 Visits                                  farms and allow        visits to one farm
                                                         permitted unplanned    per day are allowed
                                                         visits within assigned (CQ-041). Unplanned
                                                         area                   visits are allowed
                                                                                with post-visit
                                                                                approval (CQ-012); do
                                                                                not permit visits
                                                                                outside assigned
                                                                                areas (CQ-009).

  UI-M-09        VisitExecutionScreen --  VC / BM / VQ / Capture fixed visit    Major redesign. Fixed
                 Farm Visit               WF-05, WF-06,  fields and render      visit fields remain
                                          WF-07          published              fixed system fields
                                                         questionnaire version  (CQ-015), while
                                                         with conditional       questionnaire
                                                         questions; capture     questions are
                                                         visit-start GPS;       configurable. Use
                                                         submit final visit     approved answer types
                                                                                and conditional logic
                                                                                (CQ-017--CQ-020).
                                                                                Visit GPS at start
                                                                                only (CQ-026).
                                                                                Submission is final
                                                                                (CQ-013). Use SRS
                                                                                formulas (CQ-023),
                                                                                Figma A/B/C grading
                                                                                (CQ-043), SRS feed
                                                                                list (CQ-045), and
                                                                                SRS lists (CQ-044).

  UI-M-10        ExpensesScreen --        EX / WF-08     Create and submit own  Retain/rework if this
                 Expenses (orphan)                       expense; view own      is the mobile expense
                                                         status/outcome         entry point. Remove
                                                                                unsupported
                                                                                statuses/fields.
                                                                                Expense approval
                                                                                belongs to assigned
                                                                                SM; approver may
                                                                                modify variable
                                                                                amount (CQ-030,
                                                                                CQ-031).

  UI-M-11        FeedbackScreen --        XT             None                   Exclude. Not in SRS
                 Feedback                                                       scope (CQ-034).

  UI-M-12        NotificationsScreen --   NT / WF-10     View own in-app        Redesign to
                 Notifications                           notifications and      SRS-listed events
                                                         approval outcomes      only (CQ-032,
                                                                                CQ-035). No broadcast
                                                                                response workflow
                                                                                unless SRS requires
                                                                                it.

  UI-M-13        SyncScreen -- Data       OF / WF-11     Initial / scheduled    Redesign according to
                 Synchronization                         synchronization as     SRS. Server wins on
                                                         defined in SRS;        conflict (CQ-024). No
                                                         display sync state and Day-End lock
                                                         errors                 (CQ-025). Do not
                                                                                invent additional
                                                                                sync modes or
                                                                                conflict-resolution
                                                                                choices.

  UI-M-14        MaintenanceScreen --     OF / WF-11     Only maintenance       Exclude unsupported
                 Maintenance                             actions required by    bug-report, database
                                                         SRS                    deletion/upload or
                                                                                device administration
                                                                                features unless SRS
                                                                                explicitly requires
                                                                                them.

  UI-M-15        ProfileScreen -- Profile UM             View own SRS-defined   Retain profile view
                 (+ Mark Attendance)                     user profile           with SRS fields only
                                                                                (CQ-037). Exclude
                                                                                attendance (CQ-034).
  ---------------------------------------------------------------------------------------------------

## 4. Required screens and workflows not adequately represented

  ------------------------------------------------------------------------------------------------------
              \# Required screen /     Platform    Role            Related          Version 0.2 status
                 workflow                                          requirement /    
                                                                   decision         
  -------------- --------------------- ----------- --------------- ---------------- --------------------
               1 New Farm Approval     Web         Assigned SM     WF-03; CQ-028,   Required; UI-W-25 is
                 queue and detail                                  CQ-031           only a placeholder.

               2 Province / District / Web         Administrator   WF-02; CQ-006    Required; prototype
                 Region / Territory                                                 uses Area/Route
                 master management                                                  terminology and does
                                                                                    not model the
                                                                                    confirmed hierarchy.

               3 ASM Region and        Web         Administrator / WF-02; CQ-007    Required; assignment
                 Territory assignment              authorized                       ownership and
                                                   assignment role                  cardinality need
                                                                                    detail.

               4 SM dashboard and      Web         SM              WF-04, WF-08,    Required; current
                 assigned-ASM approval                             WF-09; CQ-001,   dashboard is
                 inbox                                             CQ-046           incorrectly labelled
                                                                                    ASM.

               5 Daily itinerary with  Web /       ASM creates; SM WF-04; CQ-008,   Required; redesign
                 one Territory and     Mobile      approves        CQ-011           existing plan
                 selected farms                                                     screens.

               6 Recurring itinerary   Web /       ASM defines; SM WF-04; CQ-010,   Required; no
                 definition and        Mobile      approves        CQ-011           adequate prototype
                 approval                                                           screen.

               7 Unplanned visit with  Mobile /    ASM records;    WF-05; CQ-009,   Required; exception
                 post-visit approval   Web         assigned SM     CQ-012           approval workflow
                                                   approves                         needs detailed
                                                                                    design.

               8 Questionnaire builder Web         Administrator   WF-07;           Required; prototype
                 and publish/version                               CQ-016--CQ-020   builder is
                 management                                                         incomplete.

               9 Conditional           Mobile      ASM             WF-07; CQ-017,   Required; no dynamic
                 questionnaire                                     CQ-018           questionnaire
                 renderer                                                           renderer in
                                                                                    prototype.

              10 Expense evidence      Mobile /    ASM submits;    WF-08; CQ-030,   Verify exact
                 capture and review    Web         assigned SM     CQ-031           evidence
                                                   approves                         requirements against
                                                                                    SRS; approval amount
                                                                                    modification is
                                                                                    required.

              11 In-app                Web         SM              WF-10; CQ-032    Required if listed
                 pending-approval                                                   by SRS; current
                 notification inbox                                                 broadcast-oriented
                                                                                    screen does not meet
                                                                                    this need.

              12 SRS report catalogue  Web         Administrator / WF-09; CQ-033,   Required; exact
                 and report-specific               SM / ASM by     CQ-046           reports must be
                 screens                           scope                            taken from SRS only.

              13 Audit details with    Web         Administrator / WF-10; CQ-035    Retain only
                 SRS-defined fields                authorized role                  SRS-defined events
                                                                                    and fields.

              14 Batch create/update   Mobile      ASM             WF-06; CQ-021    Required; prototype
                 experience aligned                                                 screens conflict and
                 with                                                               need reconciliation.
                 one-breed-per-batch                                                
                 rule                                                               

              15 Farm GPS capture at   Mobile      ASM             WF-03, WF-05;    Required; capture
                 mobile registration                               CQ-026, CQ-027   points differ by
                 and visit-start GPS                                                workflow.
                 capture                                                            
  ------------------------------------------------------------------------------------------------------

## 5. Cross-screen design rules

### 5.1 Role and authorization

1.  Use only Administrator, SM and ASM as current roles.
2.  Treat Supervisor as ASM; do not expose a separate Supervisor role.
3.  Approval actions for itineraries, mobile-created farms and expenses
    belong to the ASM's assigned SM.
4.  SM visibility is limited to assigned ASMs (CQ-046).
5.  ASM access is limited to assigned Regions and Territories (CQ-009).
6.  Hiding a menu item is not authorization. Every protected API
    operation must validate role, platform and data scope.

### 5.2 Terminology and data consistency

-   Use **Itinerary**, not Visit Plan / Visit Schedule, where the SRS
    uses Itinerary.
-   Use **Province, District, Region, Territory**; do not use Area /
    Main Route / Sub Route as substitutes for the confirmed hierarchy.
-   Use **ASM** consistently for the field user.
-   Treat **Farm Type** as a farm master attribute.
-   Treat **Breeder Farm** as a separate supplier/master entity.
-   Use the SRS lists for housing, water and feed (CQ-044, CQ-045).
-   Use Figma A/B/C grading (CQ-043), but retain SRS grading
    definitions/values where the SRS provides them.
-   Use the SRS formulas (CQ-023).
-   Use LKR where currency is displayed; do not retain prototype `$`
    labels.

### 5.3 Visit and questionnaire behaviour

-   Fixed visit fields are system-defined; they are not dynamically
    added or removed by administrators.
-   Questionnaire questions are configurable by Administrator and can be
    conditional.
-   Published questionnaire changes create a new version.
-   Visit GPS is captured at visit start only.
-   Visit submission is final; do not add Visit Amendment or general
    visit approval.
-   Multiple visits per farm per day are permitted.

### 5.4 Excluded prototype features

Do not include these features in the current release solely because they
appear in S5/S6:

-   Farm Evaluation or Feedback.
-   Attendance marking.
-   Company Master or ERP integration not required by SRS.
-   Broadcast notifications and feedback-response workflows.
-   Visit Amendment.
-   Day-End lock.
-   Additional reports/KPIs beyond the SRS.
-   Device/IMEI administration, database deletion and bug-report
    functions unless explicitly required by SRS.
-   Separate Analyst, Finance Analyst, Supervisor or Regional Manager
    roles.

## 6. Open UI / UX design items

  -------------------------------------------------------------------------
  ID                      Open item               Required resolution
  ----------------------- ----------------------- -------------------------
  UI-OPEN-01              Exact screens and       Confirm the exact SRS
                          fields for user         user fields and whether
                          administration          multiple roles per user
                                                  are allowed
                                                  operationally.

  UI-OPEN-02              ASM assignment          Define assignment owner,
                          management              multiple Region/Territory
                                                  rules, effective dates
                                                  and history.

  UI-OPEN-03              SM dashboard content    Map only SRS reports and
                                                  approval queues; do not
                                                  invent KPIs.

  UI-OPEN-04              Daily itinerary states  Define draft, submitted,
                                                  approved, rejected and
                                                  resubmission behaviour
                                                  from SRS.

  UI-OPEN-05              Recurring itinerary     Define recurrence
                          controls                patterns, generation
                                                  horizon and how changes
                                                  are approved.

  UI-OPEN-06              Unplanned visit         Define required
                          exception approval      reason/evidence, status
                                                  transitions and rejection
                                                  handling.

  UI-OPEN-07              Fixed visit fields vs   Confirm screen grouping
                          questionnaire questions and data-entry order
                                                  without making fixed
                                                  fields configurable.

  UI-OPEN-08              Questionnaire version   Define effective date,
                          lifecycle               active version,
                                                  in-progress visit
                                                  behaviour and historical
                                                  rendering.

  UI-OPEN-09              Farm registration       Confirm rejection reason,
                          approval feedback       correction/resubmission
                                                  process and duplicate
                                                  handling from SRS.

  UI-OPEN-10              Expense evidence and    Confirm exact image
                          approval                requirements, allowed
                                                  file types/size and
                                                  rejection/resubmission
                                                  rules from SRS.

  UI-OPEN-11              Batch lifecycle screens Confirm closure fields
                                                  and lifecycle from SRS;
                                                  CQ-021 confirms only one
                                                  breed per batch.

  UI-OPEN-12              Report-by-report access Map each SRS report to
                                                  Administrator, SM and ASM
                                                  with the correct data
                                                  filters.

  UI-OPEN-13              Sync UI behaviour       Define status, retry and
                                                  error display consistent
                                                  with server-wins conflict
                                                  handling and no Day-End
                                                  lock.

  UI-OPEN-14              Exact audit fields      Use only the SRS event
                                                  list and fields; do not
                                                  infer additional audit
                                                  events.
  -------------------------------------------------------------------------

## 7. UI implementation acceptance checklist

-   [ ] Only current roles Administrator, SM and ASM appear in
    role-sensitive UI.
-   [ ] Approval dashboard is presented to SM, not ASM.
-   [ ] SM approval queues are restricted to assigned ASMs.
-   [ ] ASM farm, itinerary and visit screens are restricted to assigned
    Regions/Territories.
-   [ ] Geographic masters follow Province → District → Region →
    Territory.
-   [ ] Daily itinerary supports one Territory and selected farms.
-   [ ] Recurring itinerary is defined by ASM and approved by SM.
-   [ ] Mobile farm registration captures mandatory GPS and enters
    assigned-SM approval.
-   [ ] Farm Type is maintained at farm level.
-   [ ] Visit screen separates fixed system fields from configurable
    questionnaire questions.
-   [ ] Questionnaire renderer supports conditional questions and
    published versions.
-   [ ] Visit GPS is captured at start only.
-   [ ] Visit submission is final; no Visit Amendment screen is exposed.
-   [ ] Unplanned visits have a post-visit approval path without
    allowing out-of-area visits.
-   [ ] Expense approval allows the assigned SM to modify the variable
    amount.
-   [ ] Reports and notifications are limited to SRS scope.
-   [ ] Prototype-only and explicitly excluded features are removed.
-   [ ] Backend/API enforces role, platform and data-scope
    authorization.

## 8. Change history

  -----------------------------------------------------------------------
  Version                 Date                    Changes
  ----------------------- ----------------------- -----------------------
  0.1                     04-11-2026              Initial mapping of 26
                                                  web prototype
                                                  screens/components and
                                                  15 mobile screens;
                                                  identified missing
                                                  screens and prototype
                                                  deviations.

  **0.2**                 **05-11-2026**          Applied CQ decisions:
                                                  three current roles;
                                                  Supervisor = ASM; SM
                                                  approval and
                                                  assigned-ASM scope;
                                                  confirmed geographic
                                                  hierarchy;
                                                  daily/recurring
                                                  itinerary rules;
                                                  unplanned visits with
                                                  post-visit approval;
                                                  final visit submission;
                                                  questionnaire
                                                  conditional/version
                                                  rules; farm GPS rules;
                                                  one breed per batch;
                                                  SRS-only reports,
                                                  notifications and audit
                                                  events;
                                                  removed/excluded
                                                  out-of-scope prototype
                                                  features.
  -----------------------------------------------------------------------
