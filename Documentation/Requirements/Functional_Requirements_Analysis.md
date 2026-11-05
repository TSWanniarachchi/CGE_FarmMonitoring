# CGE Farm Monitoring System – Functional Requirements Analysis

| | |
|---|---|
| Document | Functional_Requirements_Analysis.md |
| Version | 0.2 – Updated with user clarification responses |
| Date | 05-11-2026 |
| Status | **Analysis only – pending stakeholder validation and formal sign-off.** User clarification responses are reflected as working decisions; they are not represented as signed SRS amendments. |
| Related | [Role_Permission_Matrix](Role_Permission_Matrix.md) · [Business_Workflow_Analysis](Business_Workflow_Analysis.md) · [Database_Gap_Analysis](Database_Gap_Analysis.md) · [UI_Screen_Requirement_Mapping](UI_Screen_Requirement_Mapping.md) · [Business_Clarification_Register](Business_Clarification_Register.md) |

## 1. Purpose and approach

### Delivery phase

- **Phase 1 – Web:** deliver the web application and its required backend capabilities first.
- **Future Phase – Mobile:** deliver the mobile application later, using the shared backend/API.
- Requirements that are inherently mobile (for example offline visit capture and mobile farm registration) remain documented for the future mobile phase; they are not silently converted into Phase 1 web features.

This document breaks the Software Requirement Specification (SRS) into uniquely identified functional requirements. It then checks each one against the other project sources. It records:

- what is documented;
- what is assumed;
- what is still unanswered.

It does not decide anything on behalf of stakeholders. The delivery sequence is Phase 1 – Web, followed by Future Phase – Mobile. Requirement rows retain their original IDs for traceability. SRS terminology is preserved: *Itinerary*, *Visit Questionnaire*, *Batch/Flock*, *New Farm Approval*, *Area Sales Manager (ASM)*, *Sales Manager (SM)*, *Administrator*.

### 1.1 Source register

All documents in this set cite these source IDs.

| ID | Source | Version / date | Authority used in this analysis |
|---|---|---|---|
| **S1** | *PMS – CGE FMS – Software Requirement Specification.pdf* | v1.0, 15-07-2026 (prepared by the Business Analyst) | **Primary.** Page numbers are the printed page numbers ("p.N"). |
| **S2** | *PMS – CGE Farm Monitoring System – Project Proposal.pdf* | v1.1, 13-07-2026 | Primary for scope. Where S1 and S2 conflict, the conflict is recorded rather than resolved. |
| **S3** | `Documentation/06_Database/Farm_FMS_Module_01_Database_Specification_v0.2.xlsx` | v0.2 | Approved column baseline for **Module 01 (Security & Access) only**. |
| **S4** | *DataBase Design.xlsx* (legacy feature list "CGE FARM MS" and legacy table list "CGE Farm Tables") | undated | Secondary: evidence of the existing (2008) system and the planned feature list. |
| **S5** | Figma web prototype (*Farm Management System – web.zip*, 24 screens) | 02-11-2026 export | Secondary. S1 p.16 states the mock-ups are "for illustrative purposes only and may not reflect the final design". |
| **S6** | Figma mobile prototype (*Farm monitoring System – Mobile.zip*, 15 screens) | 02-11-2026 export | Secondary (same caveat). S1 p.12 references this prototype URL. |
| **S7** | *Farm_FMS_Requirements_Traceability_Matrix_v0.1.xlsx* (Desktop) | v0.1 first-pass draft | Secondary. It cites "approved user decisions" that are **not recorded in any signed document**. These are treated as unverified (see CQ-004). |
| **S8** | *Farm_FMS_Database_Gap_Analysis_v0.1.xlsx* (Desktop) | v0.1 first-pass draft | Secondary. |
| **S9** | `Documentation/Architecture/FarmFMS_Architecture_Decisions_v2.md` | current | Architecture decisions (AD-xx). Not a business source. |
| **S10** | `Documentation/06_Database/Database_Conventions.md` | current | Naming, foreign-key and procedure conventions. Not a business source. |

### 1.2 Review limitations

1. **L1:** In the supplied copies of S1 (p.19–21) and S2 (p.16–18), the **signature and date fields are blank**. The approval state of the SRS cannot be confirmed from these files (CQ-005).
2. **L2:** Two parts of S1 are images only and could not be reviewed in this environment: the context diagram (p.2) and the *User Interface Screens* section (p.16). The UI review uses S5 and S6 instead.
3. **L3:** S5 and S6 are client-side mock prototypes. Their data, hard-coded lists and behaviour are evidence of design intent, not of agreed requirements.
4. **L4:** The decisions that S7 and S8 cite as "approved user decisions (Modules 01–15)" have no source document. Where they contradict S1, the requirement is marked **Needs Clarification**.
5. **L5:** No existing questionnaire forms were supplied, although S1 p.9 requires them to be replicated (CQ-019).

### 1.3 Conventions

| Item | Meaning |
|---|---|
| `FR-<MOD>-NN` | Functional requirement. Modules: **UM** User Management, **AH** Area & Hierarchy, **FM** Farm Management, **MD** Master Data, **IP** Itinerary Planning & Approval, **VQ** Visit Questionnaire configuration, **VC** Visit Data Capture, **BM** Batch Monitoring & Calculations, **EX** Expense Management, **RP** Reporting & Analytics, **AU** Audit & Logging, **NT** Notifications & Approvals, **OF** Offline Capability, **XT** items found outside the SRS. |
| `NFR-NN` | Non-functional requirement (summarised only; section 17). |
| `CQ-NNN` | Clarification question in the [Business_Clarification_Register](Business_Clarification_Register.md). |
| `WF-NN` | Workflow in the [Business_Workflow_Analysis](Business_Workflow_Analysis.md). |
| `DBG-NNN` | Gap in the [Database_Gap_Analysis](Database_Gap_Analysis.md). |
| `UI-W-NN` / `UI-M-NN` | Web / mobile screen in the [UI_Screen_Requirement_Mapping](UI_Screen_Requirement_Mapping.md). |
| **Confirmed** | Stated in S1 (or S3 for Module 01) and not contradicted by another source. |
| **Needs Clarification** | Stated in a source, but ambiguous, incomplete, or contradicted by another source. |
| **Missing** | Required for the system to work end to end (or asked about in the review brief), but not stated in S1. Analyst-identified; nothing is assumed about the answer. |
| *[A]* | Analyst assumption or inference. Everything else in the requirement rows is documented in the cited source. |

**DB entities column:** names from S3 are approved (Module 01). Everything else is written *"none approved"*, plus the legacy table name from S4 where one exists, as evidence only.

---

## 2. User Management (S1 §User Management, p.4–5)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-UM-01 | The system shall support secure user authentication. | All users | Active user account | User enters credentials (Username + Password per S3) → system validates → session starts | Authenticated session; login event recorded | AppUser, UserSession, LoginHistory (S3) | UI-W-01, UI-M-01 | S1 p.4; S3 Confirmed Decisions | Confirmed |
| FR-UM-02 | The system shall enforce Role-Based Access Control so users access only the functions and information relevant to their responsibilities. | System | User has ≥1 active role | Every request is checked against the user's effective permissions | Unauthorised functions hidden and blocked at API level | Role, Permission, Menu, RoleMenuAccess (S3) | UI-W-07, UI-W-08 | S1 p.3, p.4; S3 Read Me row 20 | Confirmed |
| FR-UM-03 | A user may hold multiple roles; the system combines (unions) the permissions of all assigned roles. | Administrator | User exists | Admin assigns one or more roles | Effective permissions = union of active roles' Allow permissions | UserRole (S3) | UI-W-04 | S1 p.4; S3 | Confirmed |
| FR-UM-04 | The system shall support only the roles Administrator, Sales Manager (SM) and Area Sales Manager (ASM). The term Supervisor refers to ASM and is not a separate role. | – | – | – | Three roles available | Role (S3) | UI-W-04, UI-W-08 | S1 p.4 | **Confirmed by clarification response:** supported roles are Administrator, Sales Manager (SM) and Area Sales Manager (ASM). Supervisor is the same role as ASM. SM approves itineraries, farms and expenses. See CQ-001–CQ-003. The role interpretation remains subject to formal stakeholder sign-off. |
| FR-UM-05 | The Administrator shall create, edit, activate, deactivate and manage user accounts. | Administrator | Admin authenticated | Create / edit / activate / deactivate user | Account state changed; audit record written (FR-AU-01) | AppUser, UserAccount (S3) | UI-W-04 | S1 p.4 | Confirmed |
| FR-UM-06 | The system shall maintain user data: Name, Role, Email, Contact Number, Department, User Id, Active Status. | Administrator | – | Admin enters / maintains these fields | User master complete | AppUser (S3: no Department column → DBG-001) | UI-W-04 | S1 p.5 | **Partially confirmed:** retain only user-master fields listed in the SRS (CQ-037). Department's persistence and mapping against the Module 01 baseline remain to be validated. |
| FR-UM-07 | The Administrator shall have unrestricted access to all system functions. | Administrator | – | – | All functions accessible | Role.IsSystemRole (S3) | All web | S1 p.4 | Confirmed |
| FR-UM-08 | Platform access per role: ASM uses **mobile** for operational functions and **web** for reports and dashboards; SM and Administrator use the web. | System | Account platform flags set | Login permitted only on enabled platforms | Platform-restricted access | UserAccount.CanAccessWeb / CanAccessMobile (S3) | UI-W-01, UI-M-01 | S1 p.4–5; S3 | Confirmed (S5 also lets the ASM approve plans and expenses on web → CQ-001) |
| FR-UM-09 | Password policy (min 8 chars; upper, lower, number, special), lockout after 5 failures (Admin unlock), 30-min inactivity timeout, no MFA, concurrent sessions allowed, deactivation revokes sessions. | System / Administrator | – | Enforced at login and session handling | Policy enforced | AppUser, UserSession (S3) | UI-W-01, UI-W-04 | S3 Confirmed Decisions | Confirmed (S3) |
| FR-UM-10 | Passwords are stored with reversible AES-256 encryption so an authorised Administrator can view them; every reveal is audited. | Administrator | Privileged permission | Admin requests reveal → audited | Plaintext shown to Admin only | AppUser Enc* columns (S3) | UI-W-04 | S3 Read Me rows 10–12, 21; S9 AD-11 | **Partially confirmed:** reversible AES-256-GCM was requested, subject to security approval (CQ-036). This is not a security approval; implementation remains blocked until the security review and formal approval are recorded. |
| FR-UM-11 | Login history: successful, failed and logout events. | System | – | Each event appended | Append-only login history | LoginHistory (S3) | UI-W-23 | S1 p.14; S3 | Confirmed |
| FR-UM-12 | Menu management and role-to-menu permission assignment (View, Create, Edit, Delete, Approve, Export). | Administrator | – | Admin maintains menus and grants permissions per role | Role permissions drive menus and API authorization | Menu, Permission, RoleMenuAccess (S3) | UI-W-07, UI-W-08 | S3 Confirmed Decisions; S4 item 5.0 | Confirmed (S3). S5 shows only View/Add/Edit/Delete → see Role_Permission_Matrix §5. |
| FR-UM-13 | The Administrator shall configure system settings. | Administrator | – | Admin maintains settings | Settings applied | none approved (legacy Settings, Config – S4) | UI-W-24 | S1 p.4 | **Excluded unless present in the SRS:** prototype-only settings such as device IMEI registration and per-device document numbering are not included (CQ-034). |

## 3. Area & Hierarchy Management (S1 p.6)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-AH-01 | Two-level reporting hierarchy: multiple ASMs report to a Sales Manager. | Administrator | SM and ASM accounts exist | Admin maps ASM → SM | One current SM per ASM; history preserved (S3) | UserHierarchy (S3) | UI-W-06 | S1 p.6; S3 | Confirmed |
| FR-AH-02 | The Sales Manager has visibility of, and oversight over, operations across all assigned ASMs. | Sales Manager | Hierarchy mapping exists | SM views data of their ASMs | SM sees assigned ASMs' data only *[A: S1 does not say whether an SM may see other SMs' ASMs]* | UserHierarchy (S3) | UI-W-03, UI-W-22 | S1 p.6; S2 p.6 ("Data access limited to assigned areas and reporting hierarchy") | **Confirmed by clarification response:** an SM can see only ASMs assigned to that SM (CQ-046). |
| FR-AH-03 | Farms are organised in a geographical hierarchy of **Regions** and **Territories**; each Region contains multiple Territories. | Administrator | – | Admin maintains Regions and Territories | Geography available for farms, assignments and reports | none approved (Territory owned by "Module 02" per S3; legacy uses Area – S4) | none in S5 (S5 has Area → Main Route → Sub Route) | S1 p.6 | **Confirmed by clarification response:** Province → District → Region → Territory (CQ-006). |
| FR-AH-04 | Each ASM is assigned to one or more Regions and manages the associated Territories within them. | Administrator | Regions / Territories exist | Admin assigns ASM | ASM's operating area defined | UserTerritory (S3 assigns **Territories**, not Regions) | UI-W-05 *[A]* | S1 p.6; S3 | **Partially confirmed:** ASMs are assigned to Regions and Territories (CQ-007). Assignment cardinality, overlap and effective-date/history rules remain open. |
| FR-AH-05 | The system restricts ASMs to operate only within their assigned Regions and corresponding Territories. | System | FR-AH-04 | Farm lists, itineraries, visits and new farms are filtered and validated by assignment | Out-of-area actions blocked | UserTerritory (S3) | UI-M-03, UI-M-07, UI-M-08 | S1 p.6; S2 p.6 | **Confirmed by clarification response:** ASM access is restricted to assigned areas only (CQ-009). Unplanned visit handling is separately defined in FR-IP-04 and must not be interpreted as permission to operate in unassigned areas. |
| FR-AH-06 | Route levels (Main Route, Sub Route) under the area structure, with farms mapped to Sub Routes. | Administrator | – | Admin maintains routes and maps farms | Routes available for planning | none approved (legacy AreaWiseMainRoute, SubRoute – S4) | UI-W-10 | S4 item 8.0; S5; S6 | **Excluded:** Main Route / Sub Route structures are not in the SRS and are excluded under CQ-034. |

## 4. Farm Management (S1 p.6–7)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-FM-01 | Maintain farm master data: Farm Code, Farm Name, Farm Address, Owner Name, Owner Contact Number, Dealer Indicator (Is Dealer), Province, District, Region, Territory, Active Status. | Administrator, Sales Manager | Geography masters exist | Create / edit / deactivate farm | Farm master record | none approved (legacy Farm, FarmContact – S4) | UI-W-11, UI-W-12 | S1 p.6 | Confirmed (field list). S5 has no Region, Territory, Farm Type or GPS fields → UI gaps. |
| FR-FM-02 | If a farm is a dealer, the system shall allow associating and mapping the farm accordingly. | Administrator, Sales Manager | Farm flagged Is Dealer | Map dealer farm (to what is not stated) | Dealer association stored | none approved (legacy Farm.IsFarmDealer, Farm.FarmId – S4) | UI-W-11 | S1 p.6 | **Confirmed by clarification response:** one Main Farm may have unlimited Dealer Farms (CQ-029). Dealer association cardinality is confirmed; detailed relationship attributes and validation rules remain to be specified. |
| FR-FM-03 | View farm visit history and access historical data for each farm. | Admin, SM, ASM | Farm exists | Open farm profile → history | Visit and batch history visible | none approved | UI-W-12, UI-M-04 | S1 p.6 | Confirmed |
| FR-FM-04 | Both web and mobile capture farm and farmer details when a new farm or dealer farm is identified. | ASM (mobile); Admin, SM (web) | Within assigned area (FR-AH-05) | Enter new farm details → save | New farm record created | none approved (legacy Temp_NewFarm – S4) | UI-M-05, UI-W-11 | S1 p.7 | Confirmed |
| FR-FM-05 | Any farm or dealer farm created on mobile requires approval through the web **New Farm Approval** function by System Administrators or Sales Managers. | Administrator, Sales Manager | Mobile-created farm synced | Review → approve | Approved farm | none approved | UI-W-25 (placeholder in S5) | S1 p.7 | **Partially confirmed:** mobile-created farms require SM approval (CQ-028). Approval rules for web-created farms remain unspecified. |
| FR-FM-06 | Only after approval: the farm becomes active; the assigned ASM can plan and record visits on mobile; visit data capture for the farm is enabled. | System | FR-FM-05 approved | Status change → farm enabled for planning / visits | Farm usable in FR-IP-xx and FR-VC-xx | none approved | UI-M-03, UI-M-08 | S1 p.7 | Confirmed |
| FR-FM-07 | Standalone system: no integration with M3 for farm information. | – | – | – | No M3 interface | – | – | S1 p.7; S2 p.7 (no ERP integration) | Confirmed: M3/ERP integration remains excluded; prototype-only Company Master / ERP Code is excluded under CQ-034. |
| FR-FM-08 | Rejection of a new farm: reason, notification and resubmission. | Admin, SM, ASM | New farm pending | Reject → reason → ASM notified → ASM corrects / resubmits? | – | none approved | UI-W-25, UI-M-05 | S1 is silent. S6 shows "Rejected" with no reason; S7 states "rejected submissions editable/resubmittable" (unverified) | **Missing** → CQ-028 |
| FR-FM-09 | Farm Type shall be maintained as a farm-master attribute (not as a visit-level field). | – | – | – | – | none approved (legacy FarmType; Temp_NewFarm.FarmTypeId – S4) | UI-W-14, UI-M-09 | S1 p.9 selects Farm Type **per visit**; S6 lets the ASM change the farm's type during a visit; S5 farm form has no farm type | **Confirmed by clarification response:** Farm Type is a farm-master attribute, not a visit-level field (CQ-022). |
| FR-FM-10 | The mobile farm-registration workflow shall require GPS coordinates for a newly registered farm. | – | – | – | – | none approved | none | Not in S1 farm data. S2 p.6 requires "map-based visualization of visits"; S7 states GPS required (unverified) | **Partially confirmed:** GPS is mandatory during mobile farm registration (CQ-027). Whether web-created farms require coordinates and the permitted accuracy/source rules remain open. |
| FR-FM-11 | Multiple farm contacts (name, job role, contact, email, address). | Admin, SM, ASM | – | Add contacts to a farm | Contacts stored | none approved (legacy FarmContact – S4) | UI-W-11, UI-M-05 | S4; S5; S6 (S1 has Owner Name / Contact only) | **Needs Clarification** → CQ-028 |
| FR-FM-12 | Duplicate farm detection when new farms are captured. | System | – | – | – | none approved | UI-M-05 | Not in S1. S6 performs a local, overridable name / contact check | **Missing** → CQ-028 |

## 5. Master Data (S1 p.4 "Create, modify and maintain master data"; S2 p.5)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-MD-01 | The Administrator creates, modifies and maintains master data. | Administrator | – | CRUD / deactivate master records | Masters available to web and mobile | none approved | UI-W-14, UI-W-15, UI-W-13, UI-W-10 | S1 p.4 | Confirmed (the list of masters is not defined in S1) |
| FR-MD-02 | Configuration of feed types, water availability, housing types and grading options. | Administrator | – | Maintain option lists | Lists used by visit capture | none approved (legacy Feed Type, FeedForm, FarmHousingType, FarmWaterAvailabilty – S4) | UI-W-14, UI-W-15 | S2 p.5 | Confirmed (S2). The S1 visit flow lists fixed values (FR-VC-07..10). S5/S6 lists conflict → CQ-044, CQ-045 |
| FR-MD-03 | Breed data management, and breeder farm–breed mapping. | Administrator | – | Maintain breeder farms, breeds and mappings | Breeds selectable when creating batches | none approved (legacy BreedType – S4) | UI-W-13 | S2 p.5, p.8; S1 p.9 ("Select Breed") | **Partially confirmed:** Breeder Farm is a separate supplier/master entity (CQ-039). Its required attributes and relationship to breed/batch remain open. |
| FR-MD-04 | Province and District masters (farm attributes). | Administrator | – | Maintain lists | Used by farm master | none approved (legacy ProvinceMaster, DistrictMaster – S4) | none in S5 | S1 p.6 (fields only) | **Confirmed by clarification response:** Province → District → Region → Territory (CQ-006). |

## 6. Itinerary Planning & Approval (S1 p.7)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-IP-01 | The mobile app allows ASMs to plan their **daily** itineraries. | ASM | Approved farms in assigned territory | ASM creates itinerary for a date | Itinerary submitted for approval *[A: submission step not stated]* | none approved (legacy VisitPlan – S4) | UI-M-07 | S1 p.7 | **Confirmed by clarification response:** daily itinerary; one Territory per day with selected farms (CQ-008). |
| FR-IP-02 | An ASM may plan **one territory per day**. | ASM | – | Select one territory for the date | One-territory rule enforced | none approved | UI-M-07 | S1 p.7 | **Confirmed by clarification response:** one Territory per day (CQ-008); the selected farms belong to that Territory. |
| FR-IP-03 | Within the selected territory, the ASM can schedule multiple farm visits. | ASM | FR-IP-02 | Add farms to the day's itinerary | Farm list per itinerary | none approved | UI-M-07 (S6 has no farm selection) | S1 p.7 | Confirmed |
| FR-IP-04 | ASMs can create **unplanned itineraries** for visits outside predefined territories. | ASM | – | Create unplanned itinerary | Unplanned visits possible | none approved | UI-M-08 | S1 p.7; S2 p.4 ("unplanned visit capture") | **Partially confirmed:** unplanned visits are allowed, with post-visit approval (CQ-012). They remain subject to assigned-area restrictions (CQ-009). The exact post-visit approval record, timing and exception criteria remain open. |
| FR-IP-05 | The web app supports generating predefined **recurring itineraries**. | Admin / SM *[A]* | – | Define recurrence → generate itineraries | Itineraries created automatically | none approved | none | S1 p.7 | **Partially confirmed:** ASM defines their own recurring plans (CQ-010), and SM approves (CQ-011). Recurrence patterns, generation horizon, edit/cancel rules and approval timing remain open. |
| FR-IP-06 | System Administrators and Sales Managers can create, modify, review and approve itineraries planned by ASMs on the web. | Admin, SM | Itinerary submitted | Review → modify / approve | Approved itinerary | none approved | UI-W-17 | S1 p.4–5, p.7 | Confirmed (S5 also lets the ASM approve → CQ-001) |
| FR-IP-07 | If an ASM has no planned itinerary, Admin or SM can create and assign one on the web. | Admin, SM | ASM has no itinerary for the date | Create & assign | Itinerary assigned to ASM | none approved | UI-W-17 | S1 p.7 | Confirmed (approval state of an assigned itinerary not stated → CQ-011) |
| FR-IP-08 | Rejection of an itinerary: reason, notification and resubmission. | Admin, SM | Itinerary submitted | – | – | none approved | UI-W-17 (S5 has no reject) | S1 is silent (S6 shows "Rejected"; S7 "rejected plan requires new plan", unverified) | **Missing** → CQ-011 |
| FR-IP-09 | Amendment or cancellation of an approved itinerary. | ASM, Admin, SM | Approved itinerary | – | – | none approved | UI-W-26 (placeholder) | S4 item 9.0 ("Amend current date Visit Plan"); S5 placeholder; S1 silent | **Missing** → CQ-011 |
| FR-IP-10 | Relationship between an approved itinerary and visit capture (is approval a precondition?). | System | – | – | – | none approved | UI-M-08 | S1 p.7 is silent | **Partially confirmed:** unplanned visits are allowed with post-visit approval (CQ-012). The exact approval record and timing remain open. |

## 7. Visit Questionnaire – configuration (S1 p.8–9)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-VQ-01 | Dynamic questionnaire management in the web app: define, configure and modify questionnaires. | "Authorized users (e.g., System Administrators or Sales Managers)" | – | Build questionnaire → sections / questions → options | Questionnaire available | none approved (legacy QuestionnaireSet, QuestionnaireSection, Questions, AnswerType, QuesAnswer – S4) | UI-W-16 | S1 p.8 | **Needs Clarification.** S1 p.4 gives questionnaires to the Administrator only; the S1 p.5 SM list omits them. → CQ-020 |
| FR-VQ-02 | Questions can be customised by farm type, operational need or data-collection objective. | Admin (SM?) | – | Assign questionnaire to farm type (and other dimensions?) | Correct questionnaire served per visit | none approved | UI-W-16 (S5 has no assignment) | S1 p.8 | **Needs Clarification** → CQ-020 |
| FR-VQ-03 | Questionnaires can be updated without system changes or redeployment. | Admin | – | Change configuration | New definition reaches mobile at next sync *[A]* | none approved | UI-W-16 | S1 p.8 | Confirmed (effect on history → FR-VQ-08) |
| FR-VQ-04 | Questionnaires defined on web are available on mobile; ASMs must complete the relevant questionnaire during each farm visit. | ASM | Initial / midday sync done | Visit → questionnaire → submit | Answers captured per visit | none approved (legacy QuesAnswerHeader, QuesAnswerDetail, QuesAnswerImage – S4) | UI-M-09 | S1 p.8 | Confirmed (S6 capture screen has no questionnaire engine → UI gap) |
| FR-VQ-05 | Questions may be logically linked by conditional dependencies; responses drive the display and flow of later questions. | System | – | Answer drives next question | Context-sensitive flow | none approved (legacy Questions.Predecessor – S4) | UI-W-16 (no conditional logic in S5) | S1 p.8 | **Needs Clarification.** S7/S8 say conditional visibility was "rejected" (unverified). → CQ-017 |
| FR-VQ-06 | Question attributes: Mandatory/Optional; Answer Type (Multiple Choice – checkbox; Single Choice – radio or dropdown; Text; Image capture/upload); predefined answer options; Previous Data Reference (pre-fill / display previous data). | Admin | – | Configure question | Question rendered accordingly | none approved (legacy Questions.IsRequired, AnswerTypeId, RetrievePrevRecords – S4) | UI-W-16 | S1 p.8 | **Needs Clarification.** S5 offers 11 answer types (adds Number, Rating, GPS Log, Signature, Data Set Input). S1 has no numeric type although the visit flow captures numbers. → CQ-018 |
| FR-VQ-07 | The system shall replicate all currently defined questionnaires. | PMS (configuration) | Existing questionnaires supplied | Configure the existing forms | Parity with current forms | – | – | S1 p.9 | **Needs Clarification.** Source forms not supplied. → CQ-019 |
| FR-VQ-08 | Questionnaire versioning and preservation of historical responses. | Admin / System | – | – | – | none approved | UI-W-16 (S5: edits in place, hard delete, "Duplicate" only) | Not in S1. S6 notification mentions an "updated farm visit questionnaire"; S7 states versioning (unverified) | **Missing** → CQ-016 |

## 8. Visit Data Capture – Questionnaire Flow (S1 p.9–11)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-VC-01 | During a visit, the system guides the user through a structured questionnaire based on the selected visit type and farm type. | ASM | Farm active (FR-FM-06) | Steps 1–5 below | Complete visit record | none approved (legacy VisitHeader, VisitDetail – S4) | UI-M-09 | S1 p.9 | Confirmed |
| FR-VC-02 | Visit Initialization: select Visit Type **Farm** or **Dealer**. | ASM | – | Choose type | Type recorded | none approved | UI-M-09 | S1 p.9 | Confirmed (S6 does not save it → UI gap) |
| FR-VC-03 | If Dealer: enter Dealer Name, select Used Feed Type, enter Feed Volume. | ASM | Visit Type = Dealer | Enter dealer fields | Dealer data recorded | none approved | UI-M-09 | S1 p.9 | **Needs Clarification.** Unit of Feed Volume; whether Dealer Name refers to a dealer farm record (FR-FM-02); whether batch / farm-type steps apply to dealer visits. → CQ-029 |
| FR-VC-04 | The system captures the GPS location of the visit. | System / ASM | Device location available | Capture coordinates | Visit location stored | none approved (legacy VisitDetail.GPSLat/GPSLong – S4) | UI-M-09 | S1 p.9; S2 p.4 ("GPS location capture for each farm visit") | Confirmed. Capture points (start / end), accuracy, validation and fallback are not defined → CQ-026 |
| FR-VC-05 | Select Farm Type: Broiler, Layer, Dairy, Mixed. | ASM | – | Choose farm type | Questionnaire adapts (FR-VC-13) | none approved | UI-M-09 | S1 p.9 | **Needs Clarification.** Per-visit selection vs farm attribute; S5 Farm Type master seeds Broiler / Layer / **Breeder**. → CQ-022 |
| FR-VC-06 | Batch Selection / Creation (all farm types): select an existing **open** batch or create a new one (Batch Start Date, Initial Batch Size, Breed). | ASM | Farm Type selected | Select / create batch | Visit linked to batch(es) | none approved (no Batch table in S4) | UI-M-09, UI-M-04 | S1 p.9 | Confirmed (one or many batches per visit not stated; S6 allows several → CQ-021) |
| FR-VC-07 | Farm-type specific capture. **Broiler:** Current Batch Size, Average Body Weight (kg), Production (if applicable), Cumulative Mortality Count. **Layer:** Current Batch Size, Current Feed per Animal (g), Production (Egg Count), Cumulative Mortality Count. **Dairy / Mixed:** Current Batch Size, Current Feed per Animal (g), Cumulative Mortality Count. | ASM | Batch selected | Enter metrics | Batch metrics stored | none approved | UI-M-09 | S1 p.10 | **Needs Clarification.** Broiler has no feed input although FCR needs cumulative feed (FR-BM-04); "Production (if applicable)" for Broiler is undefined. → CQ-023 |
| FR-VC-08 | Common: Feed Type (Prima / F/C, CIC, Anthonys, New Hope, Cosmo, Super, Self, Other + specify) and Feed Form (Mash, Crumble, Pellet). | ASM | – | Select | Feed data stored | none approved | UI-M-09 | S1 p.10–11 | Confirmed (S5/S6 lists differ → CQ-045) |
| FR-VC-09 | Housing Type: Closed, Open, Battery Cages, Other + specify. | ASM | – | Select | Stored | none approved | UI-M-09 | S1 p.11 | Confirmed (S6 uses per-farm-type lists → CQ-044) |
| FR-VC-10 | Environmental Data: enter Water Availability. | ASM | – | Enter | Stored | none approved | UI-M-09 | S1 p.11 | **Needs Clarification.** Free entry (S1 "Enter") vs a configured list (S2 p.5; S5/S6 lists differ). → CQ-044 |
| FR-VC-11 | Additional inputs: Farm Management Grading (if applicable); Complaints & Observations; Recommendations & Comments; capture and upload Images. | ASM | – | Enter / capture | Stored with visit | none approved (legacy QuesAnswerImage – S4) | UI-M-09 | S1 p.11 | **Needs Clarification.** "If applicable" and the grading scale are undefined (S6 A/B/C; S2 p.5 configurable grading). → CQ-043 |
| FR-VC-12 | Image capture is mandatory for selected farm types. | System | – | Block submission without image for those types | – | none approved | UI-M-09 | S2 p.5 only | **Needs Clarification** (which farm types) → CQ-043 |
| FR-VC-13 | The questionnaire adjusts dynamically to the selected Farm Type. | System | – | – | Only relevant fields shown | none approved | UI-M-09 | S1 p.11 | Confirmed |
| FR-VC-14 | Mandatory validations are enforced where applicable. | System | – | Validate before save / submit | Incomplete visits blocked | none approved | UI-M-09 | S1 p.11 | **Needs Clarification.** The validation rules themselves are not listed (see Business_Workflow_Analysis WF-05). |
| FR-VC-15 | Captured data is stored and used for reporting and analysis. | System | – | – | Data available to FR-RP-xx | none approved | – | S1 p.11 | Confirmed |
| FR-VC-16 | Planned and unplanned farm visits. | ASM | Planned: on approved itinerary *[A]* | Start visit from plan, or ad hoc | Visit type (planned / unplanned) recorded | none approved | UI-M-08 | S1 p.7; S2 p.4, p.8 | Confirmed (precondition → CQ-012) |
| FR-VC-17 | Visit lifecycle: start / end, draft / resume, submission, immutability, correction. | ASM / System | – | – | – | none approved | UI-M-08, UI-M-09 | S1 silent. S6 "Continue Visit" exists but does not resume; S7 states resume / immutable / correction (unverified) | **Missing** → CQ-041 |
| FR-VC-18 | Visit review / approval / rejection by SM or Admin. | SM, Admin | – | – | – | none approved | UI-W-22 (S5 shows visits "Approved / Pending" in a report) | S1 silent; S2 silent | **Missing** → CQ-013 |
| FR-VC-19 | Farm visit report sharing with farmers (and read tracking). | System | – | Send report → track read | – | none approved (legacy VisitReportReadLog – S4) | UI-W-22 ("Farmer Visit Report Notification" menu only) | S2 p.4, p.5; S4 item 34.0 | **Needs Clarification** → CQ-040 |

## 9. Batch Monitoring & Calculations (S1 p.11–12; S2 p.5)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-BM-01 | Multiple batches per farm; a new batch can be created without closing existing ones. | ASM | Farm active | Create batch | Several open batches per farm | none approved | UI-M-04, UI-M-09 | S1 p.11; S2 p.5 | Confirmed |
| FR-BM-02 | Previously entered batch data is available for selection and update (carry-forward across visits). | ASM | Batch open | Select batch → prior values available | Continuity across visits | none approved | UI-M-09 | S1 p.11; S2 p.5 | Confirmed |
| FR-BM-03 | Batch age is calculated from the flock start date. Units: Broiler – days; Layer – weeks; Dairy – months. | System | Start date known | Calculate | Age displayed / stored | none approved | UI-M-09, UI-W-12 | S1 p.12 | **Needs Clarification.** Unit for Mixed farms undefined. → CQ-023 |
| FR-BM-04 | FCR = Cumulative Feed ÷ Mean Body Weight. | System | Inputs captured | Calculate | FCR per batch | none approved | UI-M-09, UI-W-12 | S1 p.1, p.12; S2 p.4 | **Confirmed by clarification response:** retain the SRS formula as written (CQ-023); do not substitute the prototype formula. Any missing input/unit definitions in the SRS remain open. |
| FR-BM-05 | Mortality = Dead animals ÷ Initial count. | System | Inputs captured | Calculate | Mortality rate per batch | none approved | UI-M-09, UI-W-12 | S1 p.12 | Confirmed (presentation as % *[A]*; S6 treats the "Cumulative Mortality Count" input as a per-visit increment → CQ-023) |
| FR-BM-06 | Calculations update dynamically when data changes (automated recalculation of FCR and mortality on data updates). | System | Data changed | Recalculate | Up-to-date metrics | none approved | – | S1 p.12; S2 p.5 | **Needs Clarification.** Whether later visits and reports are recalculated when an earlier value changes. → CQ-023 |
| FR-BM-07 | Batch / flock creation, update and **closure**; active and closed batches per farm. | ASM (mobile) *[A]* | Batch open | Close batch | Batch closed and kept in history | none approved | UI-M-04 (S6 "Close" with no data), UI-W-12 (read-only) | S2 p.5 (S1 says only "open batch") | **Partially confirmed:** one breed per batch (CQ-021). Batch closure, reopening and lifecycle rules are not answered and remain open. |
| FR-BM-08 | Batch-level performance tracking and historical tracking of batch performance. | SM, Admin, ASM | – | View batch history | Performance history | none approved | UI-W-12, UI-W-22 | S2 p.5; S1 p.13 | Confirmed |
| FR-BM-09 | Production % (layers) and other derived production metrics. | System | – | – | – | none approved | UI-M-09 | S6 only ("Production %") | **Needs Clarification** → CQ-023 |

## 10. Expense Management (S1 p.13)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-EX-01 | The web app defines and manages Expense Types categorised as **Fixed** and **Variable**. | Administrator *[A: S1 does not name the actor]* | – | Create / edit / deactivate expense type | Types available on mobile | none approved (legacy ExpensTypes – S4) | UI-W-15 | S1 p.13; S2 p.5 | Confirmed |
| FR-EX-02 | Fixed expenses have a predefined amount configured in the system; variable expenses take the amount at data entry. | Administrator | – | Configure amount for Fixed | – | none approved | UI-W-15 | S1 p.13 | Confirmed |
| FR-EX-03 | Each expense type has an option indicating whether image proof is mandatory. | Administrator | – | Set flag | Enforced on mobile | none approved (flag absent in legacy table – S8 DB-018) | UI-W-15 | S1 p.13 | Confirmed |
| FR-EX-04 | Mobile: ASMs record expenses during field visits. Fixed amount auto-populated and not editable; variable amount entered by the ASM; image captured and uploaded when required. | ASM | Expense types synced | Select type → amount → image (if required) → save → sync | Expense submitted | none approved (legacy UserWiseExpenses – S4) | UI-M-02 (dialog), UI-M-10 (orphan) | S1 p.13 | Confirmed. Whether the expense must link to a visit ("during field visits") → CQ-030. S6 has no image capture → UI gap. |
| FR-EX-05 | The ASM enters expense data "subjected [to] predefined criteria". | ASM | – | – | – | – | – | S1 p.5 | **Needs Clarification.** The criteria (limits, frequency, eligibility) are undefined. → CQ-030 |
| FR-EX-06 | All ASM expenses go through an **Expense Approval** workflow on the web; authorized users (e.g., Sales Managers / Administrators) review, approve or reject them. | SM, Admin | Expense synced | Review → approve / reject | Final decision recorded | none approved | UI-W-18 | S1 p.13 | Confirmed (approver scope → CQ-031) |
| FR-EX-07 | For variable expenses, the approver may modify or confirm the reimbursable amount during approval. | SM, Admin | Variable expense pending | Adjust amount → approve | Reimbursable amount recorded | none approved | UI-W-18 (no amount edit in S5) | S1 p.13 | **Confirmed by clarification response:** the approver may modify the variable expense amount (CQ-030). |
| FR-EX-08 | Rejection reason; cancellation / resubmission of rejected or pending expenses. | SM, Admin, ASM | – | – | – | none approved | UI-W-18 (reason mandatory in S5), UI-W-19 ("cancelled" status) | S1 silent | **Missing** → CQ-030 |
| FR-EX-09 | Financial processing, accounting, payroll and automated reimbursement are **out of scope**. | – | – | – | Approval outcome only | – | – | S2 p.7 | Confirmed (scope boundary) |

## 11. Reporting & Analytics (S1 p.13–14; S2 p.6)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-RP-01 | **Farm-Level Reports:** visit history, production data, feed usage, mortality rates and overall performance trends per farm. | Admin, SM, ASM (web) | Data captured | Select filters → generate | Report | reads visit / batch data (none approved) | UI-W-22 | S1 p.13 | Confirmed (columns and filters undefined → CQ-033) |
| FR-RP-02 | **Batch-Level Reports:** batch lifecycle status, FCR, mortality and historical comparison across visits. | Admin, SM, ASM | – | Generate | Report | none approved | UI-W-22 | S1 p.13 | Confirmed (→ CQ-033) |
| FR-RP-03 | **Supervisor and Area-Based Reports:** aggregated by Supervisors, Regions and Territories (coverage, efficiency). | Admin, SM | – | Generate | Report | none approved | UI-W-22 | S1 p.14 | **Confirmed by clarification response:** Supervisor means ASM (CQ-003). Report access remains governed by the clarified role visibility rules. |
| FR-RP-04 | Generate and view reports and dashboards (Admin, SM, ASM). | Admin, SM, ASM | – | Open dashboard | Role-specific KPIs | none approved | UI-W-02, UI-W-03, UI-M-02 | S1 p.4–5 | Confirmed (KPIs undefined → CQ-033) |
| FR-RP-05 | ASM-wise and area-wise reporting; farm, batch and visit-level reports; feed type comparison reports; output vs input dashboards; map-based visualization of visits with visit sequence; filters by Date, Farm Type and Feed Type. | Admin, SM | – | – | – | none approved | UI-W-22 | S2 p.6 | **Needs Clarification** (S2 items not in S1) → CQ-033 |
| FR-RP-06 | Reporting Parameter Management: define parameters and parameter range criteria for analytic reports. | Admin *[A]* | – | – | – | none approved | none | S2 p.9 | **Confirmed by clarification response:** SRS-listed reports only (CQ-033); prototype and legacy-only reports are excluded. |
| FR-RP-07 | Named legacy reports: Visit Summary, Visit Detail, Farm Monitor Visit (Process 1), Farm Evaluation (Process 2), Sync Status, Sync History, Visit Plan, Expense, Feedback. | Admin, SM | – | – | – | none approved | UI-W-22 (menus only) | S4 item 14.0; S5 | **Confirmed by clarification response:** Farm Monitoring only; Farm Evaluation is excluded (CQ-014)., CQ-033 |
| FR-RP-08 | Export / print of reports (S5 shows Excel and PDF buttons). | Admin, SM | – | – | – | – | UI-W-22 | S1 silent | **Missing** → CQ-033 |

## 12. Audit & Logging (S1 p.14)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-AU-01 | Maintain an audit trail of: User Creation, Modification, Login, Logout; Farm Creation, Modification; Approvals; Role-Based Access Changes. | System | – | Write audit record on each event | Complete trail | LoginHistory (S3) covers login / logout only; no audit log entity approved (→ DBG-008) | UI-W-23 | S1 p.14 | Confirmed |
| FR-AU-02 | Each audit record includes: User, Date & Time, Action Performed, Module, Previous Value, Updated Value (where applicable). | System | – | – | Before / after values stored | none approved (legacy AuditLog lacks previous / updated values – S4/S8) | UI-W-23 (S5 has no before / after) | S1 p.14 | Confirmed |
| FR-AU-03 | Audit logs are read-only and accessible only to authorised users. | Administrator (S1 p.4) | – | View / filter | No update / delete possible | none approved | UI-W-23 | S1 p.4, p.14 | Confirmed |
| FR-AU-04 | Audit all user login attempts and access to sensitive data. | System | – | – | – | LoginHistory (S3) | UI-W-23 | S1 p.17 (Security NFR) | Confirmed ("sensitive data" undefined → CQ-035) |
| FR-AU-05 | Audit history of **all data changes**, with previous / updated values and user / timestamp. | System | – | – | – | none approved | UI-W-23 | S2 p.5–6 ("audit logging of all data changes") | **Confirmed by clarification response:** audit only the events listed in the SRS (CQ-035). Do not extend to all data changes unless an approved change request is raised. |
| FR-AU-06 | Audit retention period and archival. | – | – | – | – | – | – | Not stated | **Missing** → CQ-035 |

## 13. Notifications & Approvals (S1 p.14–15; S2 p.6)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-NT-01 | The system automatically generates in-app notifications for important system events. | System | Triggering event | Create notification for recipients | Notification delivered in-app | none approved (legacy Notification – S4) | UI-M-12, UI-W-21 (bell) | S1 p.14 | Confirmed (S2 p.9 also lists "Email gateways" → CQ-032) |
| FR-NT-02 | ASM (mobile) notifications: Visit Plan Approvals, New Farm Approvals, Expense Approvals. | ASM | Decision recorded | Notify ASM | ASM sees outcome | none approved | UI-M-12 | S1 p.14 | Confirmed (rejection outcomes implied *[A]*) |
| FR-NT-03 | SM (web) notifications: Pending Visit Plan Approvals, Pending New Farm Approvals, Pending Expense Approvals. | Sales Manager | Item pending | Notify SM | SM sees pending items | none approved | UI-W-21 (bell), UI-W-03 | S1 p.14–15 | Confirmed (Administrator notifications not stated → CQ-032) |
| FR-NT-04 | Users can view notification history within the application. | All | – | Open history | History visible | none approved | UI-M-12, UI-W-21 | S1 p.15 | Confirmed |
| FR-NT-05 | Approval status visible in both mobile and web. | ASM, SM, Admin | – | – | Status visible | none approved | UI-M-02, UI-M-05, UI-W-17, UI-W-18 | S2 p.6 | Confirmed (consistent with S1) |
| FR-NT-06 | Administrator-authored notifications / messages to field users (with pictures, response required, due date) and images shown as pop-ups on mobile. | Administrator | – | Create → target recipients → track read / response | – | none approved (legacy Notification, NotiifcationImage – S4) | UI-W-21, UI-M-12 | S2 p.5 (pop-up images); S4 items 11.0, 23.0; S5; S6 | **Confirmed by clarification response:** in-app notifications only (CQ-032). Email, SMS and external notification channels are excluded. |
| FR-NT-07 | Alerts for upcoming visits, unclosed flocks, etc. | System | – | – | – | none approved | – | S2 p.14 ("Expected Outcomes") | **Confirmed by clarification response:** in-app notifications only (CQ-032). Email, SMS and external notification channels are excluded. |

## 14. Offline Capability (S1 p.15)

| ID | Requirement | Actor | Preconditions | Main flow | Expected outcome | DB entities | UI | Source | Status |
|---|---|---|---|---|---|---|---|---|---|
| FR-OF-01 | The mobile app supports offline operation when connectivity is unavailable (offline-first). | ASM | Initial sync completed *[A]* | Capture data offline → store locally | No data loss | none approved (legacy SyncHistory, SyncSchedule – S4) | UI-M-13 | S1 p.3, p.15; S2 p.4 | Confirmed |
| FR-OF-02 | **Initial Sync:** user-initiated at start of day; downloads latest master **and transactional** data. | ASM | Connectivity | Run Initial Sync | Device ready for the day | none approved | UI-M-13 | S1 p.15 | Confirmed ("transactional data" content undefined; S6 downloads masters only → CQ-024) |
| FR-OF-03 | **Midday Sync:** user-initiated during the day; uploads new data and retrieves critical updates; also the forced-sync mechanism. | ASM | Connectivity | Run Midday Sync | Data uploaded; critical updates received | none approved | UI-M-13 | S1 p.15 | Confirmed ("critical updates" undefined → CQ-024) |
| FR-OF-04 | **Automatic Sync:** background sync triggered by network availability, time interval or system events; automatic sync when connectivity is restored. | System | – | Detect trigger → sync | Data uploaded without user action | none approved | UI-M-13 (not in S6) | S1 p.15; S2 p.4 | Confirmed (intervals and events undefined → CQ-024) |
| FR-OF-05 | **Day-End Sync:** user-initiated at end of day; uploads all pending transactions so that daily activities are "properly completed or paused". | ASM | – | Run Day-End Sync | Day closed | none approved | UI-M-13 | S1 p.15 | **Confirmed by clarification response:** no day-end lock (CQ-025). The SRS day-end sync requirement remains, but it must not lock the user's day or require an administrator unlock. |
| FR-OF-06 | In offline mode, GPS capture may have reduced accuracy. | System | – | – | Accepted limitation | – | UI-M-09 | S1 p.15 | Confirmed (acceptance / flagging policy → CQ-026) |
| FR-OF-07 | Conflict detection and resolution for data changed both on the device and centrally. | System | – | – | – | none approved | none (S6 only "Reconciling records…") | Not in S1 | **Confirmed by clarification response:** server wins when resolving offline/online data conflicts (CQ-024). Conflict audit and retry details remain open. |
| FR-OF-08 | Device registration / configuration and mobile maintenance functions (initial configuration, send DB, bug report, delete DB, special sync, app update). | ASM, Admin | – | – | – | none approved (legacy Settings, Config – S4) | UI-M-14, UI-W-24 | S4 items 3.0, 19.0, 20.0; S5; S6 | **Confirmed by clarification response:** exclude all features that are outside the SRS (CQ-034). |

## 15. Requirements found outside the SRS (S2, S4, S5, S6, S7)

These items appear in the Proposal, legacy feature list or prototypes but **not in the SRS**. They are retained below only as a historical inventory; the clarification response excludes them from the current requirements baseline (CQ-034).

| ID | Requirement (as evidenced) | Evidence | Status |
|---|---|---|---|
| FR-XT-01 | **Attendance:** ASM marks daily attendance (check-in / check-out). | S4 item 24.0; S6 prototype; S7 unverified RTM | **Excluded – outside SRS (CQ-034)** |
| FR-XT-02 | **Feedback / farmer complaints:** record and track farmer feedback. | S4, S5, S6 prototype evidence | **Excluded – outside SRS (CQ-034)** |
| FR-XT-03 | **Company master** (ERP code, registration numbers, GPS). | S4; S5 prototype | **Excluded – outside SRS (CQ-034)** |
| FR-XT-04 | **Farm Monitor vs Farm Evaluation processes** and farm performance scoring. | S4; S3; S7 unverified RTM | **Farm Monitoring only; Farm Evaluation excluded (CQ-014)** |
| FR-XT-05 | **Role-based position accounts** reassignable between people. | S5 prototype | **Excluded; transactions belong to individual users (CQ-038)** |
| FR-XT-06 | **Day-End lock and Administrator unlock.** | S4; S7 unverified RTM | **Excluded – no day-end lock (CQ-025)** |
| FR-XT-07 | **Visit types** beyond the SRS-defined visit fields. | S5 prototype | **Excluded – outside SRS (CQ-034)** |


## 16. Clarification response application (CQ-001–CQ-046)

The following records how the user's clarification responses have been applied. These are working requirements decisions for analysis; they do not replace a signed SRS amendment or formal change request.

| CQ | Applied response | Remaining point / dependency |
|---|---|---|
| CQ-001–003 | Roles are Admin, SM and ASM; Supervisor = ASM; SM approves itinerary, farm and expense workflows. | Confirm role terminology in formal stakeholder sign-off. |
| CQ-004 | No undocumented features or decisions may override the signed SRS / approved change requests. | Confirm signed baseline and approved change-control register. |
| CQ-005 | User response says both are approved; the supplied copies still have blank signature/date fields, so retain the user's confirmation but obtain the formal approval evidence for the project file. | Obtain signed copies / approval evidence for the project record. |
| CQ-006–007 | Geography: Province → District → Region → Territory; assign ASM to Regions and Territories. | Assignment cardinality, overlap and history rules. |
| CQ-008–012 | Daily plan; one Territory/day with selected farms; recurring plan defined by ASM and approved by SM; unplanned visits allowed with post-visit approval; assigned-area restriction remains. | Recurrence rules and the separate post-visit approval process. |
| CQ-013–015 | Submitted visit is final; Farm Monitoring only; visit fields are fixed system fields. | Clarify how fixed system fields coexist with the configurable Visit Questionnaire. |
| CQ-016–020 | New questionnaire version for every published change; conditional questions required; Figma question types retained; approved forms to be supplied; Admin configures mandatory/validation per question. | Form content and exact validation rules pending approved form pack. |
| CQ-021–023 | One breed per batch; Farm Type is a farm-master attribute; calculations use SRS formulas. | Batch lifecycle beyond breed cardinality; Farm Type value list; any formula gaps in SRS. |
| CQ-024–026 | Server wins sync conflicts; no day-end lock; GPS captured at visit start only. | Conflict logging/retry behaviour and GPS accuracy/exception policy. |
| CQ-027–029 | GPS required during mobile farm registration; mobile-created farms require SM approval; one Main Farm can have unlimited Dealer Farms. | Web-created farm approval and farm/dealer relationship attributes. |
| CQ-030–033 | Approver may modify variable expense amount; only assigned ASM's SM approves; in-app notifications only; SRS reports only. | Expense eligibility/limits and detailed report layouts/filters where SRS is silent. |
| CQ-034–038 | Exclude all out-of-SRS features; audit SRS-listed events only; reversible AES-256-GCM subject to security approval; SRS user fields only; transactions belong to individual users. | Security approval; exact Module 01 schema reconciliation. |
| CQ-039–046 | Breeder Farm is a separate supplier/master entity; no external sharing; multiple visits per farm/day; retain SRS targets; Figma A/B/C grading; use SRS lists/feed list; SM sees assigned ASMs only. | Breeder master attributes/relationships; grading definitions; confirm any ambiguity in SRS list values. |

### 16.1 Explicit exclusions

The following are excluded from the requirements baseline unless an approved change request is raised:

- Farm Evaluation process and associated scoring/reporting.
- Attendance, feedback/complaints, company master and ERP integration.
- External farmer sharing or farmer-facing access.
- Email/SMS notifications and other external notification channels.
- Day-end locking and administrator unlock.
- Legacy-only reports, prototype-only functions and other features not present in the SRS.
- Any additional audit events beyond the event list in the SRS.

### 16.2 Open dependencies and contradictions

1. **Visit field model:** CQ-015 says visit fields are fixed system fields, while CQ-016–020 require versioned, configurable conditional questionnaires. The boundary between fixed visit data and configurable questionnaire content must be defined before final schema/API design.
2. **Unplanned visit approval:** CQ-012 requires post-visit approval, while CQ-013 says visit submission is final. Treat the visit record as immutable after submission; the post-visit approval should be a separate decision/status record unless stakeholders specify otherwise.
3. **Geographic assignment:** CQ-007 confirms assignments at Region and Territory levels but does not define whether multiple ASMs may share a Territory, whether Region assignment automatically grants all child Territories, or how reassignment history works.
4. **Recurring plans:** CQ-010–011 confirm ASM ownership and SM approval but do not define recurrence patterns, generated date range, editing/cancellation or whether approval is per occurrence or per series.
5. **Security:** CQ-036 requests reversible AES-256-GCM but makes it subject to security approval. Do not implement password reversibility; confirm whether the decision applies to a separate credential/secret field or to passwords. Passwords should remain protected using an approved password-hashing design.
6. **Farm creation:** CQ-028 confirms SM approval for mobile-created farms. The approval route for web-created farms and rejection/resubmission behaviour remain unspecified.
7. **Batch model:** CQ-021 confirms one breed per batch but does not resolve batch closure/reopening, required lifecycle dates or other batch-level validation.
8. **Breeder Farm:** CQ-039 establishes a separate supplier/master entity, but attributes and its link to breed, batch and supplier records remain unspecified.
9. **Grading:** CQ-043 selects Figma A/B/C, but the meaning, thresholds and applicable farm/batch types need confirmation.
10. **NFR compatibility:** CQ-042 says retain SRS targets as written. The platform/browser compatibility statements that appear inconsistent within the SRS should be transcribed as-is and flagged for stakeholder confirmation rather than silently reconciled.

## 17. Cross-cutting observations

1. **Two descriptions of visit capture.** S1 p.8–9 describes a configurable questionnaire (FR-VQ-xx). S1 p.9–11 then describes a fixed, field-level flow (FR-VC-02..11). S6 implements the fixed flow for capture and shows a sectioned questionnaire only in visit history. Whether the fixed fields are system fields or questionnaire content is the most important design question for visits and batches (CQ-015).
2. **Role model.** S1 (ASM visits, SM approves), S2 (Supervisor visits, ASM approves) and S5 (ASM approves Supervisors' plans and expenses) describe different operating models (CQ-001).
3. **Undocumented decisions.** S7/S8 contain unverified decisions. Per CQ-004, they do not override the SRS; out-of-SRS items are excluded unless covered by an approved change request.
4. **Geography.** The clarified hierarchy is Province → District → Region → Territory. Assignment cardinality and history rules remain open (CQ-006–CQ-007).
5. **Batch.** Central to S1 and S2, yet absent from the legacy data model (S4) and inconsistent between the two S6 screens (CQ-021).

## 18. Non-functional requirements (summary, S1 p.17–18)

| ID | Requirement | Status / note |
|---|---|---|
| NFR-01 | Response to user input within 2 s; average response time below 1.5 s | Confirmed |
| NFR-02 | ≥1000 concurrent users; ≥100 transactions/minute | Confirmed (sizing assumption to validate against the expected user base → CQ-042) |
| NFR-03 | Recovery within 5 minutes after failure | Confirmed |
| NFR-04 | Authentication with ≥128-bit encryption; RBAC; audit of login attempts and sensitive-data access | Confirmed (relationship to reversible password storage, FR-UM-10 → CQ-036) |
| NFR-05 | Company coding standards; ≥80% automated unit-test coverage; version control | Confirmed (coverage scope → CQ-042) |
| NFR-06 | Compatibility: Windows 8+, macOS 11, Ubuntu 20.04; latest Chrome, Firefox, Edge (S1 p.3 lists Chrome and Edge only) | Needs Clarification → CQ-042 |
| NFR-07 | WCAG 2.0 accessibility | Confirmed |
| NFR-08 | Horizontal scalability; server CPU ≤70% at peak | Confirmed |
| NFR-09 | Daily automated backups; quarterly backup / recovery tests | Confirmed |
| NFR-10 | Mobile platform: Android, offline capability | Confirmed (S1 p.3) |
| NFR-11 | Encryption at rest and in transit | Confirmed (S1 p.3) |
| NFR-12 | Deployment on-premises with Dev / Test / Production separation; document storage on-premises | Confirmed (S2 p.9) |

## 19. Summary counts

> **Note:** The v0.1 module counts are not carried forward as authoritative counts because several requirement statuses have changed and some items are now explicitly excluded. Recalculate counts after the detailed rows and remaining open questions are validated.

| Module (v0.1 baseline; not recalculated) | Total | Confirmed | Needs Clarification | Missing |
|---|---|---|---|---|
| User Management (UM) | 13 | 9 | 4 | 0 |
| Area & Hierarchy (AH) | 6 | 1 | 5 | 0 |
| Farm Management (FM) | 12 | 6 | 3 | 3 |
| Master Data (MD) | 4 | 2 | 2 | 0 |
| Itinerary Planning (IP) | 10 | 3 | 4 | 3 |
| Visit Questionnaire (VQ) | 8 | 2 | 5 | 1 |
| Visit Data Capture (VC) | 19 | 9 | 8 | 2 |
| Batch Monitoring (BM) | 9 | 4 | 5 | 0 |
| Expense Management (EX) | 9 | 6 | 2 | 1 |
| Reporting & Analytics (RP) | 8 | 3 | 4 | 1 |
| Audit & Logging (AU) | 6 | 4 | 1 | 1 |
| Notifications (NT) | 7 | 5 | 2 | 0 |
| Offline (OF) | 8 | 5 | 2 | 1 |
| Outside SRS (XT) | 7 | 0 | 7 | 0 |
| **Total functional** | **126** | **59** | **54** | **13** |

Non-functional: 12 (NFR-01..12). CQ-042 instructs that the SRS targets remain as written; internal SRS inconsistencies are retained as open validation items.
