# CGE Farm Monitoring System -- Role & Permission Matrix

  ----------------------------------- ----------------------------------------------------------------
  Document                            `Role_Permission_Matrix.md`

  Version                             **0.2 -- Updated analysis draft**

  Date                                05-11-2026

  Status                              **Analysis only.** Updated using recorded clarification answers.
                                      This document does not replace the SRS or constitute formal
                                      stakeholder approval.

  Sources                             S1--S10 as defined in [Functional_Requirements_Analysis
                                      §1.1](Functional_Requirements_Analysis.md#11-source-register);
                                      recorded clarification decisions CQ-001--CQ-046.
  ----------------------------------- ----------------------------------------------------------------

## 1. Purpose and interpretation

This matrix describes the role responsibilities, function-level
permissions, platform access and data-visibility boundaries identified
for the CGE Farm Monitoring System.

-   **Confirmed** means the rule is supported by a recorded
    clarification answer.
-   **SRS** means the rule is explicitly described in the Software
    Requirements Specification.
-   **Pending** means the available sources and clarification answers do
    not define the rule sufficiently.
-   A permission in this document does not automatically grant access to
    every record. Data scope and reporting hierarchy restrictions must
    also be enforced.
-   This matrix is an analysis artefact. Any change to the signed SRS
    must follow the approved change-control process (CQ-004).

## 2. Confirmed roles

Only the following three roles are in scope at this stage.

  -----------------------------------------------------------------------------
  Role                Platform / access Confirmed             Status
                                        responsibilities      
  ------------------- ----------------- --------------------- -----------------
  **Administrator**   Web               System                Confirmed role
                                        administration, user  
                                        and role              
                                        administration,       
                                        master-data and       
                                        questionnaire         
                                        configuration,        
                                        system-level          
                                        administration, and   
                                        access to system      
                                        functions as defined  
                                        by the SRS.           

  **Sales Manager     Web               Supervises assigned   Confirmed role
  (SM)**                                ASMs; approves        and approval
                                        itineraries,          responsibility
                                        mobile-created farms  
                                        and expenses; can     
                                        view the work of      
                                        assigned ASMs within  
                                        the defined reporting 
                                        scope.                

  **Area Sales        Mobile for field  Defines recurring     Confirmed role
  Manager (ASM)**     operations; web   plans, prepares daily 
                      reporting access  itineraries, conducts 
                      as specified by   farm visits, records  
                      the SRS           visit/questionnaire   
                                        data, registers new   
                                        farms through mobile, 
                                        and submits expenses. 
                                        Operates within       
                                        assigned Regions and  
                                        Territories.          
  -----------------------------------------------------------------------------

### 2.1 Role names and exclusions

  -----------------------------------------------------------------------
  Name / role found in source         Version 0.2 treatment
  material                            
  ----------------------------------- -----------------------------------
  Supervisor                          Same role as **ASM**. It is not a
                                      separate system role.

  Field Supervisor                    Not a separate role; map to ASM
                                      only where the source context
                                      refers to the field user performing
                                      ASM duties.

  Regional Manager / Manager          Not included as a separate role.

  Regional Sales Manager /            Treat as the ASM's assigned **Sales
  Supervising SM                      Manager** for approval and
                                      reporting relationships.

  Analyst                             Deferred for later consideration;
                                      not part of the current role set.

  Finance Analyst                     Deferred for later consideration;
                                      not part of the current role set.
  -----------------------------------------------------------------------

**Role-count rule:** the current role set is Administrator, SM and ASM
only. Do not create extra roles based solely on prototype labels or
legacy terminology.

## 3. Permission legend

  -----------------------------------------------------------------------
  Code                                Meaning
  ----------------------------------- -----------------------------------
  **C**                               Create

  **V**                               View

  **U**                               Update / modify

  **D**                               Deactivate / delete, where
                                      supported by the function

  **A**                               Approve / reject

  **X**                               Export

  **---**                             No permission identified for this
                                      role in the current analysis

  **?**                               Not sufficiently defined; requires
                                      confirmation
  -----------------------------------------------------------------------

Platform: **W** = Web, **M** = Mobile.

A listed permission is subject to the data-scope restrictions in §5. Do
not interpret `—` as a newly approved prohibition if the SRS is silent;
it means that no access is established by the available evidence.

## 4. Permission matrix by function

  ----------------------------------------------------------------------------------------------------------------
            \# Function          Administrator   Sales Manager ASM               Platform   Decision / notes
  ------------ ----------------- --------------- ------------- ----------------- ---------- ----------------------
             1 User account      C V U D         ---           ---               W          Administrator role;
               administration                                                               detailed user fields
                                                                                            must remain within SRS
                                                                                            requirements.

             2 Role, menu and    C V U D         ---           ---               W          Administrator manages
               permission                                                                   role-based access.
               administration                                                               

             3 SM--ASM reporting C V U           V (assigned   V (own reporting  W          SM visibility is
               relationship                      ASMs)         manager)                     limited to assigned
                                                                                            ASMs (CQ-046).
                                                                                            Assignment maintenance
                                                                                            rules require detailed
                                                                                            definition.

             4 Province /        C V U D         ?             V (assigned       W          Hierarchy order
               District / Region                               areas)                       confirmed: Province →
               / Territory                                                                  District → Region →
               master data                                                                  Territory (CQ-006).

             5 ASM Region /      C V U D\*       ?             V (own            W          ASM is assigned to
               Territory                                       assignments)                 Regions and
               assignment                                                                   Territories (CQ-007).
                                                                                            Who performs
                                                                                            assignment maintenance
                                                                                            and whether SM can
                                                                                            assign remain to be
                                                                                            specified. `*`
                                                                                            Administrator access
                                                                                            is consistent with
                                                                                            system administration
                                                                                            but detailed
                                                                                            assignment authority
                                                                                            needs confirmation.

             6 Farm-related      C V U D         ?             V (required       W / M      Farm Type is a farm
               master data                                     mobile reference             master attribute, not
               (including Farm                                 data)                        a visit-level
               Type)                                                                        attribute (CQ-022).
                                                                                            Master-data ownership
                                                                                            by role needs
                                                                                            confirmation where not
                                                                                            stated in SRS.

             7 Feed, housing,    C V U D\*       ?             V (required       W / M      Use the lists and
               water, breed and                                mobile reference             definitions in the SRS
               grading reference                               data)                        (CQ-044, CQ-045,
               data                                                                         CQ-043). `*` Admin
                                                                                            maintenance is a
                                                                                            working interpretation
                                                                                            of system master-data
                                                                                            administration;
                                                                                            confirm detailed
                                                                                            ownership if needed.

             8 Farm master --    C V U D\*       V / U\*       V (assigned       W / M      Preserve the SRS role
               existing farm                                   areas)                       capabilities. Farm
               records                                                                      deactivation and exact
                                                                                            SM update scope are
                                                                                            not fully defined.

             9 New farm          V / A           V / A         C                 W / M      Mobile-created farms
               registration                                                                 require SM approval
               (mobile)                                                                     (CQ-028). Approver is
                                                                                            the SM assigned to the
                                                                                            ASM (CQ-031).

            10 New Farm Approval ---             A (assigned   ---               W          SM approval confirmed.
                                                 ASM's farms)                               Do not route to an
                                                                                            unrelated SM. Whether
                                                                                            Administrator is an
                                                                                            operational approver
                                                                                            is not established by
                                                                                            the clarification
                                                                                            decision; retain only
                                                                                            if explicitly required
                                                                                            by the signed SRS.

            11 Farm visit        V               V (assigned   V (per            W / M      SM sees only assigned
               history /                         ASMs)         assigned-area                ASMs (CQ-046). ASM
               historical visit                                scope)                       access is limited to
               data                                                                         assigned
                                                                                            Regions/Territories.

            12 Daily itinerary   V / U\*         V             C / U (own plan)  W / M      ASM plans daily work
               -- create and                                                                for one Territory with
               maintain draft                                                               selected farms
                                                                                            (CQ-008).

            13 Daily itinerary   V               A (assigned   C / V (own        W          SM approves
               -- approval                       ASM)          submission)                  itineraries (CQ-001,
                                                                                            CQ-011). Do not grant
                                                                                            ASM approval of own or
                                                                                            other users'
                                                                                            itineraries.

            14 Recurring         V / U\*         V / A         C / U (own        W          ASM defines recurring
               itinerary                                       recurring plan)              plans; SM approves
               definition                                                                   them (CQ-010, CQ-011).
                                                                                            Recurrence rules and
                                                                                            approval lifecycle
                                                                                            still need detail.

            15 Unplanned visit / V               A (post-visit C (record visit)  M          Unplanned visits are
               visit outside                     exception,                                 allowed with
               itinerary                         assigned ASM)                              post-visit approval
                                                                                            (CQ-012). ASM remains
                                                                                            restricted to assigned
                                                                                            areas (CQ-009). Exact
                                                                                            exception record and
                                                                                            workflow need
                                                                                            definition.

            16 Visit capture and V               V (assigned   C (submit final   M          Visit submission is
               questionnaire                     ASMs)         visit)                       final; no general
               response                                                                     visit approval and no
                                                                                            post-submission
                                                                                            editing (CQ-013).

            17 Visit             C V U D         ---           ---               W          Administrator
               questionnaire                                                                configures
               configuration                                                                questionnaires. Fixed
                                                                                            system visit fields
                                                                                            and configurable
                                                                                            questionnaire
                                                                                            questions must remain
                                                                                            distinct; conditional
                                                                                            questions are required
                                                                                            (CQ-015, CQ-017,
                                                                                            CQ-020).

            18 Questionnaire     C V U\*         V             V / use published W / M      Every published change
               version                                         version                      creates a new version
               publishing                                                                   (CQ-016).
                                                                                            Published-version
                                                                                            governance and
                                                                                            effective-date rules
                                                                                            need detail.

            19 Batch / Flock     V               V (assigned   C / U (during     M          One breed per batch
               records                           scope)        visit, subject to            (CQ-021). Full batch
                                                               SRS)                         lifecycle permissions
                                                                                            require further
                                                                                            specification.

            20 Expense type /    C V U D\*       ---           V                 W / M      SRS lists apply.
               expense reference                                                            Detailed maintenance
               data                                                                         ownership should be
                                                                                            confirmed if not
                                                                                            explicit in SRS.

            21 Expense entry     V               V (assigned   C (own expense)   M          Transactions belong to
                                                 ASMs)                                      the individual user
                                                                                            (CQ-038).

            22 Expense approval  V               A (assigned   ---               W          SM approves expenses
                                                 ASM)                                       for assigned ASMs
                                                                                            (CQ-001, CQ-031).
                                                                                            Approver may modify
                                                                                            the variable amount
                                                                                            (CQ-030).

            23 Reports and       V               V (assigned   V (SRS-authorized W          SRS reports only
               dashboards                        ASMs)         reports)                     (CQ-033). No
                                                                                            additional reports or
                                                                                            dashboard KPIs are
                                                                                            implied.

            24 Audit logs        V               ?             ---               W          Only audit events
                                                                                            listed in the SRS are
                                                                                            in scope (CQ-035). SM
                                                                                            access is not
                                                                                            established.

            25 System settings   C V U           ---           ---               W          Administrator-only
                                                                                            system administration
                                                                                            based on the SRS.

            26 In-app            V\*             V (pending    V (approval       W / M      In-app notifications
               notifications --                  approvals     outcomes relevant            only (CQ-032). Exact
               receive                           relevant to   to own                       Administrator
                                                 assigned      submissions)                 notification events
                                                 ASMs)                                      are subject to SRS
                                                                                            event definitions.

            27 In-app            ?               ---           ---               W          No general broadcast
               notifications --                                                             capability is
               broadcast /                                                                  confirmed. Do not
               author                                                                       implement
                                                                                            prototype-only
                                                                                            broadcast
                                                                                            functionality without
                                                                                            an SRS requirement.

            28 Mobile data       ---             ---           Execute           M          Server wins in
               synchronization                                                              synchronization
                                                                                            conflicts (CQ-024). No
                                                                                            Day-End lock (CQ-025).
                                                                                            Sync triggers and
                                                                                            retry behaviour must
                                                                                            follow the SRS.

            29 Farm GPS capture  V               V (assigned   C (at mobile farm M          Farm GPS is mandatory
                                                 scope)        registration /               during mobile farm
                                                               visit start as               registration (CQ-027).
                                                               applicable)                  Visit GPS is captured
                                                                                            at visit start only
                                                                                            (CQ-026).

            30 Password reveal / ---             ---           ---               W          Do not implement
               recoverable                                                                  password reveal.
               password viewing                                                             Security design must
                                                                                            use appropriate
                                                                                            password hashing;
                                                                                            CQ-036's reversible
                                                                                            encryption decision
                                                                                            applies to the
                                                                                            specified data
                                                                                            encryption
                                                                                            requirement, not
                                                                                            passwords.
  ----------------------------------------------------------------------------------------------------------------

`*` = role access inferred from the broad SRS description or
system-administration responsibility; exact operation-level scope must
be verified before implementation.

## 5. Data visibility and approval scope

  -----------------------------------------------------------------------
  Role                    Data visibility rule    Approval boundary
  ----------------------- ----------------------- -----------------------
  **Administrator**       System-wide access for  No blanket assumption
                          administration, subject that Administrator is a
                          to the SRS and security business approver for
                          controls.               every transaction.
                                                  Apply the confirmed SM
                                                  approval rule.

  **Sales Manager (SM)**  Can see only ASMs       Approves itineraries,
                          assigned to that SM     mobile-created farms
                          (CQ-046), and the       and expenses for
                          relevant operational    assigned ASMs (CQ-001,
                          data belonging to those CQ-031).
                          ASMs.                   

  **Area Sales Manager    Limited to assigned     Cannot approve own
  (ASM)**                 Regions and Territories itinerary, new farm
                          (CQ-009). Can work with registration or
                          selected farms in the   expense.
                          Territory assigned for  
                          the daily itinerary.    

  **All users**           Access must be checked  No cross-hierarchy
                          against role            approval should be
                          permissions and the     inferred.
                          user's permitted data   
                          scope.                  
  -----------------------------------------------------------------------

### 5.1 Geographic hierarchy

The confirmed hierarchy is:

`Province → District → Region → Territory`

ASM assignments are made against Regions and Territories. The detailed
assignment cardinality, overlapping assignments, effective dates and
history are not yet specified.

### 5.2 Itinerary and visit boundaries

-   Daily itinerary: one Territory per day, with selected farms.
-   ASM may plan within assigned geographic areas.
-   Unplanned visits are permitted with post-visit approval.
-   Visit submission is final. Do not add a general visit-approval stage
    or visit amendment capability.
-   Multiple visits to the same farm on the same day are allowed
    (CQ-041).

## 6. Web and mobile access

  -----------------------------------------------------------------------
  Role              Web               Mobile            Notes
  ----------------- ----------------- ----------------- -----------------
  Administrator     Yes               Not established   Administrative
                                      by current        functions are
                                      decisions         web-oriented in
                                                        the SRS.

  Sales Manager     Yes               Not established   Approval and
                                      by current        oversight
                                      decisions         functions.

  ASM               Yes --            Yes -- field      Do not copy
                    SRS-authorized    operations        prototype-only
                    reports only                        ASM web
                                                        permissions into
                                                        the product.
  -----------------------------------------------------------------------

The SRS and approved security schema define platform access controls.
Platform access must be checked on every protected request. A menu being
hidden is not sufficient authorization; the backend/API must enforce
permissions and data scope.

## 7. Permission model and enforcement

  -----------------------------------------------------------------------
  Area                                Required interpretation
  ----------------------------------- -----------------------------------
  Role set                            Administrator, SM, ASM only at this
                                      stage. Analyst and Finance Analyst
                                      are deferred.

  Role-to-function mapping            Use this matrix as the current
                                      analysis baseline, subject to
                                      unresolved items and SRS authority.

  Action permissions                  View, Create, Edit, Delete, Approve
                                      and Export are the action
                                      categories in the approved Module
                                      01 security baseline.

  Data scope                          Enforce separately from function
                                      permissions, including assigned
                                      ASM, reporting manager, Region and
                                      Territory.

  Approval scope                      Resolve the acting user's assigned
                                      reporting relationship before
                                      permitting approval.

  Platform access                     Check Web / Mobile access on each
                                      protected request.

  UI behaviour                        Hide or disable unavailable
                                      functions for usability, but always
                                      enforce access in the backend/API.

  Multiple roles                      The earlier source analysis
                                      describes combined permissions for
                                      users with multiple roles. The
                                      current business role assignment
                                      policy and conflict handling should
                                      be confirmed before relying on
                                      multi-role combinations.

  Delete vs deactivate                User accounts explicitly support
                                      activation/deactivation. Do not
                                      treat physical deletion and
                                      deactivation as interchangeable.
                                      Function-specific delete behaviour
                                      requires definition.
  -----------------------------------------------------------------------

## 8. Approval responsibility summary

  -----------------------------------------------------------------------
  Business item     Responsible actor Approver          Outcome / rule
  ----------------- ----------------- ----------------- -----------------
  Daily itinerary   ASM               Assigned SM       SM approval
                                                        required.

  Recurring         ASM defines       Assigned SM       SM approval
  itinerary                                             required;
                                                        recurrence
                                                        lifecycle details
                                                        pending.

  Mobile-created    ASM registers     Assigned SM       SM approval
  farm                                                  required before
                                                        the farm is
                                                        treated as
                                                        approved.

  Expense           ASM submits       Assigned SM       SM may modify the
                                                        variable amount
                                                        during approval.

  Unplanned visit   ASM records       Assigned SM       Post-visit
                                                        approval
                                                        required. Define
                                                        as an exception
                                                        approval, not a
                                                        visit-editing
                                                        workflow.

  Visit             ASM               No general        Submission is
  questionnaire                       approver          final; no general
  submission                                            visit approval.
  -----------------------------------------------------------------------

## 9. Out of scope / do not infer

The following must not be added to the permission model unless
introduced through the approved SRS/change-control process:

-   Separate Supervisor, Field Supervisor or Regional Manager roles.
-   Analyst and Finance Analyst roles in the current release.
-   Farm Evaluation module.
-   General visit approval or visit amendment after final submission.
-   Day-End lock.
-   External sharing with farmers.
-   Broadcast notifications not listed in the SRS.
-   Reports or dashboard KPIs beyond the SRS.
-   Prototype-only ASM web permissions.
-   Permissions or data fields not supported by the SRS.

## 10. Open items requiring detailed design

  -----------------------------------------------------------------------
  ID                      Open item               Why it remains open
  ----------------------- ----------------------- -----------------------
  RPM-OPEN-01             Exact Administrator, SM Clarifications identify
                          and ASM CRUD            lists and role
                          permissions for every   responsibilities but do
                          master-data entity      not define every
                                                  maintenance action.

  RPM-OPEN-02             Who creates and         Assignment targets are
                          maintains ASM-to-Region confirmed; assignment
                          / Territory assignments ownership and history
                                                  are not.

  RPM-OPEN-03             Whether an ASM can be   Cardinality and
                          assigned to multiple    effective-date rules
                          SMs, Regions or         are not defined.
                          Territories             
                          concurrently            

  RPM-OPEN-04             Exact SM visibility for SM scope is limited to
                          farm, visit, batch and  assigned ASMs, but
                          expense records when    record-level edge cases
                          multiple ASMs are       need specification.
                          involved                

  RPM-OPEN-05             Exact exception         Post-visit approval is
                          approval workflow for   confirmed; status
                          unplanned visits        transitions, required
                                                  evidence and rejection
                                                  handling are not.

  RPM-OPEN-06             Recurring itinerary     ASM defines and SM
                          patterns, generation    approves; operational
                          horizon and approval    details remain open.
                          lifecycle               

  RPM-OPEN-07             Boundary between fixed  Both are required;
                          visit fields and        their data model and UI
                          configurable            separation need
                          questionnaire questions detailed design.

  RPM-OPEN-08             Questionnaire           New version on every
                          publishing, effective   publish is confirmed;
                          dates and treatment of  version activation
                          in-progress visits      rules remain open.

  RPM-OPEN-09             Batch lifecycle         Breed cardinality is
                          permissions beyond one  confirmed;
                          breed per batch         create/update/close
                                                  permissions and
                                                  lifecycle are not fully
                                                  defined.

  RPM-OPEN-10             Exact approval          Approval responsibility
                          rejection and           is known; complete
                          resubmission rules for  status transitions are
                          itineraries, farms and  not.
                          expenses                

  RPM-OPEN-11             Which audit-log events  Only SRS-listed events
                          each role can view      are in scope; role
                                                  visibility is not fully
                                                  specified.

  RPM-OPEN-12             Exact report-level      Only SRS reports are in
                          access and filters for  scope; report-by-report
                          each role               visibility must be
                                                  mapped.

  RPM-OPEN-13             Multiple roles per user Source design mentions
                          and permission-conflict combined permissions;
                          resolution              business assignment
                                                  policy is not
                                                  confirmed.

  RPM-OPEN-14             Exact encryption scope  CQ-036 is subject to
                          and security approval   security approval. It
                          for reversible          must not be applied to
                          AES-256-GCM             passwords.
  -----------------------------------------------------------------------

## 11. Implementation acceptance checks

Before treating this matrix as implementation-ready, verify that:

-   [ ] Only Administrator, SM and ASM are configured as current
    business roles.
-   [ ] Supervisor is represented as ASM, not as an additional role.
-   [ ] Analyst and Finance Analyst remain deferred.
-   [ ] SM approval is restricted to the assigned ASM relationship.
-   [ ] ASM data access is restricted to assigned Regions and
    Territories.
-   [ ] Daily itinerary is limited to one Territory and selected farms.
-   [ ] Unplanned visits use a post-visit approval path without making
    submitted visit data editable.
-   [ ] Visit submission is final and has no general visit-approval
    stage.
-   [ ] Farm registration from mobile requires SM approval.
-   [ ] Expense approval allows the approver to modify the variable
    amount.
-   [ ] Backend/API enforcement exists independently of menu visibility.
-   [ ] Prototype-only permissions and out-of-scope modules are
    excluded.
-   [ ] All remaining open items are resolved or explicitly accepted
    before detailed implementation.

## 12. Change history

  -----------------------------------------------------------------------
  Version                 Date                    Changes
  ----------------------- ----------------------- -----------------------
  0.1                     04-11-2026              Initial source-based
                                                  role and permission
                                                  analysis.

  **0.2**                 **05-11-2026**          Applied recorded
                                                  clarification
                                                  decisions: three
                                                  current roles;
                                                  Supervisor = ASM;
                                                  Analyst and Finance
                                                  Analyst deferred; SM
                                                  approves itinerary,
                                                  mobile-created farms
                                                  and expenses; approval
                                                  restricted to assigned
                                                  ASM's SM; geography and
                                                  ASM assignments
                                                  updated; unplanned
                                                  visits require
                                                  post-visit approval;
                                                  visit submission final;
                                                  no Farm Evaluation,
                                                  Day-End lock, external
                                                  sharing or non-SRS
                                                  reports/events. Added
                                                  detailed-design open
                                                  items.
  -----------------------------------------------------------------------
