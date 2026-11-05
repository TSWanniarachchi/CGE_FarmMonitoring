# CGE Farm Monitoring System -- Database Gap Analysis

  ----------------------------------- -----------------------------------
  Document                            `Database_Gap_Analysis.md`

  Version                             **0.2 -- Updated analysis for
                                      stakeholder review**

  Date                                05-11-2026

  Status                              **Analysis only.** No schema,
                                      migration, table, column or Stored
                                      Procedure is created or changed by
                                      this document. Suggested actions
                                      are proposals, not approved
                                      database designs.

  Baseline compared                   **S3 -- Module 01 Database
                                      Specification v0.2 (Security &
                                      Access)**, the only approved
                                      database specification identified
                                      in the source set

  Compared against                    SRS (S1), Project Proposal (S2),
                                      Module 01 specification (S3),
                                      legacy database design (S4), Figma
                                      web/mobile prototypes (S5/S6),
                                      Functional Requirements Analysis,
                                      Business Workflow Analysis, UI
                                      Screen Requirement Mapping,
                                      Requirements Traceability Matrix,
                                      Business Clarification Register,
                                      Architecture Decisions v2 and
                                      Database Conventions

  Architecture constraints            AD-04, AD-06, AD-07, AD-08, AD-14
                                      and AD-24;
                                      `Database_Conventions.md`

  Supersedes                          Version 0.1 dated 04-11-2026
  ----------------------------------- -----------------------------------

> **Authority rule:** The SRS and approved project decisions govern
> business behaviour. The approved Module 01 database specification
> governs Module 01 database objects. Architecture decisions and
> Database Conventions govern implementation constraints. Legacy tables
> and Figma screens are reference evidence only; they do not authorize
> copying a legacy schema or treating a prototype as a final data model.

------------------------------------------------------------------------

## 1. Executive summary

The approved database specification still covers **Module 01 -- Security
& Access only**. No approved database specification has been identified
for Geography & Assignment, Farm, Master Data, Questionnaire,
Batch/Flock, Itinerary, Visit, Expense, Notification, Audit or Offline
Sync.

The business clarifications have reduced uncertainty in several areas,
but they have **not** produced approved table designs. The next
database-design work should therefore be specification-led and
module-by-module.

### 1.1 Gap register summary

The IDs below are retained from Version 0.1 for traceability. A finding
can remain open even when its related business clarification is
answered: the clarification may settle behaviour while the database
representation remains to be designed.

  ------------------------------------------------------------------------
  Area                                      Findings Version 0.2 position
  --------------------- ---------------------------- ---------------------
  Module 01 -- Security                 DBG-001--014 Existing approved
  & Access                                           baseline; several
                                                     specification and
                                                     operational
                                                     dependencies remain

  Geography &                           DBG-020--023 Business hierarchy
  assignment                                         and assignment
                                                     direction clarified;
                                                     data model and
                                                     effective-dating
                                                     rules still need
                                                     specification

  Questionnaire                         DBG-030--034 Question types,
                                                     conditional questions
                                                     and versioning
                                                     clarified;
                                                     questionnaire schema
                                                     still absent

  Batch / flock                         DBG-040--042 One breed per batch
                                                     clarified; batch and
                                                     visit-measurement
                                                     schema still absent

  Farm & master data                    DBG-050--055 Farm type, GPS,
                                                     approval, dealer
                                                     relationship and
                                                     master-data decisions
                                                     partly clarified

  Itinerary, visit,                     DBG-060--067 Several workflow
  expense, notification                              rules clarified;
                                                     transaction models
                                                     and approval records
                                                     still need
                                                     specification

  Sync, device, audit,                  DBG-070--075 Server-wins conflict
  cross-cutting                                      rule, no day-end
                                                     lock, audit event
                                                     scope and UTC
                                                     direction clarified;
                                                     sync and audit
                                                     structures remain
                                                     open

  **Total**                      **46 retained IDs** No new database
                                                     object is approved by
                                                     this document
  ------------------------------------------------------------------------

### 1.2 Key conclusions

1.  **Module 01 remains the only approved database specification.** Do
    not extend it informally to cover business modules.
2.  The geography hierarchy is now **Province → District → Region →
    Territory**. ASM assignments cover Regions and Territories. The
    exact assignment cardinality, overlap, history and effective dates
    still require a data-design decision.
3.  The system requires configurable Visit Questionnaires with
    conditional questions, supported Figma answer types, Admin-defined
    mandatory/validation rules and a new version for every published
    change. The questionnaire data model is not yet specified.
4.  A Batch has one breed. Batch lifecycle and the storage of
    visit-based measurements still require a dedicated specification.
5.  Mobile-created farms require SM approval. One Main Farm may have
    unlimited Dealer Farms. Farm GPS is required during mobile farm
    registration.
6.  A visit submission is final. Unplanned visits are allowed with
    post-visit approval; this does **not** mean the submitted visit
    itself is editable or generally approvable.
7.  Expense approvers may modify the variable amount. The expense model
    must preserve submitted and approved values and the decision
    history; exact fields and workflow remain to be specified.
8.  Offline conflict resolution is **server wins**. There is no Day-End
    lock.
9.  Only SRS-listed audit events are in scope. Audit storage and
    retention details still need specification.
10. Database conventions must follow the latest approved rules: `dbo`
    only, no physical foreign keys, Stored Procedures for application
    data operations, EF Core migrations for schema only, descriptive
    identifiers up to SQL Server's 128-character limit, and no custom
    15-character limit.

------------------------------------------------------------------------

## 2. Status definitions

  -----------------------------------------------------------------------
  Status                              Meaning
  ----------------------------------- -----------------------------------
  **Open -- specification required**  Business direction is sufficiently
                                      clear to begin modelling, but no
                                      approved database specification
                                      exists

  **Open -- decision required**       A material business or technical
                                      rule is still unresolved

  **Partly clarified**                A CQ answer resolves part of the
                                      finding; remaining data-design or
                                      policy detail is open

  **Clarified -- design required**    The related business decision is
                                      known, but database representation
                                      has not been approved

  **Baseline issue**                  A gap or inconsistency exists in
                                      the already approved Module 01
                                      specification or its operational
                                      dependencies

  **Out of scope**                    The related feature is explicitly
                                      excluded; do not create database
                                      objects for it unless scope changes
                                      through the approved change process

  **Pending external approval**       Work is blocked by a required
                                      approval or controlled dependency
  -----------------------------------------------------------------------

Priority remains an implementation-planning indicator, not an approval.
**Critical / High / Medium / Low** does not authorize implementation.

------------------------------------------------------------------------

## 3. Module 01 -- Security & Access (approved baseline S3)

The following findings compare the approved Module 01 specification with
the functional requirements and the latest decisions. Module 01 object
definitions must not be changed through this gap-analysis document.

  ----------------------------------------------------------------------------------------------------------
  ID             Finding                      Version 0.2 status and   Required action /      Priority
                                              decision impact          dependency             
  -------------- ---------------------------- ------------------------ ---------------------- --------------
  DBG-001        User profile attributes:     **Partly clarified.**    Compare every SRS user Medium
                 Department is not present in CQ-037 says use SRS      field with S3. Raise a 
                 the Module 01 `AppUser`      fields only. This does   controlled Module 01   
                 baseline; other fields in    not automatically        specification revision 
                 the SRS/Figma must be        authorize adding every   for any confirmed      
                 checked against the          Figma field.             omission. Do not add   
                 confirmed user-field list.                            Designation, Company,  
                                                                       State, User Type or    
                                                                       Group Name solely      
                                                                       because a prototype    
                                                                       shows them.            

  DBG-002        `UserAccount.ProcessId`      **Clarified at business  Review whether         High
                 requires a Process master,   level.** CQ-014 confirms `ProcessId` remains    
                 but the meaning and          Farm Monitoring only;    necessary in the       
                 permitted values were not    Farm Evaluation is       approved Module 01     
                 defined in the baseline.     excluded.                specification. If the  
                                                                       column is mandatory    
                                                                       there, document a      
                                                                       controlled             
                                                                       migration/seed         
                                                                       strategy; do not       
                                                                       silently remove or     
                                                                       repurpose it.          

  DBG-003        `UserTerritory` does not     **Partly clarified.**    Specify                Critical
                 fully represent the          CQ-006 sets Province →   Region/Territory       
                 clarified assignment model   District → Region →      assignment             
                 and has no defined           Territory. CQ-007        cardinality, whether   
                 historical/effective-date    assigns ASMs to Regions  multiple ASMs may      
                 behaviour.                   and Territories.         share an area, whether 
                                              Assignment exclusivity,  an ASM may have        
                                              overlap and history      multiple areas,        
                                              remain open.             effective dates,       
                                                                       reassignment behaviour 
                                                                       and historical         
                                                                       reporting. Reconcile   
                                                                       Module 01 references   
                                                                       through a controlled   
                                                                       specification          
                                                                       revision.              

  DBG-004        `UserHierarchy` manager      **Baseline issue --      Specification owner to Medium
                 naming differs across the    unresolved.**            reconcile the workbook 
                 Module 01 workbook sheets                             sheets and ERD, then   
                 (`ManagerId` /                                        publish a controlled   
                 `ManagerAccountId`); the ASM                          revision. Do not       
                 index naming also needs                               rename implementation  
                 consistency review.                                   objects independently  
                                                                       of the approved        
                                                                       workbook.              

  DBG-005        Module 01 workbook Read Me   **Resolved as an         Update the Module 01   Medium
                 contains a 15-character      architecture convention; workbook Read Me       
                 identifier limit,            source workbook          through its own        
                 conflicting with the current correction pending.**    controlled revision.   
                 approved AD-08 / Database    The custom 15-character  Preserve the 40        
                 Conventions rule.            limit is withdrawn. SQL  reviewed Module 01     
                                              Server's 128-character   key/constraint/index   
                                              identifier maximum       names unless the       
                                              applies.                 specification owner    
                                                                       explicitly revises     
                                                                       them.                  

  DBG-006        Role/menu permissions do not **Partly clarified.**    Define data-scope      High
                 by themselves fully express  CQ-001, CQ-031 and       enforcement for Admin, 
                 record-level data scope and  CQ-046 establish SM      SM and ASM across each 
                 approval ownership.          approval of itinerary,   module. Specify how    
                                              farm and expense; only   the API and Stored     
                                              the assigned ASM's SM    Procedures enforce     
                                              approves; SM sees only   assigned-ASM scope. Do 
                                              assigned ASMs.           not assume menu        
                                                                       permissions alone      
                                                                       provide row-level      
                                                                       authorization.         

  DBG-007        `Menu.Platform` and          **Open -- specification  Reconcile code lists   Low
                 `LoginHistory.EventType`     detail required.**       with Module 01         
                 code lists need                                       specification and      
                 authoritative values.                                 application            
                                                                       requirements. Do not   
                                                                       invent enum values in  
                                                                       business tables or     
                                                                       procedures.            

  DBG-008        No approved audit-log data   **Partly clarified.**    Define the audit       Critical
                 model is defined for the     CQ-035 limits audit to   specification for the  
                 SRS-required audit events,   SRS-listed events. The   exact SRS event list,  
                 including previous and       storage structure,       including before/after 
                 updated values.              access, retention and    values where required, 
                                              value-capture format     actor identity, UTC    
                                              remain open.             timestamp, target      
                                                                       record, read           
                                                                       permissions and        
                                                                       retention. Coordinate  
                                                                       with AD-14.            

  DBG-009        Module 01                    **Partly clarified.**    Ensure the approved    High
                 role/permission/assignment   Audit events are limited audit design captures  
                 changes can lose historical  to the SRS list;         the required           
                 detail if hard-deleted       role-based access        role-based access      
                 without audit history.       changes are among the    changes. Decide        
                                              SRS audit requirements.  whether operational    
                                                                       records are            
                                                                       soft-deactivated or    
                                                                       physically deleted     
                                                                       only where the Module  
                                                                       01 specification       
                                                                       permits.               

  DBG-010        The relationship between a   **Clarified at business  Use the approved       High
                 person, user account and     level.** CQ-038 says     Module 01 user         
                 transactions needs           transactions belong to   identity as the        
                 consistent ownership.        the individual user.     transaction actor.     
                                                                       Specify how ownership  
                                                                       remains attributable   
                                                                       when an account is     
                                                                       deactivated or a staff 
                                                                       member changes role.   
                                                                       Do not create          
                                                                       role-slot ownership    
                                                                       semantics unless newly 
                                                                       approved.              

  DBG-011        Device registration and      **Out of scope unless    Do not add device      Low
                 device-side document         required by SRS.**       registration or legacy 
                 numbering were inferred from CQ-034 excludes all      document counters      
                 legacy/Figma evidence.       features outside the     merely because S4/S5   
                                              SRS.                     contains them. Reopen  
                                                                       only through an        
                                                                       approved scope change  
                                                                       or explicit SRS        
                                                                       requirement.           

  DBG-012        The Module 01 Stored         **Open -- controlled     Cross-check every      High
                 Procedure inventory may not  specification review     Module 01 FR against   
                 cover every operation        required.**              the approved Stored    
                 identified by the functional                          Procedures sheet. Add  
                 analysis.                                             missing operations     
                                                                       only through a Module  
                                                                       01 specification       
                                                                       revision, preserving   
                                                                       the exact existing     
                                                                       procedure names and    
                                                                       Database Conventions   
                                                                       rules.                 

  DBG-013        Password history and         **Open --                Confirm whether        Low
                 password expiry policy are   decision/specification   password reuse         
                 not fully specified in the   required.**              prevention or expiry   
                 database baseline.                                    is required by the     
                                                                       SRS/security policy.   
                                                                       Do not infer these     
                                                                       from legacy columns.   

  DBG-014        Bootstrap Administrator and  **Partly clarified /     Define controlled seed High
                 System user seed data are    operational              data for the required  
                 not fully specified in the   dependency.** AD-14      roles, menus,          
                 database baseline.           requires a controlled    permissions,           
                                              System user and          Administrator and      
                                              configured               System user. Never     
                                              `SystemUser:UserId`; the hard-code the System   
                                              Module 01 specification  user ID. Resolve the   
                                              still needs to define    missing System-user    
                                              repeatable seed          lookup procedure       
                                              ownership and required   dependency before      
                                              records.                 production use.        
  ----------------------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 4. Geography & assignment

Business decisions: Province → District → Region → Territory (CQ-006);
ASM assigned to Regions and Territories (CQ-007); SM visibility
restricted to assigned ASMs (CQ-046).

  ----------------------------------------------------------------------------------------------
  ID             Finding                   Version 0.2 status  Required action /  Priority
                                           and decision impact dependency         
  -------------- ------------------------- ------------------- ------------------ --------------
  DBG-020        No approved               **Clarified --      Create a Geography Critical
                 Region/Territory database design required.**  & Assignment       
                 specification exists.     The four-level      database           
                 Legacy Area/Main          hierarchy is        specification.     
                 Route/Sub Route is not    confirmed.          Model only the     
                 equivalent to the new                         confirmed          
                 hierarchy.                                    hierarchy and      
                                                               document codes,    
                                                               names, active      
                                                               state, parent      
                                                               relationships and  
                                                               seed/migration     
                                                               requirements.      

  DBG-021        Farm Province/District    **Partly            Define the         High
                 and operational           clarified.**        authoritative      
                 Region/Territory          Geography levels    mapping and        
                 relationships may be      are confirmed;      validation rules.  
                 inconsistent if           whether             Do not assume that 
                 independently maintained. Region/Territory    a farm's           
                                           are selected or     Region/Territory   
                                           derived from        can be derived     
                                           Province/District   without an         
                                           remains a modelling approved mapping.  
                                           rule.                                  

  DBG-022        Farm-to-route/sub-route   **Open / likely out Do not create      Low
                 mapping exists only in    of scope unless SRS route/sub-route    
                 prototype/legacy evidence confirms.**         entities from      
                 and is not established as                     legacy evidence    
                 a business requirement.                       alone. Check the   
                                                               SRS and approved   
                                                               UI mapping;        
                                                               include only if    
                                                               explicitly         
                                                               required.          

  DBG-023        Approval routing when an  **Partly            Define assignment  Critical
                 ASM's                     clarified.** Only   snapshot/routing   
                 hierarchy/assignment      the assigned ASM's  rule for pending   
                 changes while a request   SM is the approver  approvals,         
                 is pending is not fully   (CQ-031). Whether   including          
                 defined.                  approver is         reassignment,      
                                           resolved at         deactivation and   
                                           submission or at    historical audit.  
                                           decision time                          
                                           remains open.                          
  ----------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 5. Questionnaire

Business decisions: Farm Monitoring only (CQ-014); fixed system visit
fields plus configurable questionnaire (CQ-015); a new version for every
published change (CQ-016); conditional questions required (CQ-017);
supported question types from Figma (CQ-018); approved forms will be
provided (CQ-019); Admin configures mandatory fields and validation per
question (CQ-020).

  ---------------------------------------------------------------------------------------
  ID             Finding                Version 0.2     Required action /  Priority
                                        status and      dependency         
                                        decision impact                    
  -------------- ---------------------- --------------- ------------------ --------------
  DBG-030        No approved            **Clarified --  Create a           High
                 questionnaire,         design          questionnaire      
                 section, question,     required.** The specification      
                 answer-type or         question types  covering           
                 answer-storage data    and             definitions,       
                 model exists.          configuration   sections,          
                                        requirements    questions,         
                                        are known, but  supported answer   
                                        schema is not.  types,             
                                                        option/data-set    
                                                        sources,           
                                                        validation,        
                                                        mandatory rules,   
                                                        conditional rules  
                                                        and answer         
                                                        persistence. Use   
                                                        the approved form  
                                                        pack when          
                                                        supplied.          

  DBG-031        Published              **Clarified --  Specify immutable  Critical
                 questionnaire          design          published versions 
                 definitions need       required.**     and ensure each    
                 versioning;            Every published visit answer is    
                 legacy/prototype       change creates  associated with    
                 editing in place can   a new version   the exact version  
                 change the meaning of  (CQ-016).       used. Define draft 
                 historical answers.                    editing, publish,  
                                                        retire and mobile  
                                                        download           
                                                        behaviour. Do not  
                                                        hard-delete a      
                                                        version used by    
                                                        submitted visits.  

  DBG-032        Questionnaire          **Partly        Define which       High
                 applicability to Farm  clarified.**    questionnaire      
                 Type / Visit Type /    Farm Type is a  version applies to 
                 Process is not defined master          which Farm Type    
                 in an approved model.  attribute only  and visit context. 
                                        (CQ-022);       Do not add Farm    
                                        Process scope   Evaluation or      
                                        is Farm         unconfirmed Visit  
                                        Monitoring only Type dimensions.   
                                        (CQ-014). Exact                    
                                        questionnaire                      
                                        applicability                      
                                        rules remain to                    
                                        be specified.                      

  DBG-033        Conditional-question   **Partly        Specify supported  Medium
                 dependency and         clarified.**    condition          
                 Previous Data          Conditional     operators, answer  
                 Reference behaviour    questions are   dependencies,      
                 are not fully          required        evaluation order,  
                 specified.             (CQ-017).       missing/previous   
                                        Previous Data   answer handling    
                                        Reference       and whether        
                                        behaviour still previous data can  
                                        needs a precise prefill or only    
                                        rule.           display.           

  DBG-034        Repeating Data Set     **Open --       Inspect the        Medium
                 Input answer storage   confirm against approved           
                 is not specified.      approved form   questionnaire      
                                        pack.** CQ-018  forms and define   
                                        lists Data Set  Data Set columns,  
                                        as an answer    repeated rows,     
                                        type, but does  validation and     
                                        not fully       answer retrieval   
                                        define          before designing   
                                        row/column      storage.           
                                        semantics.                         
  ---------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 6. Batch / flock

Business decision: one breed per batch (CQ-021). The SRS remains the
authority for formulas (CQ-023); visit fields are fixed system fields
(CQ-015).

  ---------------------------------------------------------------------------------------
  ID             Finding         Version 0.2     Required action /         Priority
                                 status and      dependency                
                                 decision impact                           
  -------------- --------------- --------------- ------------------------- --------------
  DBG-040        No Batch entity **Clarified --  Create a Batch/Flock      Critical
                 exists in the   design          database specification    
                 approved        required.** One from the SRS. Define farm 
                 database        breed per batch relationship, breed,      
                 specification   is confirmed.   start date, initial       
                 or legacy table                 quantity, lifecycle and   
                 model, although                 closure attributes from   
                 Batch/Flock is                  approved requirements     
                 central to the                  only.                     
                 SRS.                                                      

  DBG-041        Visit-based     **Partly        Map each SRS measurement  Critical
                 batch           clarified.**    to a fixed visit/batch    
                 measurements    Visit system    data element and decide   
                 (e.g. current   fields are      its relationship to Batch 
                 size, feed,     fixed; formulas and Visit. Document       
                 body weight,    must follow the formula inputs,           
                 production,     SRS.            precision, rounding,      
                 mortality and                   null/zero handling and    
                 derived                         recalculation. Do not     
                 metrics) have                   duplicate values in       
                 no approved                     questionnaire answers     
                 storage model.                  unless the approved form  
                                                 explicitly requires it.   

  DBG-042        Batch status    **Open --       Define lifecycle          High
                 and closure     specification   statuses, close date,     
                 fields/rules    required.**     final                     
                 are not fully                   count/weight/FCR/reason   
                 defined.                        only where supported by   
                                                 SRS; define whether       
                                                 closed batches can be     
                                                 reopened and who may do   
                                                 so.                       
  ---------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 7. Farm & master data

Business decisions: Farm Type is a master attribute only (CQ-022); GPS
is mandatory during mobile farm registration (CQ-027); mobile-created
farms require SM approval (CQ-028); one Main Farm can have unlimited
Dealer Farms (CQ-029); Farm Monitoring only (CQ-014); use SRS lists for
master data and feed list (CQ-044/045); use Figma A/B/C grading
(CQ-043); Breeder Farm is a separate supplier/master entity (CQ-039).

  ---------------------------------------------------------------------------------------------------
  ID             Finding           Version 0.2 status and  Required action /           Priority
                                   decision impact         dependency                  
  -------------- ----------------- ----------------------- --------------------------- --------------
  DBG-050        Legacy Farm       **Partly clarified --   Create the Farm database    Critical
                 structure does    design required.**      specification using SRS     
                 not cover the SRS Region/Territory        fields. Include the         
                 farm attributes   hierarchy, Farm Type as confirmed geography         
                 and clarified     master attribute and    mapping, Farm Type          
                 geography/GPS     mobile GPS requirement  reference and mobile        
                 needs.            are known.              registration GPS            
                                                           requirement. Define         
                                                           coordinate validation and   
                                                           update policy from          
                                                           requirements; do not add    
                                                           unsupported fields from     
                                                           Figma alone.                

  DBG-051        New Farm approval **Clarified -- design   Specify                     Critical
                 cannot be         required.**             pending/approved/rejected   
                 represented by a  Mobile-created farms    states, reviewer, decision  
                 simple legacy     require SM approval     timestamp, rejection reason 
                 `IsApproved` bit. (CQ-028), with the      and resubmission rules only 
                                   assigned ASM's SM as    to the extent required by   
                                   approver (CQ-031).      the SRS and confirmed       
                                                           workflow. Preserve the      
                                                           original submission and     
                                                           decision history.           

  DBG-052        Main Farm /       **Clarified -- design   Specify parent/child        High
                 Dealer Farm       required.** One Main    relationship, permitted     
                 relationship is   Farm may have unlimited farm types, whether a       
                 ambiguous in the  Dealer Farms (CQ-029).  Dealer Farm can be          
                 legacy                                    reassigned, duplicate       
                 self-reference.                           prevention and visibility   
                                                           rules. Do not assume a      
                                                           Dealer Farm can itself be a 
                                                           parent.                     

  DBG-053        Farm Code format, **Open -- decision      Confirm code format,        Medium
                 uniqueness and    required.** CQ-028      uniqueness scope,           
                 assignment point  confirms approval       generation timing           
                 are not           workflow, not code      (submission vs approval)    
                 confirmed.        format.                 and whether rejected        
                                                           submissions retain a code.  
                                                           Do not reuse prototype      
                                                           examples as approved        
                                                           formats.                    

  DBG-054        Master data       **Partly clarified --   Create a Master Data        High
                 entities and      design required.** Use  specification from the SRS  
                 relationships are SRS lists for           lists. Define each master's 
                 not covered by an grading/housing/water   attributes, active state,   
                 approved database and the SRS feed list   uniqueness and              
                 specification.    (CQ-043--045); Breeder  relationships. Specify      
                                   Farm is a separate      Breeder Farm separately and 
                                   supplier/master entity  define its relationship to  
                                   (CQ-039).               breed/batch only from       
                                                           source requirements.        

  DBG-055        Farm duplicate    **Open -- decision      Define whether duplicate    Medium
                 detection rule is required.**             checks use farm name,       
                 not defined.                              phone, GPS proximity, owner 
                                                           or a combination; specify   
                                                           warning vs hard-block and   
                                                           who can override. Do not    
                                                           create a unique constraint  
                                                           on a non-confirmed business 
                                                           key.                        
  ---------------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 8. Itinerary, visit, expense and notification

Business decisions: ASM defines recurring plans (CQ-010), SM approves
(CQ-011); unplanned visits allowed with post-visit approval (CQ-012);
submitted visit is final (CQ-013); all visit fields fixed system fields
(CQ-015); GPS captured at visit start only (CQ-026); multiple visits per
farm per day allowed (CQ-041); approver may modify variable expense
amount (CQ-030); in-app notifications only (CQ-032); SRS reports only
(CQ-033).

  ----------------------------------------------------------------------------------------------
  ID             Finding             Version 0.2     Required action / dependency Priority
                                     status and                                   
                                     decision impact                              
  -------------- ------------------- --------------- ---------------------------- --------------
  DBG-060        No approved         **Partly        Specify itinerary            Critical
                 itinerary model     clarified --    header/detail,               
                 exists. Legacy      design          one-Territory-per-day rule,  
                 `VisitPlan` cannot  required.**     selected farms,              
                 represent the       Daily itinerary recurring-plan definition,   
                 confirmed           is one          generated instances,         
                 daily/recurring     Territory with  approval states, rejection,  
                 workflow.           selected farms  cancellation and audit.      
                                     (CQ-008); ASM   Resolve recurrence           
                                     defines         pattern/generation horizon   
                                     recurring plans and edit-after-approval      
                                     and SM approves rules before final schema.   
                                     (CQ-010/011).                                

  DBG-061        Visit data model    **Clarified --  Specify Visit identity,      Critical
                 lacks a confirmed   design          farm/batch context,          
                 representation of   required.**     planned/unplanned marker,    
                 fixed visit fields, Visit           start GPS and timestamp,     
                 GPS-at-start and    submission is   submitted timestamp, fixed   
                 repeat visits.      final; GPS is   SRS fields and linkage to    
                                     captured at     questionnaire                
                                     start only;     version/answers. Do not      
                                     multiple visits impose                       
                                     per farm/day    one-visit-per-farm-per-day   
                                     are allowed     uniqueness.                  
                                     (CQ-013,                                     
                                     CQ-026,                                      
                                     CQ-041).                                     

  DBG-062        Visit               **Clarified in  Do not add draft/edit/reopen High
                 status/correction   part.**         states after final           
                 model was           Submitted visit submission unless explicitly 
                 previously unclear. is final        required. Specify a separate 
                                     (CQ-013).       unplanned-visit exception    
                                     Unplanned       approval record/state and    
                                     visits require  its relationship to the      
                                     post-visit      immutable submitted visit.   
                                     approval                                     
                                     (CQ-012); this                               
                                     is a separate                                
                                     approval                                     
                                     concern, not                                 
                                     general visit                                
                                     approval.                                    

  DBG-063        Expense type        **Open --       Define expense type fields   High
                 image-proof         specification   and proof requirement from   
                 requirement and     required.**     SRS. Connect proof files to  
                 supporting-file                     the file-storage design      
                 linkage need                        (DBG-074); do not add image  
                 confirmation                        requirements beyond the SRS. 
                 against SRS.                                                     

  DBG-064        Expense approval    **Partly        Specify submitted amount,    Critical
                 model does not      clarified --    approved amount, approver,   
                 preserve the full   design          decision, timestamp,         
                 decision/amount     required.**     rejection reason if          
                 history in the      Approver may    required, and audit trail.   
                 legacy design.      modify the      Define fixed vs variable     
                                     variable amount amount behaviour and whether 
                                     (CQ-030); only  the approved amount can be   
                                     the assigned    changed after approval.      
                                     ASM's SM                                     
                                     approves                                     
                                     (CQ-031).                                    

  DBG-065        Notification        **Clarified --  Specify recipient, event     High
                 recipient, event    design          reference, read/unread       
                 type and read state required.**     state, created/read          
                 are not specified   Notifications   timestamps and allowed event 
                 in an approved      are in-app only catalogue from SRS. Exclude  
                 model.              (CQ-032);       SMS, email and push          
                                     events are      notification infrastructure  
                                     restricted to   unless scope changes.        
                                     SRS-listed                                   
                                     events                                       
                                     (CQ-035).                                    

  DBG-066        Legacy Attendance,  **Out of        Do not create tables or      Low
                 Feedback, Company   scope.** CQ-034 procedures for these legacy  
                 and Farmer          excludes all    features unless an approved  
                 report-read-log     features        SRS/change request brings    
                 features are not in outside the     them into scope.             
                 confirmed scope.    SRS; CQ-033                                  
                                     restricts                                    
                                     reports to SRS                               
                                     reports.                                     

  DBG-067        Legacy object names **Resolved as a Do not carry legacy names    Low
                 contain             design rule.**  into the new schema. Follow  
                 misspellings,                       approved AD-08 and           
                 spaces and                          `Database_Conventions.md`;   
                 inconsistent                        do not rename approved       
                 naming.                             Module 01 objects            
                                                     independently.               
  ----------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 9. Sync, device, audit and cross-cutting

Business decisions: server wins on conflict (CQ-024); no Day-End lock
(CQ-025); GPS at visit start only (CQ-026); audit only SRS-listed events
(CQ-035); AES-256-GCM reversible encryption requested subject to
security approval (CQ-036); transactions belong to individual user
(CQ-038); no external sharing (CQ-040).

  ----------------------------------------------------------------------------------------
  ID             Finding          Version 0.2       Required action /       Priority
                                  status and        dependency              
                                  decision impact                           
  -------------- ---------------- ----------------- ----------------------- --------------
  DBG-070        Offline sync     **Partly          Specify sync request    Critical
                 protocol lacks   clarified --      identity, idempotency,  
                 an approved      design            server version/modified 
                 persistence      required.**       timestamp, upload       
                 model for client Server wins on    acknowledgement, retry  
                 identity,        conflict          behaviour, partial      
                 idempotency,     (CQ-024).         failure and duplicate   
                 versioning,                        submission handling.    
                 incremental                        Server-wins must be     
                 download and                       enforced consistently;  
                 failure                            do not silently         
                 tracking.                          overwrite server data   
                                                    with stale client       
                                                    state.                  

  DBG-071        Legacy Day-End   **Out of scope /  Do not create Day-End   Low
                 lock state and   not required.**   lock tables, columns,   
                 unlock events    CQ-025 confirms   procedures or workflow. 
                 were proposed as no Day-End lock.  Remove any inherited    
                 database                           lock assumption from    
                 requirements.                      module designs.         

  DBG-072        Audit event      **Partly          Build an event          High
                 coverage and     clarified.** Only catalogue from SRS and  
                 retention        SRS-listed events map each event to its   
                 outside Module   are audited       actor, target,          
                 01 are not fully (CQ-035).         timestamp and           
                 specified.       Retention and     before/after values     
                                  exact             where required. Define  
                                  event-to-entity   retention/access with   
                                  mapping remain    the relevant owner.     
                                  open.             Avoid auditing every    
                                                    database mutation by    
                                                    default if not in       
                                                    scope.                  

  DBG-073        Ownership and    **Partly          Define ownership and    Critical
                 immutability     clarified.**      post-submission         
                 rules differ by  Transactions      mutation rules per      
                 transaction type belong to the     entity. Keep visit data 
                 and were         individual user   immutable after         
                 previously       (CQ-038); visit   submission; represent   
                 unresolved.      submission is     approvals/adjustments   
                                  final (CQ-013);   as separate decision    
                                  expense approval  data where appropriate. 
                                  can modify        Define itinerary and    
                                  variable amount   farm approval lifecycle 
                                  (CQ-030).         separately.             

  DBG-074        Secure           **Open --         Specify file            High
                 on-premises file specification and metadata/reference,     
                 storage with     security design   storage key/path        
                 version control  required.** No    strategy, content       
                 is required by   external sharing  type/size validation,   
                 the proposal,    (CQ-040).         checksum, versioning,   
                 but no approved                    access authorization,   
                 file-reference                     retention and backup.   
                 model/location                     Store file bytes in the 
                 contract is                        approved secure         
                 documented.                        on-premises storage; do 
                                                    not expose public URLs  
                                                    or external sharing.    

  DBG-075        Business         **Partly          Store timestamps in UTC Medium
                 date/time-zone   clarified.**      as required by AD-14.   
                 handling for     AD-14 requires    Define the business     
                 daily itinerary  UTC timestamps.   timezone and conversion 
                 rules and        The application   boundary for daily      
                 offline sync     operates in Sri   itinerary and           
                 needs an         Lanka, but the    reporting. Do not store 
                 explicit rule.   business-date     local timestamps as if  
                                  conversion and    they were UTC.          
                                  boundary rule                             
                                  should be                                 
                                  explicitly                                
                                  documented.                               
  ----------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 10. Data ownership and access -- updated business rules

This table records business decisions and outstanding data-design
questions. It is not a substitute for the Role Permission Matrix or a
database specification.

  -----------------------------------------------------------------------
  Concept                 Confirmed business rule Remaining
                                                  database-design
                                                  question
  ----------------------- ----------------------- -----------------------
  User, role and          Admin, SM and ASM roles Reconcile the role seed
  permission              only; Supervisor is the data and permission
                          same role as ASM        catalogue with the
                          (CQ-002/003).           approved Module 01
                                                  specification.

  Geography and           Province → District →   Assignment cardinality,
  assignment              Region → Territory; ASM overlap, history,
                          assigned to Regions and effective dates and
                          Territories             reassignment behaviour.
                          (CQ-006/007).           

  Farm                    Admin/SM web farm       Full farm attributes,
                          management; ASM mobile  approval lifecycle,
                          farm registration       duplicate rules and
                          requires GPS and SM     field-level edit
                          approval (CQ-027/028).  permissions.

  Main Farm / Dealer Farm One Main Farm may have  Parent/child
                          unlimited Dealer Farms  constraints and
                          (CQ-029).               reassignment behaviour.

  Itinerary               ASM defines recurring   Recurrence generation,
                          plans; SM approves.     approval snapshot,
                          Daily itinerary         cancellation and
                          contains one Territory  amendment rules.
                          and selected farms      
                          (CQ-008/010/011).       

  Planned visit           Visit submission is     Exact fixed field list,
                          final; multiple visits  batch linkage and
                          per farm per day are    exception approval
                          allowed (CQ-013/041).   representation.

  Unplanned visit         Allowed with post-visit Approval record,
                          approval (CQ-012).      decision states and
                                                  approver resolution
                                                  timing.

  Batch                   One breed per batch     Batch lifecycle,
                          (CQ-021).               closure and visit
                                                  measurement
                                                  persistence.

  Questionnaire           Versioned on every      Exact version
                          published change;       structure, form
                          conditional questions;  applicability,
                          Admin configures        condition operators and
                          mandatory/validation    Data Set answer model.
                          rules (CQ-016--020).    

  Expense                 Assigned ASM's SM       Amount history,
                          approves; approver can  rejection details,
                          modify variable amount  proof files and
                          (CQ-030/031).           post-approval change
                                                  rules.

  Notification            In-app only; SRS-listed Event catalogue,
                          events only             recipient resolution
                          (CQ-032/035).           and read-state
                                                  persistence.

  Audit                   Only SRS-listed events  Retention, access and
                          (CQ-035).               exact before/after
                                                  representation.

  User/transaction        Transactions belong to  Deactivation and
  ownership               individual user         staff-transfer history
                          (CQ-038).               behaviour.

  Breeder Farm            Separate                Attributes and approved
                          supplier/master entity  relationship to
                          (CQ-039).               breed/batch.

  External sharing        Not permitted (CQ-040). File and API
                                                  authorization must
                                                  enforce internal-only
                                                  access.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 11. Stored Procedure inventory -- planning view only

The following are operation groups implied by the requirements and
approved architecture rules. **This is not a procedure-name list and
does not approve any Stored Procedure.** Exact procedure names must be
defined in each approved module database specification and follow
`Database_Conventions.md`.

  -------------------------------------------------------------------------------
  Module                  Required operation groups to    Notes
                          specify                         
  ----------------------- ------------------------------- -----------------------
  Module 01 -- Security & User, role, menu, permission,   Reconcile the approved
  Access                  account, territory/hierarchy,   S3 procedure inventory
                          login history, session and      with all Module 01 FRs.
                          password operations             Preserve specified
                                                          names.

  Geography & Assignment  Geography master maintenance;   Must support Province →
                          Region/Territory assignment;    District → Region →
                          user-area lookup; scope         Territory and ASM
                          validation                      assignment to
                                                          Regions/Territories.

  Farm                    Web farm maintenance; mobile    Approval by assigned
                          farm submission; pending-farm   ASM's SM; GPS mandatory
                          list; approve/reject; farm      for mobile
                          search/profile                  registration.

  Master Data             SRS-listed master maintenance   Include only SRS lists;
                          and active-list retrieval       no legacy-only masters.

  Questionnaire           Draft maintenance; version      Published versions
                          publish/retire; applicable      immutable; visit
                          version retrieval; answer       answers linked to the
                          validation/persistence          version used.

  Itinerary               Daily itinerary                 One Territory per daily
                          create/submit/approve/reject;   itinerary; selected
                          recurring-plan                  farms; recurring plan
                          definition/generation;          approval by SM.
                          retrieval                       

  Visit & Batch           Visit submission;               Submitted visit final;
                          unplanned-visit exception       repeat visits allowed;
                          approval; batch                 one breed per batch.
                          create/update/close;            
                          measurement retrieval           

  Expense                 Expense submission;             Assigned ASM's SM
                          approval/rejection; approved    approval; variable
                          amount capture; status          amount may be modified
                          retrieval                       by approver.

  Notification            Create SRS event notification;  In-app only; SRS-listed
                          recipient list; mark read       events only.

  Audit                   Append required audit event;    SRS event catalogue
                          authorized audit query          only; retention/access
                                                          to be specified.

  Sync                    Download changes; upload with   Server wins on
                          idempotency;                    conflict.
                          acknowledgement/error history   

  Reporting               Read procedures for SRS-listed  No additional reports
                          reports only                    without approved
                                                          change.
  -------------------------------------------------------------------------------

------------------------------------------------------------------------

## 12. Architecture and database conventions -- mandatory design constraints

All future database specifications and implementation work must respect
the latest authoritative decisions:

1.  **EF Core Code First** manages database schema and migrations only.
    Migrations must not create or alter Stored Procedures.
2.  **Stored Procedures only** for application CRUD and business data
    operations. No inline SQL or EF Core LINQ for application data.
3.  **Dapper** executes Stored Procedures through the approved
    `StoredProcedureExecutor` / `IStoredProcedureExecutor` and
    `IDbConnectionFactory`.
4.  **No physical foreign keys** in EF models, migrations or SQL
    artifacts. Logical referential integrity is enforced inside Stored
    Procedures.
5.  **`dbo` only.** All Stored Procedure references must be
    dbo-qualified.
6.  **Naming:** descriptive PascalCase identifiers, maximum SQL Server
    length 128 characters. There is no custom 15-character limit. Do not
    truncate names. Apply approved prefixes and abbreviations from
    `Database_Conventions.md`.
7.  **Audit identity:** `CreatedBy` / `ModifiedBy` use the approved user
    identity convention; clients must not submit audit actor IDs.
    Timestamps are UTC using `SYSUTCDATETIME()`.
8.  **System user:** controlled seed process; environment-specific
    `SystemUser:UserId`; never hard-code the ID.
9.  **Stored Procedure errors:** use the approved AD-09 error contract.
    Every business error number must be registered; raw SQL `THROW`
    messages must not be exposed to API clients.
10. **File storage:** no external sharing; file access must be
    authorized and use the approved secure on-premises storage design.
11. **Testing:** live SQL Server integration behaviour remains
    unverified until a dedicated non-production SQL Server test database
    is provisioned. Never use production data for integration tests.

> The architecture and naming rules above constrain implementation; they
> do not fill missing business schema definitions.

------------------------------------------------------------------------

## 13. Recommended next steps

  ----------------------------------------------------------------------------------------
                         Order Work item             Dependency / exit condition
  ---------------------------- --------------------- -------------------------------------
                             1 Reconcile Module 01   Specification owner reviews missing
                               specification         fields, ProcessId, assignment
                               findings DBG-001--014 references, naming-sheet
                                                     inconsistencies, audit coverage,
                                                     procedures and seed requirements.
                                                     Publish a controlled workbook
                                                     revision if approved.

                             2 Produce Module 02 --  Use CQ-006/007/031/046; settle
                               Geography &           assignment cardinality, overlap,
                               Assignment database   history and pending-approval routing.
                               specification         

                             3 Produce Farm & Master Use
                               Data database         CQ-022/027/028/029/039/043/044/045;
                               specifications        obtain approved SRS master lists and
                                                     confirm duplicate/code rules.

                             4 Produce Questionnaire Use CQ-015--020; obtain the approved
                               database              forms and define immutable
                               specification         versioning, conditional logic and
                                                     Data Set answers.

                             5 Produce Batch & Visit Use CQ-013/015/021/023/026/041; map
                               database              fixed SRS visit fields and formulas.
                               specifications        

                             6 Produce Itinerary &   Use CQ-008/010/011/012/030/031;
                               Expense database      resolve recurrence and approval
                               specifications        history.

                             7 Produce Sync, Audit,  Use CQ-024/032/035/036/038/040 and
                               Notification and File approved security design; define
                               Storage               event catalogue, retention,
                               specifications        idempotency and file controls.

                             8 Review Stored         Every approved FR maps to a specified
                               Procedure inventory   database operation; procedure names
                               and implementation    and error codes are approved before
                               readiness             implementation.
  ----------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 14. Open decisions and dependencies

The following items must remain visible in the project register. Do not
silently decide them in database implementation:

-   ASM assignment cardinality and overlap across Regions/Territories;
    effective dates and history.
-   Approval routing when an ASM's reporting relationship or area
    assignment changes while an approval is pending.
-   Whether Province/District determine Region/Territory or are
    independently assigned to a farm.
-   Farm Code format, generation timing and duplicate detection.
-   Exact questionnaire applicability rules and Previous Data Reference
    behaviour.
-   Data Set Input row/column structure from the approved forms.
-   Batch lifecycle, close/reopen rules and measurement ownership.
-   Exact recurring itinerary pattern, generation horizon and
    amendment/cancellation rules.
-   Unplanned-visit exception approval states and routing snapshot.
-   Expense proof requirements, variable amount rules and post-approval
    changes.
-   Audit retention period, authorized readers and precise
    event-to-entity mapping.
-   Offline idempotency key, version token, retry and partial-sync
    contract.
-   Secure on-premises file storage design, versioning, retention,
    backup and access controls.
-   Business timezone and business-date boundary for daily operations.
-   Module 01 specification owner review and controlled correction of
    workbook inconsistencies.
-   Security approval of the reversible AES-256-GCM design. This is a
    pending security dependency, not an endorsement; passwords must not
    be stored reversibly.

------------------------------------------------------------------------

## 15. Version history

  ------------------------------------------------------------------------
  Version                 Date                    Changes
  ----------------------- ----------------------- ------------------------
  0.1                     04-11-2026              Initial gap analysis
                                                  against Module 01
                                                  database specification,
                                                  functional requirements,
                                                  legacy database and
                                                  prototype evidence.

  **0.2**                 **05-11-2026**          Reassessed all 46
                                                  retained gap IDs against
                                                  CQ-001--CQ-046, Business
                                                  Workflow Analysis, UI
                                                  mapping, Requirements
                                                  Traceability Matrix,
                                                  Architecture Decisions
                                                  v2 and Database
                                                  Conventions. Updated
                                                  clarified/out-of-scope
                                                  items, corrected
                                                  outdated assumptions,
                                                  preserved unresolved
                                                  design dependencies, and
                                                  reinforced the rule that
                                                  no business-module
                                                  schema is approved by
                                                  this analysis.
  ------------------------------------------------------------------------

------------------------------------------------------------------------

**Document control:** This document is an analysis and planning
artifact. It does not authorize schema changes, EF migrations, Stored
Procedure creation, data migration or production deployment. Each module
requires its own reviewed and approved database specification before
implementation.
