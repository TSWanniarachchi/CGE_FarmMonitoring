# CGE Farm Monitoring System -- Module 02 Database Specification

## Geography & Assignment

  ----------------------------------- ------------------------------------------------
  Document                            `Farm_FMS_Module_02_Database_Specification.md`

  Module                              **02 -- Geography & Assignment**

  Version                             **0.1 -- Draft for technical and stakeholder
                                      review**

  Date                                05-11-2026

  Status                              **Proposed specification only -- not approved
                                      for implementation.** No database objects,
                                      migrations or Stored Procedures are authorized
                                      by this document.

  Database                            `CGE_FarmMonitoring`

  Schema                              `dbo` only (AD-24)

  Related requirements                FR-AH-01--FR-AH-05; FR-FM-01;
                                      FR-IP-01--FR-IP-03; relevant reporting
                                      requirements

  Related decisions                   CQ-006, CQ-007, CQ-031, CQ-046; AD-04, AD-06,
                                      AD-07, AD-08, AD-14, AD-24

  Prerequisite                        Module 01 Database Specification v0.2 --
                                      approved baseline for Security & Access

  Related documents                   `Database_Gap_Analysis.md` v0.2;
                                      `Functional_Requirements_Analysis.md`;
                                      `Business_Workflow_Analysis.md`;
                                      `Database_Conventions.md`;
                                      `FarmFMS_Architecture_Decisions_v2.md`
  ----------------------------------- ------------------------------------------------

> **Important:** This document translates confirmed geography and
> assignment requirements into a reviewable database specification
> outline. It deliberately marks unresolved cardinality, history and
> mapping rules as pending. The conceptual entities and object names
> below are proposals, not approved schema. Do not generate EF Core
> entities, migrations, SQL scripts or Stored Procedures from this draft
> until the open decisions are signed off.

------------------------------------------------------------------------

## 1. Purpose and scope

Module 02 provides the geographical master structure and the
relationship between field users (ASM) and their assigned operating
areas.

### 1.1 In scope

-   Maintain the confirmed geographical hierarchy:
    -   Province
    -   District
    -   Region
    -   Territory
-   Maintain Region-to-Territory membership.
-   Maintain ASM assignments to Regions and Territories, subject to the
    unresolved assignment rules in §8.
-   Provide geography and assignment lookups for farm management,
    itinerary planning, access filtering and reports.
-   Validate whether an ASM is assigned to a Region/Territory before
    allowing scoped operations.
-   Preserve assignment history if confirmed by the decision owner;
    historical behaviour is currently unresolved.

### 1.2 Out of scope

-   Main Route, Sub Route and Area hierarchy. These appear in
    legacy/Figma material but are not part of the confirmed hierarchy.
-   Farm master data itself. Farm records belong to the Farm module;
    this module provides geography references.
-   Itinerary, Visit, Batch and Expense transactions.
-   User, Role, UserHierarchy and authentication management, which
    belong to Module 01.
-   Attendance, Feedback, Day-End lock and other features excluded from
    the SRS.
-   External geography integrations or map services; no such integration
    is approved by the sources reviewed.

------------------------------------------------------------------------

## 2. Source requirements and confirmed decisions

### 2.1 Functional requirements

  -----------------------------------------------------------------------------
  Requirement             Source statement /      Database implication
                          intent                  
  ----------------------- ----------------------- -----------------------------
  FR-AH-01                Multiple ASMs report to User reporting hierarchy is
                          a Sales Manager.        already represented by Module
                                                  01 `UserHierarchy`; Module 02
                                                  must not duplicate it.

  FR-AH-02                SM has visibility and   Assignment and reporting
                          oversight across        scope must be available to
                          assigned ASMs.          authorization/query
                                                  procedures. CQ-046 restricts
                                                  SM visibility to assigned
                                                  ASMs.

  FR-AH-03                Farms are organized by  Geography master data must
                          Regions and             represent Region and
                          Territories; each       Territory membership.
                          Region contains         
                          multiple Territories.   

  FR-AH-04                Each ASM is assigned to ASM-to-Region and
                          one or more Regions and ASM-to-Territory assignment
                          manages associated      is required, but exact
                          Territories within      cardinality and inheritance
                          them.                   rules remain open.

  FR-AH-05                ASMs operate only       Assignment validation must be
                          within assigned Regions callable by farm, itinerary,
                          and corresponding       visit and reporting
                          Territories.            operations.

  FR-FM-01                Farm master includes    Farm module must reference
                          Province, District,     the canonical geography
                          Region and Territory.   masters; ownership of the
                                                  Province/District-to-Region
                                                  mapping must be defined.

  FR-IP-02                One Territory per daily Territory identity must be
                          itinerary.              stable and available to
                                                  itinerary operations.
  -----------------------------------------------------------------------------

### 2.2 Confirmed clarification decisions

  -----------------------------------------------------------------------------
  CQ                      Confirmed decision      Applied interpretation
  ----------------------- ----------------------- -----------------------------
  CQ-006                  Province → District →   This is the canonical
                          Region → Territory      hierarchy for the project.
                                                  Legacy Area/Main Route/Sub
                                                  Route is not carried forward.

  CQ-007                  Assign ASM to Regions   Both assignment levels are
                          and Territories         required. The answer does not
                                                  define whether Territory
                                                  assignment is independent or
                                                  inherited through Region
                                                  assignment, nor
                                                  sharing/cardinality/history
                                                  rules.

  CQ-031                  Only the assigned ASM's Approval scope must be
                          SM approves             resolvable using Module 01
                                                  reporting hierarchy plus the
                                                  relevant ASM identity.

  CQ-046                  SM sees only assigned   SM-scoped data access must
                          ASMs                    not expose other SMs' ASMs.
  -----------------------------------------------------------------------------

------------------------------------------------------------------------

## 3. Design principles and dependencies

1.  **Module 01 is authoritative for users and reporting hierarchy.**
    Reuse its `AppUser`, `UserAccount`, `UserHierarchy` and approved
    `UserTerritory` definitions where applicable. Do not recreate user
    or SM/ASM entities.
2.  **Module 01 already contains `UserTerritory`.** Its approved
    structure links `UserAccount` and `Territory`. Any Region assignment
    requirement must be reconciled with this baseline. Do not silently
    replace, rename or change `UserTerritory`.
3.  **No physical foreign keys** are permitted (AD-07). Referential
    integrity and active-state checks must be implemented in Stored
    Procedures.
4.  **EF Core Code First** manages schema and migrations only (AD-04).
    It must not be used for application CRUD/query operations.
5.  **All application data operations use Stored Procedures** through
    the approved Dapper executor (AD-06).
6.  **`dbo` only** (AD-24). All procedure references are dbo-qualified.
7.  **Audit actor and timestamps** follow AD-14 and
    `Database_Conventions.md`: actor IDs are derived server-side;
    timestamps are UTC.
8.  **Naming** follows `Database_Conventions.md`: descriptive
    PascalCase, maximum SQL Server identifier length 128; no custom
    15-character limit; approved prefixes and abbreviations only.
9.  The Province/District/Region/Territory chain must be the same source
    of truth used by Farm, Itinerary and Reporting modules.

------------------------------------------------------------------------

## 4. Proposed logical data model

This section defines logical responsibilities only. Physical
table/column names, SQL types, nullability, constraints and indexes
remain pending approval.

### 4.1 Proposed logical entities

  -------------------------------------------------------------------------
  Logical entity    Responsibility    Status            Notes
  ----------------- ----------------- ----------------- -------------------
  Province          Stores the        Proposed          Required by CQ-006
                    top-level                           and FR-FM-01.
                    Province master.                    

  District          Stores District   Proposed          Each District
                    master records                      belongs to a
                    under a Province.                   Province, subject
                                                        to confirmation of
                                                        the one-parent
                                                        rule.

  Region            Stores            Proposed          Each Region belongs
                    operational                         to a District under
                    Regions.                            the confirmed
                                                        four-level
                                                        hierarchy.

  Territory         Stores            Proposed          FR-AH-03 states
                    Territories                         each Region
                    within a Region.                    contains multiple
                                                        Territories.

  UserRegion        Represents        Proposed /        CQ-007 requires
  assignment        assignment of an  decision          Region assignment;
                    ASM's Module 01   dependent         exact key and
                    user account to a                   relationship to
                    Region.                             existing
                                                        `UserAccount`
                                                        remain to be
                                                        approved.

  UserTerritory     Represents        Existing baseline `UserTerritory`
  assignment        assignment of an                    already exists in
                    ASM's Module 01                     approved Module 01
                    user account to a                   specification.
                    Territory.                          Module 02 must
                                                        consume/reconcile
                                                        it, not create a
                                                        duplicate.
  -------------------------------------------------------------------------

### 4.2 Relationship outline

``` text
Province
  └── District
        └── Region
              └── Territory

Module 01:
AppUser
  └── UserAccount
        ├── UserHierarchy (ASM → SM reporting relationship)
        └── UserTerritory (existing approved Territory assignment)

Module 02 proposal:
UserAccount (ASM)
  └── UserRegion assignment (proposed; approval required)

Farm module:
Farm → Province / District / Region / Territory
```

The diagram shows logical relationships, not approved physical foreign
keys. No physical FK constraints may be created.

### 4.3 Key relationship rules

  -------------------------------------------------------------------------------
  Relationship      Source support     Proposed rule     Approval status
  ----------------- ------------------ ----------------- ------------------------
  Province →        CQ-006             A District        Logical proposal;
  District                             belongs to a      confirm whether a
                                       Province.         District can ever move
                                                         between Provinces and
                                                         how history is handled.

  District → Region CQ-006             A Region belongs  Logical proposal;
                                       to a District.    confirm one-parent rule
                                                         and reassignment
                                                         behaviour.

  Region →          FR-AH-03, CQ-006   A Territory       Requirement-supported;
  Territory                            belongs to a      exact uniqueness/history
                                       Region; a Region  details pending.
                                       contains multiple 
                                       Territories.      

  ASM → Region      FR-AH-04, CQ-007   ASM can be        Requirement-supported;
                                       assigned to one   concurrent sharing and
                                       or more Regions.  history pending.

  ASM → Territory   Module 01          ASM can be        Existing Module 01
                    `UserTerritory`,   assigned to       baseline;
                    CQ-007             Territories.      cardinality/history
                                                         conflict needs
                                                         reconciliation.

  ASM → SM          FR-AH-01, Module   ASM reports to    Module 01
                    01 `UserHierarchy` one current SM,   responsibility; do not
                                       with history      duplicate in Module 02.
                                       preserved as      
                                       defined by Module 
                                       01.               
  -------------------------------------------------------------------------------

------------------------------------------------------------------------

## 5. Proposed data dictionary -- logical fields only

The following is a requirements-level field inventory. It intentionally
does not assign SQL data types, lengths, defaults, nullability or
physical column names before the Module 02 design decisions are
approved.

### 5.1 Province

  ----------------------------------------------------------------------------------
  Logical field     Meaning                Required?         Source / status
  ----------------- ---------------------- ----------------- -----------------------
  Province          Unique system identity Yes               Proposed technical key;
  identifier        for a Province.                          exact type follows
                                                             project database rules.

  Province name     Display name.          Yes               CQ-006 confirms
                                                             Province level; exact
                                                             validation/uniqueness
                                                             rule pending.

  Province code     Business code, if      Pending           No code format
                    required.                                confirmed in the
                                                             supplied decisions.

  Active status     Whether the Province   Proposed          Deactivation behaviour
                    can be selected for                      must be approved.
                    new                                      
                    assignments/records.                     

  Audit fields      Created/modified actor As required by    Apply approved audit
                    and UTC timestamps.    AD-14             convention.
  ----------------------------------------------------------------------------------

### 5.2 District

  ------------------------------------------------------------------------
  Logical field     Meaning            Required?         Source / status
  ----------------- ------------------ ----------------- -----------------
  District          Unique system      Yes               Proposed
  identifier        identity for a                       technical key.
                    District.                            

  Province          Parent Province.   Yes               Logical
  reference                                              relationship from
                                                         CQ-006.

  District name     Display name.      Yes               CQ-006;
                                                         uniqueness scope
                                                         pending.

  District code     Business code, if  Pending           Not defined by
                    required.                            supplied
                                                         decisions.

  Active status     Whether selectable Proposed          Deactivation
                    for new records.                     behaviour
                                                         pending.

  Audit fields      Created/modified   As required by    Apply approved
                    actor and UTC      AD-14             audit convention.
                    timestamps.                          
  ------------------------------------------------------------------------

### 5.3 Region

  ----------------------------------------------------------------------------
  Logical field     Meaning                Required?         Source / status
  ----------------- ---------------------- ----------------- -----------------
  Region identifier Unique system identity Yes               Proposed
                    for a Region.                            technical key.

  District          Parent District.       Yes               Proposed
  reference                                                  interpretation of
                                                             confirmed
                                                             hierarchy;
                                                             confirm exact
                                                             mapping.

  Region name       Display name.          Yes               FR-AH-03 /
                                                             CQ-006.

  Region code       Business code, if      Pending           Not defined by
                    required.                                supplied
                                                             decisions.

  Active status     Whether selectable for Proposed          Deactivation
                    new                                      behaviour
                    assignments/records.                     pending.

  Audit fields      Created/modified actor As required by    Apply approved
                    and UTC timestamps.    AD-14             audit convention.
  ----------------------------------------------------------------------------

### 5.4 Territory

  ----------------------------------------------------------------------------
  Logical field     Meaning                Required?         Source / status
  ----------------- ---------------------- ----------------- -----------------
  Territory         Unique system identity Yes               Existing Module
  identifier        for a Territory.                         01
                                                             `UserTerritory`
                                                             depends on
                                                             Territory
                                                             identity.

  Region reference  Parent Region.         Yes               FR-AH-03 /
                                                             CQ-006.

  Territory name    Display name.          Yes               FR-AH-03.

  Territory code    Business code, if      Pending           Not defined by
                    required.                                supplied
                                                             decisions.

  Active status     Whether selectable for Proposed          Deactivation
                    new                                      behaviour
                    assignments/records.                     pending.

  Audit fields      Created/modified actor As required by    Apply approved
                    and UTC timestamps.    AD-14             audit convention.
  ----------------------------------------------------------------------------

### 5.5 ASM-to-Region assignment (proposed)

  --------------------------------------------------------------------------
  Logical field     Meaning            Required?         Source / status
  ----------------- ------------------ ----------------- -------------------
  Assignment        Unique assignment  Proposed          Physical key
  identity          record identity.                     type/name pending.

  ASM user-account  Module 01 account  Yes               Reuse Module 01
  reference         being assigned.                      identity; exact
                                                         relationship must
                                                         be reconciled with
                                                         S3.

  Region reference  Assigned Region.   Yes               CQ-007.

  Effective-from    Date/time          Pending           CQ-007 did not
                    assignment becomes                   answer
                    effective.                           history/effective
                                                         dating.

  Effective-to      Date/time          Pending           CQ-007 did not
                    assignment ceases                    answer
                    to be effective.                     history/effective
                                                         dating.

  Active/current    Current assignment Pending           Must avoid
  indicator         state, if needed.                    duplicating an
                                                         effective-date
                                                         rule.

  Audit fields      Created/modified   As required by    Apply approved
                    actor and UTC      AD-14             audit convention.
                    timestamps.                          
  --------------------------------------------------------------------------

### 5.6 ASM-to-Territory assignment

The approved Module 01 specification already defines `UserTerritory`.
Module 02 must use that approved structure unless a formal Module 01
specification revision is approved.

  -----------------------------------------------------------------------
  Existing logical element            Requirement
  ----------------------------------- -----------------------------------
  User account reference              Use the existing Module 01
                                      `UserAccount` relationship.

  Territory reference                 Reference the Module 02 Territory
                                      master logically.

  Uniqueness                          Follow the approved Module 01
                                      workbook until formally revised.

  Assignment history                  Not represented by effective dates
                                      in the current Module 01 baseline;
                                      requires an explicit decision and
                                      controlled specification change.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 6. Data integrity and business validation

The rules below are proposed validations derived from the confirmed
hierarchy and functional requirements. The exact error codes and
procedure contracts must be approved with the procedure specification.

  ----------------------------------------------------------------------------
  Rule ID           Validation rule   Enforcement point Status
  ----------------- ----------------- ----------------- ----------------------
  GEO-VAL-001       A District must   Geography write   Proposed
                    reference an      Stored Procedures 
                    existing active                     
                    Province.                           

  GEO-VAL-002       A Region must     Geography write   Proposed
                    reference an      Stored Procedures 
                    existing active                     
                    District.                           

  GEO-VAL-003       A Territory must  Geography write   Requirement-aligned
                    reference an      Stored Procedures proposal
                    existing active                     
                    Region.                             

  GEO-VAL-004       A farm's selected Farm write Stored Proposed; Farm module
                    Province,         Procedures        must consume Module 02
                    District, Region                    validation.
                    and Territory                       
                    must form a valid                   
                    hierarchy path.                     

  GEO-VAL-005       An ASM may        Farm, Itinerary,  FR-AH-05
                    operate only in   Visit and         
                    assigned Regions  relevant read     
                    and corresponding Stored Procedures 
                    Territories.                        

  GEO-VAL-006       An SM may access  Scoped read/write CQ-046
                    only data         Stored Procedures 
                    belonging to                        
                    assigned ASMs.                      

  GEO-VAL-007       The approver for  Approval Stored   CQ-031
                    an ASM must be    Procedures using  
                    that ASM's        Module 01         
                    assigned SM.      `UserHierarchy`   

  GEO-VAL-008       Inactive          Relevant write    Proposed; historical
                    geography records Stored Procedures read behaviour pending
                    must not be                         
                    selectable for                      
                    new assignments                     
                    or new                              
                    farm/itinerary                      
                    records.                            

  GEO-VAL-009       A Territory must  Geography write   Proposed;
                    belong to one     Stored Procedures reassignment/history
                    Region at a time.                   rule requires approval

  GEO-VAL-010       A user assignment Scoped validation Pending CQ-007 history
                    must be valid for Stored Procedures decision
                    the transaction's                   
                    effective date.                     
  ----------------------------------------------------------------------------

No physical FK constraints are created for these rules (AD-07).

------------------------------------------------------------------------

## 7. Stored Procedure operation requirements

Procedure names are intentionally omitted. Names must be approved as
part of the final Module 02 specification and comply with
`Database_Conventions.md`.

  -----------------------------------------------------------------------
  Operation group         Required behaviour      Authorization / scope
  ----------------------- ----------------------- -----------------------
  Province maintenance    Create, update,         Administrator, subject
                          deactivate and retrieve to final
                          Province records.       role-permission
                                                  mapping.

  District maintenance    Create, update,         Administrator.
                          deactivate and retrieve 
                          District records;       
                          validate Province       
                          parent.                 

  Region maintenance      Create, update,         Administrator.
                          deactivate and retrieve 
                          Region records;         
                          validate District       
                          parent.                 

  Territory maintenance   Create, update,         Administrator.
                          deactivate and retrieve 
                          Territory records;      
                          validate Region parent. 

  Geography hierarchy     Retrieve Province →     Authorized users
  retrieval               District → Region →     according to use case.
                          Territory tree or       
                          filtered child lists.   

  ASM Region assignment   Assign/remove Region    Administrator.
                          for an ASM and retrieve 
                          current/history         
                          assignments if          
                          approved.               

  ASM Territory           Use/reconcile Module 01 Administrator for
  assignment              `UserTerritory`; list   maintenance; system for
                          and validate assigned   validation.
                          Territories.            

  ASM scope lookup        Retrieve the effective  Authenticated ASM;
                          Regions and Territories server derives
                          for the authenticated   identity.
                          ASM.                    

  SM scope lookup         Retrieve only the ASMs  Authenticated SM;
                          assigned to the         server derives
                          authenticated SM and    identity.
                          their permitted areas.  

  User-in-area validation Validate that an ASM    Internal application
                          can act within a        call; actor identity is
                          specific                server-derived.
                          Region/Territory for a  
                          requested               
                          operation/date.         
  -----------------------------------------------------------------------

All application database access must use Dapper through the approved
Stored Procedure executor. Do not implement these operations with inline
SQL or EF Core LINQ.

------------------------------------------------------------------------

## 8. Decisions required before approval

These are blocking design decisions. The answer to CQ-007 ("Assign ASM
to Regions and Territories") does not resolve them.

  -------------------------------------------------------------------------------
  ID                Decision required   Why it matters          Current status
  ----------------- ------------------- ----------------------- -----------------
  M02-DEC-001       Can one Region be   Determines uniqueness   Open
                    assigned to         and access scope.       
                    multiple ASMs at                            
                    the same time?                              

  M02-DEC-002       Can one Territory   Module 01 currently     Open
                    be assigned to      allows                  
                    multiple ASMs at    UserAccount/Territory   
                    the same time?      pairs without a stated  
                                        one-ASM exclusivity     
                                        rule.                   

  M02-DEC-003       Is Territory        Prevents contradictory  Open
                    assignment          or duplicate assignment 
                    inherited           data.                   
                    automatically from                          
                    an assigned Region,                         
                    or must Admin                               
                    assign both                                 
                    independently?                              

  M02-DEC-004       If an ASM is        Determines validation   Open
                    assigned to a       and farm/itinerary      
                    Region, must every  filtering.              
                    Territory under                             
                    that Region                                 
                    automatically be                            
                    accessible?                                 

  M02-DEC-005       Must Region and     Determines effective    Open
                    Territory           dates/history design    
                    assignments retain  and historical report   
                    history?            correctness.            

  M02-DEC-006       If an ASM's Region  Determines reassignment Open
                    assignment changes, workflow and            
                    what happens to its consistency rules.      
                    existing Territory                          
                    assignments?                                

  M02-DEC-007       If a Territory      Determines hierarchy    Open
                    moves to another    history and historical  
                    Region, how are     reporting.              
                    current ASM                                 
                    assignments and                             
                    historical                                  
                    farm/itinerary                              
                    records handled?                            

  M02-DEC-008       Are                 Prevents inconsistent   Open
                    Province/District   farm geography.         
                    independently                               
                    selected on a farm,                         
                    or are they derived                         
                    from the selected                           
                    Region/Territory                            
                    mapping?                                    

  M02-DEC-009       Can a District      The confirmed order     Open
                    contain multiple    implies a parent chain, 
                    Regions, and can a  but cardinality should  
                    Region span more    be explicitly           
                    than one District?  confirmed.              

  M02-DEC-010       What are the code   Determines business     Open
                    format and          keys, duplicate checks  
                    uniqueness rules    and imports.            
                    for Province,                               
                    District, Region                            
                    and Territory?                              

  M02-DEC-011       What happens to     Determines deactivation Open
                    existing farms,     and historical read     
                    itineraries and     behaviour.              
                    visits when a                               
                    geography record is                         
                    deactivated?                                

  M02-DEC-012       At what point is    CQ-031 identifies the   Open
                    the ASM's SM        approver but not how    
                    approver resolved:  pending approvals       
                    request submission  behave after hierarchy  
                    or approval time?   changes.                
  -------------------------------------------------------------------------------

### 8.1 Recommended stakeholder confirmation

The following is a recommended decision set for review, not an approved
assumption:

-   Decide whether assignment to a Region automatically grants access to
    all its Territories.
-   Decide whether Territory assignment is independently maintained or
    is only an explicit exception/override.
-   Decide whether an area can be shared by multiple ASMs.
-   Require assignment history if historical reporting must reflect the
    ownership at the time of a transaction.
-   Define how pending approvals behave when an ASM's SM changes.

Do not apply these recommendations without stakeholder confirmation.

------------------------------------------------------------------------

## 9. Indexes, constraints and physical implementation details

The following are **not yet specified** and must be completed after §8
decisions are approved:

-   Primary-key data types and identity strategy (follow the approved
    project standard where applicable).
-   Business-key uniqueness for geography names/codes and the scope of
    that uniqueness.
-   Indexes for parent lookups, active lists, ASM-to-Region and
    ASM-to-Territory scope lookups.
-   Filtered/unique index requirements, if any.
-   Default values and check constraints, where allowed by the approved
    conventions.
-   Exact audit columns for each new table.
-   Effective-date overlap prevention, if history is approved.
-   Seed/import data and legacy mapping strategy.

All names must be deterministic, descriptive and within SQL Server's
128-character identifier limit. Use approved naming helpers/conventions;
never truncate names. No physical FK constraints.

------------------------------------------------------------------------

## 10. Migration and seed-data considerations

No migration or seed script is authorized by this draft. Before
implementation:

1.  Confirm the source of truth and ownership for Province, District,
    Region and Territory master data.
2.  Prepare an approved seed/import file with stable codes and parent
    relationships.
3.  Validate duplicate names/codes and orphaned hierarchy records before
    import.
4.  Reconcile existing Module 01 `UserTerritory` records with the new
    Territory master.
5.  Define how Region assignments are introduced without breaking
    existing user access.
6.  Confirm treatment of inactive or renamed geography records and
    historical transactions.
7.  Create schema migrations only after the Module 02 specification is
    approved.
8.  Create Stored Procedure scripts separately from EF Core migrations,
    following the approved deployment convention.

Legacy Area/Main Route/Sub Route values must not be auto-mapped to
Region/Territory without a reviewed mapping exercise.

------------------------------------------------------------------------

## 11. Security, audit and access control

-   The authenticated user's ID must be obtained server-side; clients
    must not submit `CreatedBy` or `ModifiedBy`.
-   Administrator maintains geography and assignments, subject to the
    approved role-permission matrix.
-   ASM access is limited to assigned Regions/Territories.
-   SM access is limited to assigned ASMs (CQ-046).
-   Approval operations must resolve the ASM's assigned SM (CQ-031).
-   Every audit record must correspond to an SRS-listed event (CQ-035);
    do not introduce broad audit capture beyond scope.
-   Timestamps are UTC using `SYSUTCDATETIME()` (AD-14).
-   Logical relationships are validated in Stored Procedures; no
    physical FKs.
-   Do not expose geography or assignment records outside authorized
    scope.

The exact audit event catalogue and retention period remain governed by
the audit specification, not this module.

------------------------------------------------------------------------

## 12. Testing requirements

  -----------------------------------------------------------------------
  Test category                       Required test
  ----------------------------------- -----------------------------------
  Geography hierarchy                 Valid Province → District → Region
                                      → Territory path succeeds.

  Parent validation                   Non-existent/inactive parent is
                                      rejected.

  Territory membership                Territory cannot be assigned to an
                                      invalid Region relationship.

  ASM Region scope                    ASM can access only Regions
                                      assigned under the approved
                                      assignment rule.

  ASM Territory scope                 ASM can access only Territories
                                      assigned or inherited under the
                                      approved assignment rule.

  SM visibility                       SM cannot access another SM's ASM
                                      data.

  Approval routing                    Only the ASM's assigned SM can
                                      approve the applicable request.

  Reassignment                        Effective dates/history and pending
                                      requests behave according to the
                                      approved rules.

  Deactivation                        Inactive geography is excluded from
                                      new selections while historical
                                      records remain readable as
                                      specified.

  Stored Procedure contract           Procedures use dbo-qualified names,
                                      parameterized calls and registered
                                      business errors.

  Architecture                        No physical FK constraints; no
                                      inline SQL; no application CRUD
                                      through EF Core.

  Integration                         Live SQL Server tests run only
                                      against a dedicated non-production
                                      database. Until provisioned, live
                                      behaviour remains unverified.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 13. Approval checklist

The specification can move to **Approved** only when all applicable
items are complete:

-   [ ] Stakeholders confirm Region/Territory assignment cardinality and
    sharing.
-   [ ] Stakeholders confirm inherited versus explicit Territory
    assignment.
-   [ ] Stakeholders confirm assignment history/effective-date
    behaviour.
-   [ ] Stakeholders confirm Province/District relationship to farm
    geography.
-   [ ] Stakeholders confirm geography code and uniqueness rules.
-   [ ] Stakeholders confirm reassignment and deactivation behaviour.
-   [ ] Module 01 `UserTerritory` compatibility is reviewed and any
    required change is approved through a controlled revision.
-   [ ] Physical data dictionary completed with approved types, lengths,
    nullability and defaults.
-   [ ] Keys, indexes and constraints reviewed under
    `Database_Conventions.md`.
-   [ ] Stored Procedure inventory and names reviewed.
-   [ ] Role-scope and audit requirements reviewed.
-   [ ] SRS/requirements traceability updated.
-   [ ] Formal stakeholder approval recorded.

------------------------------------------------------------------------

## 14. Version history

  -----------------------------------------------------------------------
  Version                 Date                    Changes
  ----------------------- ----------------------- -----------------------
  **0.1**                 **05-11-2026**          Initial proposed Module
                                                  02 Geography &
                                                  Assignment
                                                  specification outline
                                                  based on CQ-006,
                                                  CQ-007, CQ-031, CQ-046,
                                                  FR-AH-01--05, FR-FM-01,
                                                  FR-IP-02, Module 01
                                                  v0.2 and approved
                                                  architecture/database
                                                  conventions. Unresolved
                                                  assignment and
                                                  hierarchy rules
                                                  explicitly retained as
                                                  open decisions.

  -----------------------------------------------------------------------

------------------------------------------------------------------------

**Document control:** Draft for review only. This document does not
authorize creation or alteration of tables, columns, indexes,
constraints, migrations, Stored Procedures, seed data or production
records. Implementation begins only after the open decisions are
resolved and this specification is formally approved.
