# CGE Farm Monitoring System -- Requirements Traceability Matrix

  ----------------------------------- ---------------------------------------
  Document                            `Requirements_Traceability_Matrix.md`

  Version                             **0.2 -- Updated analysis draft**

  Date                                05-11-2026

  Status                              **Analysis only.** This matrix does not
                                      approve, amend or supersede the SRS.

  Primary source                      `Functional_Requirements_Analysis.md`
                                      (FR-IDs and source status)

  Cross-reference sources             `Business_Workflow_Analysis.md`,
                                      `Role_Permission_Matrix.md`,
                                      `UI_Screen_Requirement_Mapping.md`,
                                      `Business_Clarification_Register.md`,
                                      `Database_Gap_Analysis.md`
  ----------------------------------- ---------------------------------------

## 1. Purpose and traceability approach

This matrix provides a consolidated trace from each functional
requirement to its source, business workflow, UI screen mapping and
database analysis. It uses the FR identifiers already established in
`Functional_Requirements_Analysis.md`; it does not create new business
requirements.

-   **Source status** is copied from the Functional Requirements
    Analysis. It is not silently changed by later clarification answers.
-   **Clarification alignment** records the CQ decisions relevant to
    that module. A CQ answer may resolve an ambiguity, but it does not
    rewrite the source SRS.
-   **UI coverage** uses the screen IDs in
    `UI_Screen_Requirement_Mapping.md` version 0.2. `None` means no
    direct screen mapping is recorded; it does not automatically mean
    the requirement is unnecessary.
-   **Workflow mapping** uses WF-01 to WF-11 from
    `Business_Workflow_Analysis.md`.
-   **Database mapping** is represented by DBG references where
    identified. Database entities remain unapproved unless explicitly
    stated in the Module 01 security baseline.
-   Prototype-only and out-of-SRS items are not treated as approved
    requirements.

## 2. Traceability summary

-   Functional requirements traced: **119**.
-   Non-functional requirements: **12 (NFR-01--NFR-12)**, summarized in
    §5.
-   Outside-SRS items: **7 (FR-XT-01--FR-XT-07)**, retained as
    scope-control references in the source analysis, not counted as
    approved functional requirements.

  --------------------------------------------------------------------------------
  Module           Requirement IDs                          Count Workflow
  ---------------- ------------------------ --------------------- ----------------
  User Management  `FR-UM-01`--`FR-UM-13`                      13 WF-01
  (UM)                                                            

  Area & Hierarchy `FR-AH-01`--`FR-AH-06`                       6 WF-02
  (AH)                                                            

  Farm Management  `FR-FM-01`--`FR-FM-12`                      12 WF-03
  (FM)                                                            

  Master Data (MD) `FR-MD-01`--`FR-MD-04`                       4 WF-02 / WF-07

  Itinerary        `FR-IP-01`--`FR-IP-10`                      10 WF-04
  Planning &                                                      
  Approval (IP)                                                   

  Visit            `FR-VQ-01`--`FR-VQ-08`                       8 WF-07
  Questionnaire                                                   
  (VQ)                                                            

  Visit Data       `FR-VC-01`--`FR-VC-19`                      19 WF-05
  Capture (VC)                                                    

  Batch Monitoring `FR-BM-01`--`FR-BM-09`                       9 WF-06
  & Calculations                                                  
  (BM)                                                            

  Expense          `FR-EX-01`--`FR-EX-09`                       9 WF-08
  Management (EX)                                                 

  Reporting &      `FR-RP-01`--`FR-RP-08`                       8 WF-09
  Analytics (RP)                                                  

  Audit & Logging  `FR-AU-01`--`FR-AU-06`                       6 WF-10
  (AU)                                                            

  Notifications &  `FR-NT-01`--`FR-NT-07`                       7 WF-10
  Approvals (NT)                                                  

  Offline          `FR-OF-01`--`FR-OF-08`                       8 WF-11
  Capability (OF)                                                 
  --------------------------------------------------------------------------------

## 3. Functional requirements traceability

### 3.1 Requirement-to-source, workflow, UI and database trace

Each row below is the corresponding requirement record from the
Functional Requirements Analysis. Requirement descriptions and source
statuses are retained; screen/workflow references are consolidated for
implementation planning.

  ------------------------------------------------------------------------------------------------------------------------------------------
  Requirement   Requirement (source      Actor             Workflow   UI mapping      Source reference          Source status
  ID            wording)                                                                                        
  ------------- ------------------------ ----------------- ---------- --------------- ------------------------- ----------------------------
  `FR-UM-01`    The system shall support All users         WF-01      UI-W-01,        S1 p.4; S3 Confirmed      Confirmed
                secure user                                           UI-M-01         Decisions                 
                authentication.                                                                                 

  `FR-UM-02`    The system shall enforce System            WF-01      UI-W-07,        S1 p.3, p.4; S3 Read Me   Confirmed
                Role-Based Access                                     UI-W-08         row 20                    
                Control so users access                                                                         
                only the functions and                                                                          
                information relevant to                                                                         
                their responsibilities.                                                                         

  `FR-UM-03`    A user may hold multiple Administrator     WF-01      UI-W-04         S1 p.4; S3                Confirmed
                roles; the system                                                                               
                combines (unions) the                                                                           
                permissions of all                                                                              
                assigned roles.                                                                                 

  `FR-UM-04`    The system shall support --                WF-01      UI-W-04,        S1 p.4                    **Needs Clarification.** S1
                the roles Administrator,                              UI-W-08                                   p.2 lists only ASM and
                Sales Manager and Area                                                                          Administrator as user
                Sales Manager.                                                                                  classes. S2 p.4--5 describes
                                                                                                                "Supervisors" doing visits
                                                                                                                and ASMs approving. S5 also
                                                                                                                seeds "Field Supervisor" and
                                                                                                                "Regional Manager". →
                                                                                                                CQ-001, CQ-002, CQ-003

  `FR-UM-05`    The Administrator shall  Administrator     WF-01      UI-W-04         S1 p.4                    Confirmed
                create, edit, activate,                                                                         
                deactivate and manage                                                                           
                user accounts.                                                                                  

  `FR-UM-06`    The system shall         Administrator     WF-01      UI-W-04         S1 p.5                    **Needs Clarification.**
                maintain user data:                                                                             Department is missing from
                Name, Role, Email,                                                                              S3. S5 adds Designation,
                Contact Number,                                                                                 Company, State, Type and
                Department, User Id,                                                                            Group Name. → CQ-037
                Active Status.                                                                                  

  `FR-UM-07`    The Administrator shall  Administrator     WF-01      All web         S1 p.4                    Confirmed
                have unrestricted access                                                                        
                to all system functions.                                                                        

  `FR-UM-08`    Platform access per      System            WF-01      UI-W-01,        S1 p.4--5; S3             Confirmed (S5 also lets the
                role: ASM uses                                        UI-M-01                                   ASM approve plans and
                **mobile** for                                                                                  expenses on web → CQ-001)
                operational functions                                                                           
                and **web** for reports                                                                         
                and dashboards; SM and                                                                          
                Administrator use the                                                                           
                web.                                                                                            

  `FR-UM-09`    Password policy (min 8   System /          WF-01      UI-W-01,        S3 Confirmed Decisions    Confirmed (S3)
                chars; upper, lower,     Administrator                UI-W-04                                   
                number, special),                                                                               
                lockout after 5 failures                                                                        
                (Admin unlock), 30-min                                                                          
                inactivity timeout, no                                                                          
                MFA, concurrent sessions                                                                        
                allowed, deactivation                                                                           
                revokes sessions.                                                                               

  `FR-UM-10`    Passwords are stored     Administrator     WF-01      UI-W-04         S3 Read Me rows 10--12,   **Needs Clarification.**
                with reversible AES-256                                               21; S9 AD-11              Recorded in S3 as a user
                encryption so an                                                                                decision, but S9 AD-11 is
                authorised Administrator                                                                        "design Pending Approval"
                can view them; every                                                                            and S3 itself flags it as
                reveal is audited.                                                                              high-risk. → CQ-036

  `FR-UM-11`    Login history:           System            WF-01      UI-W-23         S1 p.14; S3               Confirmed
                successful, failed and                                                                          
                logout events.                                                                                  

  `FR-UM-12`    Menu management and      Administrator     WF-01      UI-W-07,        S3 Confirmed Decisions;   Confirmed (S3). S5 shows
                role-to-menu permission                               UI-W-08         S4 item 5.0               only View/Add/Edit/Delete →
                assignment (View,                                                                               see Role_Permission_Matrix
                Create, Edit, Delete,                                                                           §5.
                Approve, Export).                                                                               

  `FR-UM-13`    The Administrator shall  Administrator     WF-01      UI-W-24         S1 p.4                    **Needs Clarification.** S1
                configure system                                                                                does not define which
                settings.                                                                                       settings exist. S5 shows
                                                                                                                device IMEI registration and
                                                                                                                per-device document
                                                                                                                numbering. → CQ-034

  `FR-AH-01`    Two-level reporting      Administrator     WF-02      UI-W-06         S1 p.6; S3                Confirmed
                hierarchy: multiple ASMs                                                                        
                report to a Sales                                                                               
                Manager.                                                                                        

  `FR-AH-02`    The Sales Manager has    Sales Manager     WF-02      UI-W-03,        S1 p.6; S2 p.6 ("Data     **Needs Clarification** →
                visibility of, and                                    UI-W-22         access limited to         CQ-046
                oversight over,                                                       assigned areas and        
                operations across all                                                 reporting hierarchy")     
                assigned ASMs.                                                                                  

  `FR-AH-03`    Farms are organised in a Administrator     WF-02      none in S5 (S5  S1 p.6                    **Needs Clarification.** The
                geographical hierarchy                                has Area → Main                           hierarchy differs across S1,
                of **Regions** and                                    Route → Sub                               S4, S5 and S7, and farms
                **Territories**; each                                 Route)                                    also carry Province and
                Region contains multiple                                                                        District (FR-FM-01). →
                Territories.                                                                                    CQ-006

  `FR-AH-04`    Each ASM is assigned to  Administrator     WF-02      UI-W-05 *\[A\]* S1 p.6; S3                **Needs Clarification.**
                one or more Regions and                                                                         Region-level (S1) vs
                manages the associated                                                                          Territory-level (S3)
                Territories within them.                                                                        assignment, and whether a
                                                                                                                Territory can have more than
                                                                                                                one ASM (S7). → CQ-007

  `FR-AH-05`    The system restricts     System            WF-02      UI-M-03,        S1 p.6; S2 p.6            **Needs Clarification.**
                ASMs to operate only                                  UI-M-07,                                  Conflicts with FR-IP-04
                within their assigned                                 UI-M-08                                   (unplanned itineraries
                Regions and                                                                                     "outside predefined
                corresponding                                                                                   territories"). → CQ-009
                Territories.                                                                                    

  `FR-AH-06`    Route levels (Main       Administrator     WF-02      UI-W-10         S4 item 8.0; S5; S6       **Needs Clarification.** Not
                Route, Sub Route) under                                                                         in S1. → CQ-006
                the area structure, with                                                                        
                farms mapped to Sub                                                                             
                Routes.                                                                                         

  `FR-FM-01`    Maintain farm master     Administrator,    WF-03      UI-W-11,        S1 p.6                    Confirmed (field list). S5
                data: Farm Code, Farm    Sales Manager                UI-W-12                                   has no Region, Territory,
                Name, Farm Address,                                                                             Farm Type or GPS fields → UI
                Owner Name, Owner                                                                               gaps.
                Contact Number, Dealer                                                                          
                Indicator (Is Dealer),                                                                          
                Province, District,                                                                             
                Region, Territory,                                                                              
                Active Status.                                                                                  

  `FR-FM-02`    If a farm is a dealer,   Administrator,    WF-03      UI-W-11         S1 p.6                    **Needs Clarification.** The
                the system shall allow   Sales Manager                                                          target of the mapping is
                associating and mapping                                                                         undefined. S7 says "Main
                the farm accordingly.                                                                           Farm 0--2 Dealer Farms"
                                                                                                                (unverified). S5 maps
                                                                                                                dealers to Area / Route /
                                                                                                                User. → CQ-029

  `FR-FM-03`    View farm visit history  Admin, SM, ASM    WF-03      UI-W-12,        S1 p.6                    Confirmed
                and access historical                                 UI-M-04                                   
                data for each farm.                                                                             

  `FR-FM-04`    Both web and mobile      ASM (mobile);     WF-03      UI-M-05,        S1 p.7                    Confirmed
                capture farm and farmer  Admin, SM (web)              UI-W-11                                   
                details when a new farm                                                                         
                or dealer farm is                                                                               
                identified.                                                                                     

  `FR-FM-05`    Any farm or dealer farm  Administrator,    WF-03      UI-W-25         S1 p.7                    Confirmed. Web-created farms
                created on mobile        Sales Manager                (placeholder in                           are implied not to need
                requires approval                                     S5)                                       approval *\[A\]* → CQ-028
                through the web **New                                                                           
                Farm Approval** function                                                                        
                by System Administrators                                                                        
                or Sales Managers.                                                                              

  `FR-FM-06`    Only after approval: the System            WF-03      UI-M-03,        S1 p.7                    Confirmed
                farm becomes active; the                              UI-M-08                                   
                assigned ASM can plan                                                                           
                and record visits on                                                                            
                mobile; visit data                                                                              
                capture for the farm is                                                                         
                enabled.                                                                                        

  `FR-FM-07`    Standalone system: no    --                WF-03      --              S1 p.7; S2 p.7 (no ERP    Confirmed (S5 Company Master
                integration with M3 for                                               integration)              carries an "ERP Code" →
                farm information.                                                                               CQ-034)

  `FR-FM-08`    Rejection of a new farm: Admin, SM, ASM    WF-03      UI-W-25,        S1 is silent. S6 shows    **Missing** → CQ-028
                reason, notification and                              UI-M-05         "Rejected" with no        
                resubmission.                                                         reason; S7 states         
                                                                                      "rejected submissions     
                                                                                      editable/resubmittable"   
                                                                                      (unverified)              

  `FR-FM-09`    Farm Type as a farm      --                WF-03      UI-W-14,        S1 p.9 selects Farm Type  **Needs Clarification** →
                attribute (Broiler /                                  UI-M-09         **per visit**; S6 lets    CQ-022
                Layer / Dairy / Mixed).                                               the ASM change the farm's 
                                                                                      type during a visit; S5   
                                                                                      farm form has no farm     
                                                                                      type                      

  `FR-FM-10`    Farm geolocation         --                WF-03      none            Not in S1 farm data. S2   **Missing** → CQ-027
                (latitude / longitude).                                               p.6 requires "map-based   
                                                                                      visualization of visits"; 
                                                                                      S7 states GPS required    
                                                                                      (unverified)              

  `FR-FM-11`    Multiple farm contacts   Admin, SM, ASM    WF-03      UI-W-11,        S4; S5; S6 (S1 has Owner  **Needs Clarification** →
                (name, job role,                                      UI-M-05         Name / Contact only)      CQ-028
                contact, email,                                                                                 
                address).                                                                                       

  `FR-FM-12`    Duplicate farm detection System            WF-03      UI-M-05         Not in S1. S6 performs a  **Missing** → CQ-028
                when new farms are                                                    local, overridable name / 
                captured.                                                             contact check             

  `FR-MD-01`    The Administrator        Administrator     WF-02 /    UI-W-14,        S1 p.4                    Confirmed (the list of
                creates, modifies and                      WF-07      UI-W-15,                                  masters is not defined in
                maintains master data.                                UI-W-13,                                  S1)
                                                                      UI-W-10                                   

  `FR-MD-02`    Configuration of feed    Administrator     WF-02 /    UI-W-14,        S2 p.5                    Confirmed (S2). The S1 visit
                types, water                               WF-07      UI-W-15                                   flow lists fixed values
                availability, housing                                                                           (FR-VC-07..10). S5/S6 lists
                types and grading                                                                               conflict → CQ-044, CQ-045
                options.                                                                                        

  `FR-MD-03`    Breed data management,   Administrator     WF-02 /    UI-W-13         S2 p.5, p.8; S1 p.9       **Needs Clarification.**
                and breeder farm--breed                    WF-07                      ("Select Breed")          Breeder-farm mapping is S2
                mapping.                                                                                        only. → CQ-039

  `FR-MD-04`    Province and District    Administrator     WF-02 /    none in S5      S1 p.6 (fields only)      **Needs Clarification**
                masters (farm                              WF-07                                                (relationship to Region /
                attributes).                                                                                    Territory) → CQ-006

  `FR-IP-01`    The mobile app allows    ASM               WF-04      UI-M-07         S1 p.7                    **Needs Clarification.**
                ASMs to plan their                                                                              Planning period: S1 daily;
                **daily** itineraries.                                                                          S5 weekly grid; S7 monthly
                                                                                                                (unverified); S6 daily per
                                                                                                                sub-route. → CQ-008

  `FR-IP-02`    An ASM may plan **one    ASM               WF-04      UI-M-07         S1 p.7                    **Needs Clarification.** S5
                territory per day**.                                                                            and S6 plan Sub Routes, not
                                                                                                                Territories. → CQ-008

  `FR-IP-03`    Within the selected      ASM               WF-04      UI-M-07 (S6 has S1 p.7                    Confirmed
                territory, the ASM can                                no farm                                   
                schedule multiple farm                                selection)                                
                visits.                                                                                         

  `FR-IP-04`    ASMs can create          ASM               WF-04      UI-M-08         S1 p.7; S2 p.4            **Needs Clarification.**
                **unplanned                                                           ("unplanned visit         Conflicts with FR-AH-05;
                itineraries** for visits                                              capture")                 unclear whether approval is
                outside predefined                                                                              needed. → CQ-009, CQ-012
                territories.                                                                                    

  `FR-IP-05`    The web app supports     Admin / SM        WF-04      none            S1 p.7                    **Needs Clarification.**
                generating predefined    *\[A\]*                                                                Actor, pattern and approval
                **recurring                                                                                     are not defined. → CQ-010
                itineraries**.                                                                                  

  `FR-IP-06`    System Administrators    Admin, SM         WF-04      UI-W-17         S1 p.4--5, p.7            Confirmed (S5 also lets the
                and Sales Managers can                                                                          ASM approve → CQ-001)
                create, modify, review                                                                          
                and approve itineraries                                                                         
                planned by ASMs on the                                                                          
                web.                                                                                            

  `FR-IP-07`    If an ASM has no planned Admin, SM         WF-04      UI-W-17         S1 p.7                    Confirmed (approval state of
                itinerary, Admin or SM                                                                          an assigned itinerary not
                can create and assign                                                                           stated → CQ-011)
                one on the web.                                                                                 

  `FR-IP-08`    Rejection of an          Admin, SM         WF-04      UI-W-17 (S5 has S1 is silent (S6 shows    **Missing** → CQ-011
                itinerary: reason,                                    no reject)      "Rejected"; S7 "rejected  
                notification and                                                      plan requires new plan",  
                resubmission.                                                         unverified)               

  `FR-IP-09`    Amendment or             ASM, Admin, SM    WF-04      UI-W-26         S4 item 9.0 ("Amend       **Missing** → CQ-011
                cancellation of an                                    (placeholder)   current date Visit        
                approved itinerary.                                                   Plan"); S5 placeholder;   
                                                                                      S1 silent                 

  `FR-IP-10`    Relationship between an  System            WF-04      UI-M-08         S1 p.7 is silent          **Missing** → CQ-012
                approved itinerary and                                                                          
                visit capture (is                                                                               
                approval a                                                                                      
                precondition?).                                                                                 

  `FR-VQ-01`    Dynamic questionnaire    "Authorized users WF-07      UI-W-16         S1 p.8                    **Needs Clarification.** S1
                management in the web    (e.g., System                                                          p.4 gives questionnaires to
                app: define, configure   Administrators or                                                      the Administrator only; the
                and modify               Sales Managers)"                                                       S1 p.5 SM list omits them. →
                questionnaires.                                                                                 CQ-020

  `FR-VQ-02`    Questions can be         Admin (SM?)       WF-07      UI-W-16 (S5 has S1 p.8                    **Needs Clarification** →
                customised by farm type,                              no assignment)                            CQ-020
                operational need or                                                                             
                data-collection                                                                                 
                objective.                                                                                      

  `FR-VQ-03`    Questionnaires can be    Admin             WF-07      UI-W-16         S1 p.8                    Confirmed (effect on history
                updated without system                                                                          → FR-VQ-08)
                changes or redeployment.                                                                        

  `FR-VQ-04`    Questionnaires defined   ASM               WF-07      UI-M-09         S1 p.8                    Confirmed (S6 capture screen
                on web are available on                                                                         has no questionnaire engine
                mobile; ASMs must                                                                               → UI gap)
                complete the relevant                                                                           
                questionnaire during                                                                            
                each farm visit.                                                                                

  `FR-VQ-05`    Questions may be         System            WF-07      UI-W-16 (no     S1 p.8                    **Needs Clarification.**
                logically linked by                                   conditional                               S7/S8 say conditional
                conditional                                           logic in S5)                              visibility was "rejected"
                dependencies; responses                                                                         (unverified). → CQ-017
                drive the display and                                                                           
                flow of later questions.                                                                        

  `FR-VQ-06`    Question attributes:     Admin             WF-07      UI-W-16         S1 p.8                    **Needs Clarification.** S5
                Mandatory/Optional;                                                                             offers 11 answer types (adds
                Answer Type (Multiple                                                                           Number, Rating, GPS Log,
                Choice -- checkbox;                                                                             Signature, Data Set Input).
                Single Choice -- radio                                                                          S1 has no numeric type
                or dropdown; Text; Image                                                                        although the visit flow
                capture/upload);                                                                                captures numbers. → CQ-018
                predefined answer                                                                               
                options; Previous Data                                                                          
                Reference (pre-fill /                                                                           
                display previous data).                                                                         

  `FR-VQ-07`    The system shall         PMS               WF-07      --              S1 p.9                    **Needs Clarification.**
                replicate all currently  (configuration)                                                        Source forms not supplied. →
                defined questionnaires.                                                                         CQ-019

  `FR-VQ-08`    Questionnaire versioning Admin / System    WF-07      UI-W-16 (S5:    Not in S1. S6             **Missing** → CQ-016
                and preservation of                                   edits in place, notification mentions an  
                historical responses.                                 hard delete,    "updated farm visit       
                                                                      "Duplicate"     questionnaire"; S7 states 
                                                                      only)           versioning (unverified)   

  `FR-VC-01`    During a visit, the      ASM               WF-05      UI-M-09         S1 p.9                    Confirmed
                system guides the user                                                                          
                through a structured                                                                            
                questionnaire based on                                                                          
                the selected visit type                                                                         
                and farm type.                                                                                  

  `FR-VC-02`    Visit Initialization:    ASM               WF-05      UI-M-09         S1 p.9                    Confirmed (S6 does not save
                select Visit Type                                                                               it → UI gap)
                **Farm** or **Dealer**.                                                                         

  `FR-VC-03`    If Dealer: enter Dealer  ASM               WF-05      UI-M-09         S1 p.9                    **Needs Clarification.**
                Name, select Used Feed                                                                          Unit of Feed Volume; whether
                Type, enter Feed Volume.                                                                        Dealer Name refers to a
                                                                                                                dealer farm record
                                                                                                                (FR-FM-02); whether batch /
                                                                                                                farm-type steps apply to
                                                                                                                dealer visits. → CQ-029

  `FR-VC-04`    The system captures the  System / ASM      WF-05      UI-M-09         S1 p.9; S2 p.4 ("GPS      Confirmed. Capture points
                GPS location of the                                                   location capture for each (start / end), accuracy,
                visit.                                                                farm visit")              validation and fallback are
                                                                                                                not defined → CQ-026

  `FR-VC-05`    Select Farm Type:        ASM               WF-05      UI-M-09         S1 p.9                    **Needs Clarification.**
                Broiler, Layer, Dairy,                                                                          Per-visit selection vs farm
                Mixed.                                                                                          attribute; S5 Farm Type
                                                                                                                master seeds Broiler / Layer
                                                                                                                / **Breeder**. → CQ-022

  `FR-VC-06`    Batch Selection /        ASM               WF-05      UI-M-09,        S1 p.9                    Confirmed (one or many
                Creation (all farm                                    UI-M-04                                   batches per visit not
                types): select an                                                                               stated; S6 allows several →
                existing **open** batch                                                                         CQ-021)
                or create a new one                                                                             
                (Batch Start Date,                                                                              
                Initial Batch Size,                                                                             
                Breed).                                                                                         

  `FR-VC-07`    Farm-type specific       ASM               WF-05      UI-M-09         S1 p.10                   **Needs Clarification.**
                capture. **Broiler:**                                                                           Broiler has no feed input
                Current Batch Size,                                                                             although FCR needs
                Average Body Weight                                                                             cumulative feed (FR-BM-04);
                (kg), Production (if                                                                            "Production (if applicable)"
                applicable), Cumulative                                                                         for Broiler is undefined. →
                Mortality Count.                                                                                CQ-023
                **Layer:** Current Batch                                                                        
                Size, Current Feed per                                                                          
                Animal (g), Production                                                                          
                (Egg Count), Cumulative                                                                         
                Mortality Count. **Dairy                                                                        
                / Mixed:** Current Batch                                                                        
                Size, Current Feed per                                                                          
                Animal (g), Cumulative                                                                          
                Mortality Count.                                                                                

  `FR-VC-08`    Common: Feed Type (Prima ASM               WF-05      UI-M-09         S1 p.10--11               Confirmed (S5/S6 lists
                / F/C, CIC, Anthonys,                                                                           differ → CQ-045)
                New Hope, Cosmo, Super,                                                                         
                Self, Other + specify)                                                                          
                and Feed Form (Mash,                                                                            
                Crumble, Pellet).                                                                               

  `FR-VC-09`    Housing Type: Closed,    ASM               WF-05      UI-M-09         S1 p.11                   Confirmed (S6 uses
                Open, Battery Cages,                                                                            per-farm-type lists →
                Other + specify.                                                                                CQ-044)

  `FR-VC-10`    Environmental Data:      ASM               WF-05      UI-M-09         S1 p.11                   **Needs Clarification.**
                enter Water                                                                                     Free entry (S1 "Enter") vs a
                Availability.                                                                                   configured list (S2 p.5;
                                                                                                                S5/S6 lists differ). →
                                                                                                                CQ-044

  `FR-VC-11`    Additional inputs: Farm  ASM               WF-05      UI-M-09         S1 p.11                   **Needs Clarification.** "If
                Management Grading (if                                                                          applicable" and the grading
                applicable); Complaints                                                                         scale are undefined (S6
                & Observations;                                                                                 A/B/C; S2 p.5 configurable
                Recommendations &                                                                               grading). → CQ-043
                Comments; capture and                                                                           
                upload Images.                                                                                  

  `FR-VC-12`    Image capture is         System            WF-05      UI-M-09         S2 p.5 only               **Needs Clarification**
                mandatory for selected                                                                          (which farm types) → CQ-043
                farm types.                                                                                     

  `FR-VC-13`    The questionnaire        System            WF-05      UI-M-09         S1 p.11                   Confirmed
                adjusts dynamically to                                                                          
                the selected Farm Type.                                                                         

  `FR-VC-14`    Mandatory validations    System            WF-05      UI-M-09         S1 p.11                   **Needs Clarification.** The
                are enforced where                                                                              validation rules themselves
                applicable.                                                                                     are not listed (see
                                                                                                                Business_Workflow_Analysis
                                                                                                                WF-05).

  `FR-VC-15`    Captured data is stored  System            WF-05      --              S1 p.11                   Confirmed
                and used for reporting                                                                          
                and analysis.                                                                                   

  `FR-VC-16`    Planned and unplanned    ASM               WF-05      UI-M-08         S1 p.7; S2 p.4, p.8       Confirmed (precondition →
                farm visits.                                                                                    CQ-012)

  `FR-VC-17`    Visit lifecycle: start / ASM / System      WF-05      UI-M-08,        S1 silent. S6 "Continue   **Missing** → CQ-041
                end, draft / resume,                                  UI-M-09         Visit" exists but does    
                submission,                                                           not resume; S7 states     
                immutability,                                                         resume / immutable /      
                correction.                                                           correction (unverified)   

  `FR-VC-18`    Visit review / approval  SM, Admin         WF-05      UI-W-22 (S5     S1 silent; S2 silent      **Missing** → CQ-013
                / rejection by SM or                                  shows visits                              
                Admin.                                                "Approved /                               
                                                                      Pending" in a                             
                                                                      report)                                   

  `FR-VC-19`    Farm visit report        System            WF-05      UI-W-22         S2 p.4, p.5; S4 item 34.0 **Needs Clarification** →
                sharing with farmers                                  ("Farmer Visit                            CQ-040
                (and read tracking).                                  Report                                    
                                                                      Notification"                             
                                                                      menu only)                                

  `FR-BM-01`    Multiple batches per     ASM               WF-06      UI-M-04,        S1 p.11; S2 p.5           Confirmed
                farm; a new batch can be                              UI-M-09                                   
                created without closing                                                                         
                existing ones.                                                                                  

  `FR-BM-02`    Previously entered batch ASM               WF-06      UI-M-09         S1 p.11; S2 p.5           Confirmed
                data is available for                                                                           
                selection and update                                                                            
                (carry-forward across                                                                           
                visits).                                                                                        

  `FR-BM-03`    Batch age is calculated  System            WF-06      UI-M-09,        S1 p.12                   **Needs Clarification.**
                from the flock start                                  UI-W-12                                   Unit for Mixed farms
                date. Units: Broiler --                                                                         undefined. → CQ-023
                days; Layer -- weeks;                                                                           
                Dairy -- months.                                                                                

  `FR-BM-04`    FCR = Cumulative Feed ÷  System            WF-06      UI-M-09,        S1 p.1, p.12; S2 p.4      **Needs Clarification.** The
                Mean Body Weight.                                     UI-W-12                                   S1 formula omits bird count.
                                                                                                                S6 uses Cumulative Feed ÷
                                                                                                                (MBW × current size). Feed
                                                                                                                inputs and units are
                                                                                                                undefined. → CQ-023

  `FR-BM-05`    Mortality = Dead animals System            WF-06      UI-M-09,        S1 p.12                   Confirmed (presentation as %
                ÷ Initial count.                                      UI-W-12                                   *\[A\]*; S6 treats the
                                                                                                                "Cumulative Mortality Count"
                                                                                                                input as a per-visit
                                                                                                                increment → CQ-023)

  `FR-BM-06`    Calculations update      System            WF-06      --              S1 p.12; S2 p.5           **Needs Clarification.**
                dynamically when data                                                                           Whether later visits and
                changes (automated                                                                              reports are recalculated
                recalculation of FCR and                                                                        when an earlier value
                mortality on data                                                                               changes. → CQ-023
                updates).                                                                                       

  `FR-BM-07`    Batch / flock creation,  ASM (mobile)      WF-06      UI-M-04 (S6     S2 p.5 (S1 says only      **Needs Clarification.** Who
                update and **closure**;  *\[A\]*                      "Close" with no "open batch")             closes, closure data,
                active and closed                                     data), UI-W-12                            reopen. → CQ-021
                batches per farm.                                     (read-only)                               

  `FR-BM-08`    Batch-level performance  SM, Admin, ASM    WF-06      UI-W-12,        S2 p.5; S1 p.13           Confirmed
                tracking and historical                               UI-W-22                                   
                tracking of batch                                                                               
                performance.                                                                                    

  `FR-BM-09`    Production % (layers)    System            WF-06      UI-M-09         S6 only ("Production %")  **Needs Clarification** →
                and other derived                                                                               CQ-023
                production metrics.                                                                             

  `FR-EX-01`    The web app defines and  Administrator     WF-08      UI-W-15         S1 p.13; S2 p.5           Confirmed
                manages Expense Types    *\[A: S1 does not                                                      
                categorised as **Fixed** name the actor\]*                                                      
                and **Variable**.                                                                               

  `FR-EX-02`    Fixed expenses have a    Administrator     WF-08      UI-W-15         S1 p.13                   Confirmed
                predefined amount                                                                               
                configured in the                                                                               
                system; variable                                                                                
                expenses take the amount                                                                        
                at data entry.                                                                                  

  `FR-EX-03`    Each expense type has an Administrator     WF-08      UI-W-15         S1 p.13                   Confirmed
                option indicating                                                                               
                whether image proof is                                                                          
                mandatory.                                                                                      

  `FR-EX-04`    Mobile: ASMs record      ASM               WF-08      UI-M-02         S1 p.13                   Confirmed. Whether the
                expenses during field                                 (dialog),                                 expense must link to a visit
                visits. Fixed amount                                  UI-M-10                                   ("during field visits") →
                auto-populated and not                                (orphan)                                  CQ-030. S6 has no image
                editable; variable                                                                              capture → UI gap.
                amount entered by the                                                                           
                ASM; image captured and                                                                         
                uploaded when required.                                                                         

  `FR-EX-05`    The ASM enters expense   ASM               WF-08      --              S1 p.5                    **Needs Clarification.** The
                data "subjected \[to\]                                                                          criteria (limits, frequency,
                predefined criteria".                                                                           eligibility) are undefined.
                                                                                                                → CQ-030

  `FR-EX-06`    All ASM expenses go      SM, Admin         WF-08      UI-W-18         S1 p.13                   Confirmed (approver scope →
                through an **Expense                                                                            CQ-031)
                Approval** workflow on                                                                          
                the web; authorized                                                                             
                users (e.g., Sales                                                                              
                Managers /                                                                                      
                Administrators) review,                                                                         
                approve or reject them.                                                                         

  `FR-EX-07`    For variable expenses,   SM, Admin         WF-08      UI-W-18 (no     S1 p.13                   **Needs Clarification.** S7
                the approver may modify                               amount edit in                            records the opposite
                or confirm the                                        S5)                                       ("approver cannot change
                reimbursable amount                                                                             amount", unverified). →
                during approval.                                                                                CQ-030

  `FR-EX-08`    Rejection reason;        SM, Admin, ASM    WF-08      UI-W-18 (reason S1 silent                 **Missing** → CQ-030
                cancellation /                                        mandatory in                              
                resubmission of rejected                              S5), UI-W-19                              
                or pending expenses.                                  ("cancelled"                              
                                                                      status)                                   

  `FR-EX-09`    Financial processing,    --                WF-08      --              S2 p.7                    Confirmed (scope boundary)
                accounting, payroll and                                                                         
                automated reimbursement                                                                         
                are **out of scope**.                                                                           

  `FR-RP-01`    **Farm-Level Reports:**  Admin, SM, ASM    WF-09      UI-W-22         S1 p.13                   Confirmed (columns and
                visit history,           (web)                                                                  filters undefined → CQ-033)
                production data, feed                                                                           
                usage, mortality rates                                                                          
                and overall performance                                                                         
                trends per farm.                                                                                

  `FR-RP-02`    **Batch-Level Reports:** Admin, SM, ASM    WF-09      UI-W-22         S1 p.13                   Confirmed (→ CQ-033)
                batch lifecycle status,                                                                         
                FCR, mortality and                                                                              
                historical comparison                                                                           
                across visits.                                                                                  

  `FR-RP-03`    **Supervisor and         Admin, SM         WF-09      UI-W-22         S1 p.14                   **Needs Clarification.**
                Area-Based Reports:**                                                                           "Supervisor" is not one of
                aggregated by                                                                                   the three S1 roles. → CQ-003
                Supervisors, Regions and                                                                        
                Territories (coverage,                                                                          
                efficiency).                                                                                    

  `FR-RP-04`    Generate and view        Admin, SM, ASM    WF-09      UI-W-02,        S1 p.4--5                 Confirmed (KPIs undefined →
                reports and dashboards                                UI-W-03,                                  CQ-033)
                (Admin, SM, ASM).                                     UI-M-02                                   

  `FR-RP-05`    ASM-wise and area-wise   Admin, SM         WF-09      UI-W-22         S2 p.6                    **Needs Clarification** (S2
                reporting; farm, batch                                                                          items not in S1) → CQ-033
                and visit-level reports;                                                                        
                feed type comparison                                                                            
                reports; output vs input                                                                        
                dashboards; map-based                                                                           
                visualization of visits                                                                         
                with visit sequence;                                                                            
                filters by Date, Farm                                                                           
                Type and Feed Type.                                                                             

  `FR-RP-06`    Reporting Parameter      Admin *\[A\]*     WF-09      none            S2 p.9                    **Needs Clarification** →
                Management: define                                                                              CQ-033
                parameters and parameter                                                                        
                range criteria for                                                                              
                analytic reports.                                                                               

  `FR-RP-07`    Named legacy reports:    Admin, SM         WF-09      UI-W-22 (menus  S4 item 14.0; S5          **Needs Clarification** →
                Visit Summary, Visit                                  only)                                     CQ-014, CQ-033
                Detail, Farm Monitor                                                                            
                Visit (Process 1), Farm                                                                         
                Evaluation (Process 2),                                                                         
                Sync Status, Sync                                                                               
                History, Visit Plan,                                                                            
                Expense, Feedback.                                                                              

  `FR-RP-08`    Export / print of        Admin, SM         WF-09      UI-W-22         S1 silent                 **Missing** → CQ-033
                reports (S5 shows Excel                                                                         
                and PDF buttons).                                                                               

  `FR-AU-01`    Maintain an audit trail  System            WF-10      UI-W-23         S1 p.14                   Confirmed
                of: User Creation,                                                                              
                Modification, Login,                                                                            
                Logout; Farm Creation,                                                                          
                Modification; Approvals;                                                                        
                Role-Based Access                                                                               
                Changes.                                                                                        

  `FR-AU-02`    Each audit record        System            WF-10      UI-W-23 (S5 has S1 p.14                   Confirmed
                includes: User, Date &                                no before /                               
                Time, Action Performed,                               after)                                    
                Module, Previous Value,                                                                         
                Updated Value (where                                                                            
                applicable).                                                                                    

  `FR-AU-03`    Audit logs are read-only Administrator (S1 WF-10      UI-W-23         S1 p.4, p.14              Confirmed
                and accessible only to   p.4)                                                                   
                authorised users.                                                                               

  `FR-AU-04`    Audit all user login     System            WF-10      UI-W-23         S1 p.17 (Security NFR)    Confirmed ("sensitive data"
                attempts and access to                                                                          undefined → CQ-035)
                sensitive data.                                                                                 

  `FR-AU-05`    Audit history of **all   System            WF-10      UI-W-23         S2 p.5--6 ("audit logging **Needs Clarification.**
                data changes**, with                                                  of all data changes")     Scope is wider than the S1
                previous / updated                                                                              event list. → CQ-035
                values and user /                                                                               
                timestamp.                                                                                      

  `FR-AU-06`    Audit retention period   --                WF-10      --              Not stated                **Missing** → CQ-035
                and archival.                                                                                   

  `FR-NT-01`    The system automatically System            WF-10      UI-M-12,        S1 p.14                   Confirmed (S2 p.9 also lists
                generates in-app                                      UI-W-21 (bell)                            "Email gateways" → CQ-032)
                notifications for                                                                               
                important system events.                                                                        

  `FR-NT-02`    ASM (mobile)             ASM               WF-10      UI-M-12         S1 p.14                   Confirmed (rejection
                notifications: Visit                                                                            outcomes implied *\[A\]*)
                Plan Approvals, New Farm                                                                        
                Approvals, Expense                                                                              
                Approvals.                                                                                      

  `FR-NT-03`    SM (web) notifications:  Sales Manager     WF-10      UI-W-21 (bell), S1 p.14--15               Confirmed (Administrator
                Pending Visit Plan                                    UI-W-03                                   notifications not stated →
                Approvals, Pending New                                                                          CQ-032)
                Farm Approvals, Pending                                                                         
                Expense Approvals.                                                                              

  `FR-NT-04`    Users can view           All               WF-10      UI-M-12,        S1 p.15                   Confirmed
                notification history                                  UI-W-21                                   
                within the application.                                                                         

  `FR-NT-05`    Approval status visible  ASM, SM, Admin    WF-10      UI-M-02,        S2 p.6                    Confirmed (consistent with
                in both mobile and web.                               UI-M-05,                                  S1)
                                                                      UI-W-17,                                  
                                                                      UI-W-18                                   

  `FR-NT-06`    Administrator-authored   Administrator     WF-10      UI-W-21,        S2 p.5 (pop-up images);   **Needs Clarification** →
                notifications / messages                              UI-M-12         S4 items 11.0, 23.0; S5;  CQ-032
                to field users (with                                                  S6                        
                pictures, response                                                                              
                required, due date) and                                                                         
                images shown as pop-ups                                                                         
                on mobile.                                                                                      

  `FR-NT-07`    Alerts for upcoming      System            WF-10      --              S2 p.14 ("Expected        **Needs Clarification** →
                visits, unclosed flocks,                                              Outcomes")                CQ-032
                etc.                                                                                            

  `FR-OF-01`    The mobile app supports  ASM               WF-11      UI-M-13         S1 p.3, p.15; S2 p.4      Confirmed
                offline operation when                                                                          
                connectivity is                                                                                 
                unavailable                                                                                     
                (offline-first).                                                                                

  `FR-OF-02`    **Initial Sync:**        ASM               WF-11      UI-M-13         S1 p.15                   Confirmed ("transactional
                user-initiated at start                                                                         data" content undefined; S6
                of day; downloads latest                                                                        downloads masters only →
                master **and                                                                                    CQ-024)
                transactional** data.                                                                           

  `FR-OF-03`    **Midday Sync:**         ASM               WF-11      UI-M-13         S1 p.15                   Confirmed ("critical
                user-initiated during                                                                           updates" undefined → CQ-024)
                the day; uploads new                                                                            
                data and retrieves                                                                              
                critical updates; also                                                                          
                the forced-sync                                                                                 
                mechanism.                                                                                      

  `FR-OF-04`    **Automatic Sync:**      System            WF-11      UI-M-13 (not in S1 p.15; S2 p.4           Confirmed (intervals and
                background sync                                       S6)                                       events undefined → CQ-024)
                triggered by network                                                                            
                availability, time                                                                              
                interval or system                                                                              
                events; automatic sync                                                                          
                when connectivity is                                                                            
                restored.                                                                                       

  `FR-OF-05`    **Day-End Sync:**        ASM               WF-11      UI-M-13         S1 p.15                   **Needs Clarification.**
                user-initiated at end of                                                                        "Completed or paused" and
                day; uploads all pending                                                                        any day lock (S4 "Day End
                transactions so that                                                                            Unlock Function"; S7) are
                daily activities are                                                                            undefined. → CQ-025
                "properly completed or                                                                          
                paused".                                                                                        

  `FR-OF-06`    In offline mode, GPS     System            WF-11      UI-M-09         S1 p.15                   Confirmed (acceptance /
                capture may have reduced                                                                        flagging policy → CQ-026)
                accuracy.                                                                                       

  `FR-OF-07`    Conflict detection and   System            WF-11      none (S6 only   Not in S1                 **Missing** → CQ-024
                resolution for data                                   "Reconciling                              
                changed both on the                                   records...")                              
                device and centrally.                                                                           

  `FR-OF-08`    Device registration /    ASM, Admin        WF-11      UI-M-14,        S4 items 3.0, 19.0, 20.0; **Needs Clarification** →
                configuration and mobile                              UI-W-24         S5; S6                    CQ-034
                maintenance functions                                                                           
                (initial configuration,                                                                         
                send DB, bug report,                                                                            
                delete DB, special sync,                                                                        
                app update).                                                                                    
  ------------------------------------------------------------------------------------------------------------------------------------------

### 3.2 Database trace and design status

Database entities shown in the source FR rows are not automatically
approved schema. The source analysis explicitly treats Module 01
entities in S3 as the approved baseline and other legacy entities as
evidence only. The following database-gap groups are relevant to
traceability:

  -----------------------------------------------------------------------
  Requirement area        Database gap references Traceability / design
                                                  note
  ----------------------- ----------------------- -----------------------
  Security & Access /     DBG-001--DBG-012 (where S3 is the approved
  User Management         applicable)             Module 01 column
                                                  baseline; outstanding
                                                  gaps must be resolved
                                                  before implementing
                                                  affected security
                                                  operations.

  Geography & Assignment  DBG-013 onward,         Region/Territory
                          including geography and hierarchy and
                          assignment findings     assignment rules must
                                                  follow CQ-006/CQ-007.

  Itinerary               Related itinerary and   Do not implement
                          approval gaps in        amendment or
                          Database Gap Analysis   unsupported status
                                                  transitions.

  Farm & New Farm         DBG-050--DBG-053        Farm attributes,
  Approval                                        approval state and Main
                                                  Farm/Dealer Farm
                                                  relationship require
                                                  the confirmed CQ
                                                  decisions and detailed
                                                  schema specification.

  Master Data / Breed /   DBG-054 and related     Use SRS lists; Breeder
  Supplier                master-data gaps        Farm is a separate
                                                  supplier/master entity
                                                  (CQ-039).

  Visit Questionnaire /   Related questionnaire,  Keep fixed visit fields
  Visit Capture           visit and answer-model  separate from
                          gaps                    configurable
                                                  questionnaire
                                                  definitions; version
                                                  every published change.

  Batch & Measurements    DBG-040--DBG-042        Batch is a major
                                                  missing entity in
                                                  earlier database
                                                  baselines; one breed
                                                  per batch is confirmed,
                                                  but full lifecycle
                                                  remains to be
                                                  specified.

  Expense                 Expense-related DBG     Expense amount
                          entries                 modification and
                                                  approval scope must
                                                  follow CQ-030/CQ-031.

  Audit / Notifications / Related audit,          Implement only
  Sync                    notification and        SRS-listed events and
                          synchronization gaps    server-wins conflict
                                                  behaviour; no Day-End
                                                  lock.
  -----------------------------------------------------------------------

**Important:** This is a cross-reference, not a database schema
approval. Refer to `Database_Gap_Analysis.md` for the complete DBG
register and to the relevant module specification before creating tables
or stored procedures.

## 4. Clarification decision alignment

The following CQ decisions affect traceability. They are recorded as
business decisions supplied during the clarification exercise; formal
stakeholder sign-off and any SRS change-control updates remain separate
governance steps.

  -----------------------------------------------------------------------
  Area                    Decision applied in     Related FR groups
                          version 0.2             
  ----------------------- ----------------------- -----------------------
  Roles and permissions   Administrator, SM and   FR-UM, FR-RP, FR-NT
                          ASM only; Supervisor is 
                          ASM; Analyst / Finance  
                          Analyst deferred.       

  Approval ownership      SM approves             FR-FM-05/06, FR-IP,
                          itineraries,            FR-EX, FR-NT
                          mobile-created farms    
                          and expenses; approver  
                          is the assigned ASM's   
                          SM.                     

  Reporting visibility    SM sees only assigned   FR-AH, FR-FM, FR-IP,
                          ASMs; ASM restricted to FR-VC, FR-RP
                          assigned                
                          Regions/Territories.    

  Geography               Province → District →   FR-AH, FR-FM, FR-IP,
                          Region → Territory; ASM FR-RP
                          assigned to Regions and 
                          Territories.            

  Itinerary and visits    Daily itinerary = one   FR-IP, FR-VC
                          Territory with selected 
                          farms; ASM defines      
                          recurring plans; SM     
                          approves; unplanned     
                          visits allowed with     
                          post-visit approval;    
                          visit submission is     
                          final.                  

  Farm and batch          Farm Type is a farm     FR-FM, FR-BM, FR-MD
                          master attribute;       
                          mandatory GPS on mobile 
                          farm registration; one  
                          Main Farm may have      
                          unlimited Dealer Farms; 
                          one breed per batch.    

  Questionnaire           Fixed visit fields      FR-VQ, FR-VC
                          remain system fields;   
                          conditional             
                          questionnaire questions 
                          required; supported     
                          Figma answer types;     
                          every publish creates a 
                          new version; Admin      
                          configures              
                          mandatory/validation.   

  Expenses                Approver may modify     FR-EX
                          variable amount; only   
                          assigned ASM's SM       
                          approves.               

  Synchronization         Server wins on          FR-OF
                          conflict; no Day-End    
                          lock.                   

  Reports, notifications  SRS reports only;       FR-RP, FR-NT, FR-AU
  and audit               in-app notifications    
                          only; SRS-listed audit  
                          events only.            

  Exclusions              No Farm Evaluation,     FR-XT; related FR
                          external farmer         groups
                          sharing, non-SRS        
                          features, or            
                          prototype-only          
                          features.               

  Security encryption     Reversible AES-256-GCM  FR-UM-10, NFR-04
                          request remains subject 
                          to security approval    
                          and must not be used    
                          for passwords.          
  -----------------------------------------------------------------------

### 4.1 Items requiring continued clarification / design

-   Exact CRUD rights for every master-data entity and each role.
-   Assignment cardinality, overlapping Region/Territory assignments,
    effective dates and assignment history.
-   Detailed status transitions and rejection/resubmission behaviour for
    itineraries, farms and expenses.
-   Exception approval record and rejection handling for unplanned
    visits.
-   Recurrence patterns and itinerary generation horizon.
-   Boundary between fixed visit fields and questionnaire data model.
-   Questionnaire version activation/effective-date rules and
    in-progress visit behaviour.
-   Batch lifecycle, closure fields and reopen rules.
-   Exact SRS report catalogue, report filters and role-level data
    scope.
-   Audit retention and role visibility.
-   Security approval for reversible encryption; passwords must use an
    appropriate one-way password hashing mechanism.

## 5. Non-functional requirements traceability

  ---------------------------------------------------------------------------------------
  NFR ID         Requirement summary   Source         Status in       Related validation
                 (from Functional                     source analysis / test evidence
                 Requirements                                         
                 Analysis)                                            
  -------------- --------------------- -------------- --------------- -------------------
  NFR-01         Response to user      S1 p.17        Confirmed       Performance test
                 input within 2 s;                                    results required;
                 average response time                                no test case ID
                 below 1.5 s                                          assigned yet.

  NFR-02         At least 1000         S1 p.17        Confirmed;      Load and stress
                 concurrent users; at                 sizing          test evidence
                 least 100                            assumptions to  required.
                 transactions/minute                  validate        

  NFR-03         Recovery within 5     S1 p.17        Confirmed       Recovery/failover
                 minutes after failure                                test evidence
                                                                      required.

  NFR-04         Authentication with   S1 p.17        Confirmed;      Security review and
                 at least 128-bit                     security design penetration-test
                 encryption; RBAC;                    dependency      evidence required.
                 audit login attempts                 noted           
                 and sensitive-data                                   
                 access                                               

  NFR-05         Company coding        S1 p.17        Confirmed;      CI coverage report
                 standards; at least                  coverage scope  and repository
                 80% automated                        needs           evidence required.
                 unit-test coverage;                  validation      
                 version control                                      

  NFR-06         Compatibility:        S1 p.3, p.17   Needs           Browser/OS
                 Windows 8+, macOS 11,                Clarification   compatibility test
                 Ubuntu 20.04; latest                                 matrix required
                 Chrome, Firefox, Edge                                after scope
                 (S1 p.3 lists Chrome                                 confirmation.
                 and Edge only)                                       

  NFR-07         WCAG 2.0              S1 p.17        Confirmed       Accessibility
                 accessibility                                        audit/test evidence
                                                                      required.

  NFR-08         Horizontal            S1 p.17        Confirmed       Scalability and
                 scalability; server                                  peak-load test
                 CPU at or below 70%                                  evidence required.
                 at peak                                              

  NFR-09         Daily automated       S1 p.18        Confirmed       Backup job and
                 backups; quarterly                                   restore-test
                 backup/recovery tests                                evidence required.

  NFR-10         Android mobile        S1 p.3, p.15   Confirmed       Supported Android
                 platform and offline                                 version/device
                 capability                                           matrix and offline
                                                                      test evidence
                                                                      required.

  NFR-11         Encryption at rest    S1 p.3         Confirmed       Security
                 and in transit                                       configuration and
                                                                      verification
                                                                      evidence required.

  NFR-12         On-premises           S2 p.9         Confirmed       Environment and
                 deployment with                                      deployment evidence
                 Dev/Test/Production                                  required.
                 separation; document                                 
                 storage on-premises                                  
  ---------------------------------------------------------------------------------------

## 6. Coverage gaps and implementation readiness

  -----------------------------------------------------------------------
  Traceability dimension  Current assessment      Required next action
  ----------------------- ----------------------- -----------------------
  Requirement → source    FR IDs and source       Preserve FR IDs; do not
                          references exist in the renumber without a
                          Functional Requirements controlled revision.
                          Analysis.               

  Requirement → workflow  FR modules map to       Validate detailed
                          WF-01--WF-11.           workflow steps and
                                                  status transitions
                                                  against the SRS and CQ
                                                  decisions.

  Requirement → UI        Existing prototype      Complete screen-level
                          screens mapped; missing acceptance criteria and
                          screens and redesign    final Figma designs.
                          needs identified.       

  Requirement →           Role matrix updated to  Resolve remaining CRUD
  role/permission         three current roles and and data-scope details.
                          confirmed approval      
                          ownership.              

  Requirement → database  Earlier database gap    Complete module
                          analysis identifies     database specifications
                          missing                 only after blocking
                          entities/fields; most   requirements are
                          business-module         resolved.
                          entities are not        
                          approved.               

  Requirement → test      This version identifies Create test scenarios
                          test evidence needed    and acceptance criteria
                          for NFRs but does not   after
                          contain approved test   requirements/design are
                          cases.                  baselined.
  -----------------------------------------------------------------------

**Implementation readiness:** Not yet baseline-ready. The trace is
useful for planning and identifying coverage gaps, but open business
rules, detailed workflow states, module database specifications, final
UI designs and test acceptance criteria remain prerequisites.

## 7. Traceability maintenance rules

1.  Every new or changed requirement must retain a unique FR/NFR
    identifier or receive a new identifier under change control.
2.  A requirement change must be reviewed against its workflow,
    role/permission, UI, database and test references.
3.  A UI prototype change does not by itself change an SRS requirement.
4.  A CQ answer is a clarification record; if it changes the signed SRS,
    update the SRS through the agreed change-control process.
5.  Do not mark a requirement **Implemented** or **Tested** without
    implementation evidence or a test result.
6.  Keep `Source status`, `Clarification alignment`,
    `Implementation status` and `Test status` as separate concepts.
7.  Update this matrix whenever the Functional Requirements Analysis,
    Business Workflow Analysis, Role Permission Matrix, UI Screen
    Mapping or Database Gap Analysis receives a controlled revision.

## 8. Change history

  -----------------------------------------------------------------------
  Version                 Date                    Changes
  ----------------------- ----------------------- -----------------------
  0.1                     First-pass draft        Earlier traceability
                          referenced in S7        workbook; its claimed
                                                  approved user decisions
                                                  are treated as
                                                  unverified under
                                                  CQ-004.

  **0.2**                 **05-11-2026**          Rebuilt as a
                                                  source-linked Markdown
                                                  matrix using the FR
                                                  identifiers and source
                                                  statuses in Functional
                                                  Requirements Analysis;
                                                  added workflow, UI and
                                                  database
                                                  cross-references;
                                                  aligned to recorded CQ
                                                  decisions; added NFR
                                                  traceability, coverage
                                                  gaps and maintenance
                                                  rules.
  -----------------------------------------------------------------------

## 9. Source documents

-   `Software Requirement Specification.pdf` -- primary functional and
    non-functional requirement source (S1).
-   `Project Proposal.pdf` -- scope and supporting requirements (S2).
-   `Functional_Requirements_Analysis.md` -- FR identifiers, requirement
    wording, source references and source status.
-   `Business_Workflow_Analysis.md` -- WF-01 to WF-11.
-   `Role_Permission_Matrix.md` -- role and permission mapping.
-   `UI_Screen_Requirement_Mapping.md` -- UI-W and UI-M screen
    references.
-   `Database_Gap_Analysis.md` -- DBG references and database design
    gaps.
-   `Business_Clarification_Register.md` -- CQ decisions and outstanding
    clarifications.
