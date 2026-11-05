# CGE Farm Monitoring System

# Functional Requirements

| Document Information | Details                                                 |
| -------------------- | ------------------------------------------------------- |
| Document Name        | Functional_Requirements.md                              |
| Project              | CGE Farm Monitoring System                              |
| Version              | 0.1                                                     |
| Prepared Date        | 04 November 2026                                        |
| Status               | Draft – Pending Stakeholder Review                      |
| Related Document     | Software Requirements Specification (SRS) – Version 0.2 |
| Scope                | Breeder Farm Monitoring – Livestock/Poultry             |

---

## 1. Purpose

This document defines the functional requirements of the CGE Farm Monitoring System (Farm FMS).

It expands the high-level requirements in the SRS into module-wise functional requirements that can be used for:

- System design and development.
- Database design.
- API and UI development.
- Functional testing.
- User Acceptance Testing (UAT).
- Requirements traceability.

This document identifies requirements, their expected behaviour, responsible actors, and current clarification status.

It does not independently approve business rules that have not been confirmed by CGE stakeholders.

## 2. Requirement Classification

### 2.1 Requirement Status

| Status              | Description                                                                                                            |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Confirmed           | Supported by the source SRS or an explicitly confirmed project decision.                                               |
| Needs Clarification | Requirement exists, but its behaviour, ownership, or business rules are incomplete or conflicting.                     |
| Missing             | A capability may be needed for end-to-end operation, but it is not sufficiently defined in the available requirements. |
| Future Phase        | Requirement is intended for a subsequent delivery, primarily the Mobile Application.                                   |

### 2.2 Delivery Classification

| Phase        | Description                                          |
| ------------ | ---------------------------------------------------- |
| Phase 1      | Web Application and shared backend services.         |
| Future Phase | Mobile Application and mobile-specific capabilities. |

A requirement marked Future Phase remains part of the documented product requirements but is not included in the initial Web Application delivery unless separately agreed.

### 2.3 Requirement ID Convention

Each requirement has a unique identifier.

| Prefix | Module                                           |
| ------ | ------------------------------------------------ |
| FR-UM  | User Management                                  |
| FR-AH  | Area & Hierarchy Management                      |
| FR-FM  | Farm Management                                  |
| FR-MD  | Master Data Management                           |
| FR-IP  | Itinerary Planning & Approval                    |
| FR-VQ  | Visit Questionnaire                              |
| FR-VC  | Visit Data Capture                               |
| FR-BM  | Batch/Flock Monitoring & Calculations            |
| FR-EX  | Expense Management                               |
| FR-RP  | Reporting & Analytics                            |
| FR-AU  | Audit & Logging                                  |
| FR-NT  | Notifications & Approvals                        |
| FR-OF  | Offline Capability                               |
| FR-XT  | Requirements identified outside the original SRS |

## 3. User Roles

Farm FMS shall support the following three primary system roles.

| Role                     | Description                                                                                   |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| Administrator            | System administration, user management, configuration, and authorized master data management. |
| Sales Manager (SM)       | Management oversight, operational monitoring, approval activities, and reporting.             |
| Area Sales Manager (ASM) | Assigned-area farm monitoring and field operational activities.                               |

**Role constraints**

1. Only Administrator, Sales Manager, and Area Sales Manager are defined as primary system roles.
2. A separate Supervisor role shall not be introduced.
3. No separate Supervisor login, permission group, or user designation shall be created.
4. References to Supervisor in older source materials are not adopted as an additional system role.
5. Detailed permissions and approval responsibilities must be defined within the three confirmed roles.

## 4. User Management (UM)

### 4.1 Authentication and Access Control

| ID        | Functional Requirement                                                                                                    | Actor         | Phase   | Status    |
| --------- | ------------------------------------------------------------------------------------------------------------------------- | ------------- | ------- | --------- |
| FR-UM-001 | The system shall allow registered users to authenticate using their assigned username and password.                       | All users     | Phase 1 | Confirmed |
| FR-UM-002 | The system shall enforce Role-Based Access Control (RBAC).                                                                | System        | Phase 1 | Confirmed |
| FR-UM-003 | The system shall support Administrator, Sales Manager, and Area Sales Manager roles.                                      | System        | Phase 1 | Confirmed |
| FR-UM-004 | The system shall restrict access to functions and information according to the user's assigned permissions.               | System        | Phase 1 | Confirmed |
| FR-UM-005 | The system shall support assigning one or more roles to a user according to the approved Security & Access specification. | Administrator | Phase 1 | Confirmed |
| FR-UM-006 | The system shall validate user authorization for protected operations.                                                    | System        | Phase 1 | Confirmed |

### 4.2 User Account Management

| ID        | Functional Requirement                                                                                              | Actor         | Phase   | Status              |
| --------- | ------------------------------------------------------------------------------------------------------------------- | ------------- | ------- | ------------------- |
| FR-UM-007 | The Administrator shall be able to create user accounts.                                                            | Administrator | Phase 1 | Confirmed           |
| FR-UM-008 | The Administrator shall be able to view and modify user account information.                                        | Administrator | Phase 1 | Confirmed           |
| FR-UM-009 | The Administrator shall be able to activate and deactivate user accounts.                                           | Administrator | Phase 1 | Confirmed           |
| FR-UM-010 | The system shall maintain user information including Name, Role, Email, Contact Number, User ID, and Active Status. | Administrator | Phase 1 | Confirmed           |
| FR-UM-011 | The system shall maintain relevant user account history.                                                            | System        | Phase 1 | Confirmed           |
| FR-UM-012 | The Administrator shall have unrestricted access to authorized system functions.                                    | Administrator | Phase 1 | Confirmed           |
| FR-UM-013 | The system shall support menu management and role-to-menu permission assignment.                                    | Administrator | Phase 1 | Confirmed           |
| FR-UM-014 | The system shall support permissions such as View, Create, Edit, Delete, Approve, and Export where applicable.      | Administrator | Phase 1 | Confirmed           |
| FR-UM-015 | The system shall allow authorized administrators to configure system settings.                                      | Administrator | Phase 1 | Needs Clarification |

### 4.3 Login History and Security

| ID        | Functional Requirement                                                                  | Actor                | Phase                  | Status              |
| --------- | --------------------------------------------------------------------------------------- | -------------------- | ---------------------- | ------------------- |
| FR-UM-016 | The system shall maintain successful login, failed login, and logout history.           | System               | Phase 1                | Confirmed           |
| FR-UM-017 | The system shall enforce the approved password policy.                                  | System               | Phase 1                | Confirmed           |
| FR-UM-018 | The system shall lock accounts according to the approved failed-login policy.           | System               | Phase 1                | Confirmed           |
| FR-UM-019 | The system shall enforce the approved session timeout policy.                           | System               | Phase 1                | Confirmed           |
| FR-UM-020 | The system shall revoke active sessions when an account is deactivated.                 | System               | Phase 1                | Confirmed           |
| FR-UM-021 | The system shall support password management according to the approved security design. | Administrator / User | Phase 1                | Needs Clarification |
| FR-UM-022 | The system shall support the approved web and mobile access restrictions for each role. | System               | Phase 1 / Future Phase | Needs Clarification |

**Clarification required:** The original SRS and prototype contain differences in role responsibilities and platform access. Final platform permissions must be confirmed against the three-role model.

## 5. Area & Hierarchy Management (AH)

### 5.1 Organizational Hierarchy

| ID        | Functional Requirement                                                                                 | Actor         | Phase   | Status    |
| --------- | ------------------------------------------------------------------------------------------------------ | ------------- | ------- | --------- |
| FR-AH-001 | The system shall support mapping Area Sales Managers to Sales Managers.                                | Administrator | Phase 1 | Confirmed |
| FR-AH-002 | The system shall maintain the reporting relationship between Sales Managers and their assigned ASMs.   | System        | Phase 1 | Confirmed |
| FR-AH-003 | The system shall allow Sales Managers to view relevant operational information of their assigned ASMs. | Sales Manager | Phase 1 | Confirmed |
| FR-AH-004 | The system shall maintain the history of user hierarchy assignments.                                   | System        | Phase 1 | Confirmed |

### 5.2 Geographical Hierarchy

| ID        | Functional Requirement                                                                                    | Actor         | Phase   | Status              |
| --------- | --------------------------------------------------------------------------------------------------------- | ------------- | ------- | ------------------- |
| FR-AH-005 | The system shall maintain geographical information required for farm monitoring operations.               | Administrator | Phase 1 | Confirmed           |
| FR-AH-006 | The system shall support Region and Territory information.                                                | Administrator | Phase 1 | Needs Clarification |
| FR-AH-007 | The system shall support assigning ASMs to their authorized geographical areas.                           | Administrator | Phase 1 | Needs Clarification |
| FR-AH-008 | The system shall restrict ASM access to the approved geographical assignments.                            | System        | Phase 1 | Needs Clarification |
| FR-AH-009 | The system shall support geographical information for farm assignment, itinerary planning, and reporting. | System        | Phase 1 | Needs Clarification |

**Clarification required:**

- Final geographical hierarchy: Province, District, Region, and Territory.
- Whether an ASM can be assigned to multiple Regions or Territories.
- Whether multiple ASMs can be assigned to the same Territory.
- Whether route levels such as Main Route and Sub Route are required.
- Whether reporting hierarchy and geographical hierarchy are maintained separately.

## 6. Farm Management (FM)

### 6.1 Farm Information Management

| ID        | Functional Requirement                                                                                 | Actor              | Phase   | Status              |
| --------- | ------------------------------------------------------------------------------------------------------ | ------------------ | ------- | ------------------- |
| FR-FM-001 | The system shall maintain breeder farm master information.                                             | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-002 | The system shall maintain Farm Code.                                                                   | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-003 | The system shall maintain Farm Name and Farm Address.                                                  | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-004 | The system shall maintain Farm Owner Name and Owner Contact Number.                                    | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-005 | The system shall maintain the Dealer Indicator for relevant farms.                                     | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-006 | The system shall maintain Province, District, Region, and Territory information associated with farms. | Administrator / SM | Phase 1 | Needs Clarification |
| FR-FM-007 | The system shall support farm activation and deactivation.                                             | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-008 | The system shall allow authorized users to view and update farm information.                           | Administrator / SM | Phase 1 | Confirmed           |
| FR-FM-009 | The system shall provide access to historical visit information associated with a farm.                | Authorized users   | Phase 1 | Confirmed           |
| FR-FM-010 | The system shall support the required farm and dealer farm association.                                | Administrator / SM | Phase 1 | Needs Clarification |

### 6.2 New Farm Registration

| ID        | Functional Requirement                                                                         | Actor                  | Phase                  | Status              |
| --------- | ---------------------------------------------------------------------------------------------- | ---------------------- | ---------------------- | ------------------- |
| FR-FM-011 | The system shall support capturing new farm information when a new breeder farm is identified. | ASM / Authorized users | Future Phase / Phase 1 | Confirmed           |
| FR-FM-012 | The system shall support capturing new farm information through the Mobile Application.        | ASM                    | Future Phase           | Confirmed           |
| FR-FM-013 | The system shall support reviewing mobile-created farm records through the Web Application.    | Administrator / SM     | Phase 1                | Confirmed           |
| FR-FM-014 | The system shall support approving mobile-created farms through the New Farm Approval process. | Administrator / SM     | Phase 1                | Confirmed           |
| FR-FM-015 | The system shall activate a mobile-created farm after the required approval.                   | System                 | Phase 1 / Future Phase | Confirmed           |
| FR-FM-016 | The system shall allow the assigned ASM to plan and record visits for an approved farm.        | ASM                    | Future Phase           | Confirmed           |
| FR-FM-017 | The system shall support rejecting a new farm submission.                                      | Administrator / SM     | Phase 1                | Needs Clarification |
| FR-FM-018 | The system shall support recording rejection reasons and notifying the relevant ASM.           | System                 | Phase 1 / Future Phase | Missing             |
| FR-FM-019 | The system shall support correcting and resubmitting rejected farm submissions.                | ASM                    | Future Phase           | Missing             |

### 6.3 Additional Farm Information

| ID        | Functional Requirement                                                             | Actor            | Phase                  | Status              |
| --------- | ---------------------------------------------------------------------------------- | ---------------- | ---------------------- | ------------------- |
| FR-FM-020 | The system shall support farm geolocation information where required.              | Authorized users | Phase 1                | Missing             |
| FR-FM-021 | The system shall support maintaining multiple contacts for a farm where required.  | Authorized users | Phase 1                | Needs Clarification |
| FR-FM-022 | The system shall support duplicate farm detection during new farm registration.    | System           | Phase 1 / Future Phase | Missing             |
| FR-FM-023 | The system shall support farm classification according to approved business rules. | Administrator    | Phase 1                | Needs Clarification |

**Clarification required:** Farm Type, dealer association, duplicate detection criteria, GPS capture, and rejected farm resubmission rules.

## 7. Master Data Management (MD)

| ID        | Functional Requirement                                                                | Actor         | Phase   | Status              |
| --------- | ------------------------------------------------------------------------------------- | ------------- | ------- | ------------------- |
| FR-MD-001 | The Administrator shall be able to create, modify, and maintain required master data. | Administrator | Phase 1 | Confirmed           |
| FR-MD-002 | The system shall support maintaining feed types.                                      | Administrator | Phase 1 | Confirmed           |
| FR-MD-003 | The system shall support maintaining water availability options.                      | Administrator | Phase 1 | Confirmed           |
| FR-MD-004 | The system shall support maintaining housing types.                                   | Administrator | Phase 1 | Confirmed           |
| FR-MD-005 | The system shall support maintaining grading options where required.                  | Administrator | Phase 1 | Confirmed           |
| FR-MD-006 | The system shall support maintaining breed information.                               | Administrator | Phase 1 | Needs Clarification |
| FR-MD-007 | The system shall support mapping breeder farms with relevant breeds.                  | Administrator | Phase 1 | Needs Clarification |
| FR-MD-008 | The system shall support maintaining Province and District master information.        | Administrator | Phase 1 | Needs Clarification |

The complete master data list, mandatory fields, relationships, and validation rules must be confirmed before the relevant modules are finalized.

## 8. Itinerary Planning & Approval (IP)

### 8.1 Itinerary Planning

| ID        | Functional Requirement                                                                           | Actor            | Phase                  | Status              |
| --------- | ------------------------------------------------------------------------------------------------ | ---------------- | ---------------------- | ------------------- |
| FR-IP-001 | The system shall support planning farm visit itineraries.                                        | ASM              | Future Phase           | Confirmed           |
| FR-IP-002 | The system shall support creating daily farm visit itineraries.                                  | ASM              | Future Phase           | Needs Clarification |
| FR-IP-003 | The system shall allow users to select eligible farms for an itinerary.                          | ASM              | Future Phase           | Confirmed           |
| FR-IP-004 | The system shall validate selected farms against the user's authorized geographical assignments. | System           | Future Phase           | Needs Clarification |
| FR-IP-005 | The system shall maintain itinerary date and relevant farm visit information.                    | System           | Phase 1 / Future Phase | Confirmed           |
| FR-IP-006 | The system shall support viewing planned itineraries.                                            | Authorized users | Phase 1 / Future Phase | Confirmed           |
| FR-IP-007 | The system shall maintain itinerary status information.                                          | System           | Phase 1                | Needs Clarification |
| FR-IP-008 | The system shall maintain itinerary history.                                                     | System           | Phase 1                | Needs Clarification |

### 8.2 Itinerary Review and Approval

| ID        | Functional Requirement                                                                | Actor                     | Phase                  | Status              |
| --------- | ------------------------------------------------------------------------------------- | ------------------------- | ---------------------- | ------------------- |
| FR-IP-009 | The system shall support reviewing submitted itineraries through the Web Application. | Authorized approver       | Phase 1                | Confirmed           |
| FR-IP-010 | The system shall support approving itineraries.                                       | Authorized approver       | Phase 1                | Confirmed           |
| FR-IP-011 | The system shall support rejecting itineraries.                                       | Authorized approver       | Phase 1                | Needs Clarification |
| FR-IP-012 | The system shall maintain approval decisions and related status information.          | System                    | Phase 1                | Confirmed           |
| FR-IP-013 | The system shall notify the relevant ASM of itinerary approval or rejection outcomes. | System                    | Phase 1 / Future Phase | Confirmed           |
| FR-IP-014 | The system shall support modifying itineraries according to approved business rules.  | ASM / Authorized approver | Phase 1 / Future Phase | Needs Clarification |

**Clarification required:**

- Daily, weekly, or monthly planning.
- One Territory per day or multiple Territories per day.
- Whether recurring itineraries are required.
- Who creates, reviews, and approves itineraries among Administrator, SM, and ASM.
- Rejection, modification, and resubmission behaviour.
- Whether unplanned visits outside assigned areas are permitted.

## 9. Visit Questionnaire (VQ)

### 9.1 Questionnaire Configuration

| ID        | Functional Requirement                                                                        | Actor            | Phase   | Status              |
| --------- | --------------------------------------------------------------------------------------------- | ---------------- | ------- | ------------------- |
| FR-VQ-001 | The system shall support defining questionnaires for farm visit activities.                   | Authorized users | Phase 1 | Confirmed           |
| FR-VQ-002 | The system shall support defining questionnaire questions.                                    | Authorized users | Phase 1 | Confirmed           |
| FR-VQ-003 | The system shall support defining expected response types for questions.                      | Authorized users | Phase 1 | Confirmed           |
| FR-VQ-004 | The system shall support organizing questions into relevant sections.                         | Authorized users | Phase 1 | Needs Clarification |
| FR-VQ-005 | The system shall support associating questionnaires with relevant farm monitoring activities. | Authorized users | Phase 1 | Needs Clarification |
| FR-VQ-006 | The system shall allow authorized users to maintain questionnaire definitions.                | Authorized users | Phase 1 | Confirmed           |
| FR-VQ-007 | The system shall support making configured questionnaires available for visit data capture.   | System           | Phase 1 | Confirmed           |
| FR-VQ-008 | The system shall retain questionnaire responses against the relevant farm visit.              | System           | Phase 1 | Confirmed           |

### 9.2 Questionnaire Behaviour

| ID        | Functional Requirement                                                                                        | Actor            | Phase                  | Status              |
| --------- | ------------------------------------------------------------------------------------------------------------- | ---------------- | ---------------------- | ------------------- |
| FR-VQ-009 | The system shall support the questionnaire flow required for farm monitoring visits.                          | ASM              | Future Phase           | Confirmed           |
| FR-VQ-010 | The system shall validate questionnaire responses according to configured rules.                              | System           | Phase 1 / Future Phase | Needs Clarification |
| FR-VQ-011 | The system shall support questionnaire version management where required.                                     | Authorized users | Phase 1                | Needs Clarification |
| FR-VQ-012 | The system shall preserve historical responses associated with the questionnaire used at the time of capture. | System           | Phase 1                | Needs Clarification |
| FR-VQ-013 | The system shall support conditional questions where approved by the business.                                | Authorized users | Phase 1 / Future Phase | Needs Clarification |

**Clarification required:**

- Existing questionnaire forms must be provided.
- Confirm whether visit fields are fixed system fields or configurable questionnaire questions.
- Confirm questionnaire versioning and publishing.
- Confirm conditional question behaviour.
- Confirm whether published questionnaires can be edited.

## 10. Visit Data Capture (VC)

### 10.1 Visit Information

| ID        | Functional Requirement                                                                      | Actor  | Phase        | Status              |
| --------- | ------------------------------------------------------------------------------------------- | ------ | ------------ | ------------------- |
| FR-VC-001 | The system shall support recording farm visit information.                                  | ASM    | Future Phase | Confirmed           |
| FR-VC-002 | The system shall associate a visit with the relevant farm.                                  | System | Future Phase | Confirmed           |
| FR-VC-003 | The system shall maintain visit date and relevant visit details.                            | System | Future Phase | Confirmed           |
| FR-VC-004 | The system shall support planned and unplanned visits according to approved business rules. | ASM    | Future Phase | Needs Clarification |
| FR-VC-005 | The system shall support capturing farm monitoring information during visits.               | ASM    | Future Phase | Confirmed           |
| FR-VC-006 | The system shall support capturing relevant visit observations.                             | ASM    | Future Phase | Confirmed           |
| FR-VC-007 | The system shall support capturing relevant farm type information during visits.            | ASM    | Future Phase | Needs Clarification |
| FR-VC-008 | The system shall support capturing relevant feed information.                               | ASM    | Future Phase | Confirmed           |
| FR-VC-009 | The system shall support capturing water availability information.                          | ASM    | Future Phase | Confirmed           |
| FR-VC-010 | The system shall support capturing housing information.                                     | ASM    | Future Phase | Confirmed           |
| FR-VC-011 | The system shall support capturing other required visit questionnaire responses.            | ASM    | Future Phase | Confirmed           |

### 10.2 Visit Attachments and Location

| ID        | Functional Requirement                                                                          | Actor            | Phase                  | Status              |
| --------- | ----------------------------------------------------------------------------------------------- | ---------------- | ---------------------- | ------------------- |
| FR-VC-012 | The system shall support attaching relevant images to visit records where required.             | ASM              | Future Phase           | Needs Clarification |
| FR-VC-013 | The system shall support capturing GPS information during visits where required.                | ASM              | Future Phase           | Needs Clarification |
| FR-VC-014 | The system shall maintain visit history for each farm.                                          | System           | Phase 1                | Confirmed           |
| FR-VC-015 | Authorized users shall be able to view previously recorded visit information.                   | Authorized users | Phase 1                | Confirmed           |
| FR-VC-016 | The system shall support capturing relevant batch/flock information during visits.              | ASM              | Future Phase           | Confirmed           |
| FR-VC-017 | The system shall support recording feed usage and related monitoring data.                      | ASM              | Future Phase           | Confirmed           |
| FR-VC-018 | The system shall validate visit data according to approved mandatory fields and business rules. | System           | Future Phase           | Needs Clarification |
| FR-VC-019 | The system shall maintain the relevant visit submission and review status.                      | System           | Phase 1 / Future Phase | Needs Clarification |

The detailed visit field inventory, validation rules, questionnaire flow, and visit lifecycle must be confirmed before implementation.

## 11. Batch/Flock Monitoring & Calculations (BM)

### 11.1 Batch/Flock Management

| ID        | Functional Requirement                                                                         | Actor            | Phase                  | Status              |
| --------- | ---------------------------------------------------------------------------------------------- | ---------------- | ---------------------- | ------------------- |
| FR-BM-001 | The system shall support maintaining multiple batches for a farm.                              | ASM              | Future Phase           | Confirmed           |
| FR-BM-002 | The system shall allow creating a new batch without requiring existing batches to be closed.   | ASM              | Future Phase           | Confirmed           |
| FR-BM-003 | The system shall maintain batch information associated with the relevant farm.                 | System           | Phase 1 / Future Phase | Confirmed           |
| FR-BM-004 | The system shall make previously entered batch information available during subsequent visits. | ASM              | Future Phase           | Confirmed           |
| FR-BM-005 | The system shall support updating batch monitoring information across visits.                  | ASM              | Future Phase           | Confirmed           |
| FR-BM-006 | The system shall maintain batch history.                                                       | System           | Phase 1                | Confirmed           |
| FR-BM-007 | The system shall support batch closure according to approved business rules.                   | Authorized users | Phase 1 / Future Phase | Needs Clarification |

### 11.2 Automated Calculations

| ID        | Functional Requirement                                                                 | Actor            | Phase   | Status              |
| --------- | -------------------------------------------------------------------------------------- | ---------------- | ------- | ------------------- |
| FR-BM-008 | The system shall calculate batch age using the flock start date.                       | System           | Phase 1 | Confirmed           |
| FR-BM-009 | The system shall support the approved age display units for relevant farm types.       | System           | Phase 1 | Needs Clarification |
| FR-BM-010 | The system shall calculate Feed Conversion Ratio (FCR) using the approved formula.     | System           | Phase 1 | Needs Clarification |
| FR-BM-011 | The system shall calculate mortality using the approved formula.                       | System           | Phase 1 | Confirmed           |
| FR-BM-012 | The system shall update relevant calculations when required source data changes.       | System           | Phase 1 | Needs Clarification |
| FR-BM-013 | The system shall support viewing batch-level performance information.                  | Authorized users | Phase 1 | Confirmed           |
| FR-BM-014 | The system shall maintain historical batch performance information.                    | System           | Phase 1 | Confirmed           |
| FR-BM-015 | The system shall support additional production metrics where approved by the business. | System           | Phase 1 | Needs Clarification |

**Known formulas from the original SRS:**

- FCR = Cumulative Feed ÷ Mean Body Weight.
- Mortality = Dead Animals ÷ Initial Count.

The exact formula inputs, units, percentage presentation, recalculation rules, and treatment of different farm types must be validated before implementation.

## 12. Expense Management (EX)

### 12.1 Expense Configuration

| ID        | Functional Requirement                                                          | Actor         | Phase   | Status    |
| --------- | ------------------------------------------------------------------------------- | ------------- | ------- | --------- |
| FR-EX-001 | The system shall support maintaining Expense Types.                             | Administrator | Phase 1 | Confirmed |
| FR-EX-002 | The system shall categorize expenses as Fixed or Variable.                      | Administrator | Phase 1 | Confirmed |
| FR-EX-003 | The system shall allow configuring a predefined amount for Fixed Expense Types. | Administrator | Phase 1 | Confirmed |
| FR-EX-004 | The system shall support an image-proof requirement for each Expense Type.      | Administrator | Phase 1 | Confirmed |

### 12.2 Expense Recording

| ID        | Functional Requirement                                                                 | Actor  | Phase        | Status              |
| --------- | -------------------------------------------------------------------------------------- | ------ | ------------ | ------------------- |
| FR-EX-005 | The system shall allow ASMs to record expenses associated with field activities.       | ASM    | Future Phase | Confirmed           |
| FR-EX-006 | The system shall automatically populate the predefined amount for Fixed Expense Types. | System | Future Phase | Confirmed           |
| FR-EX-007 | The system shall allow ASMs to enter the amount for Variable Expense Types.            | ASM    | Future Phase | Confirmed           |
| FR-EX-008 | The system shall support capturing and uploading image proof when required.            | ASM    | Future Phase | Confirmed           |
| FR-EX-009 | The system shall validate expense information according to configured criteria.        | System | Future Phase | Needs Clarification |

### 12.3 Expense Approval

| ID        | Functional Requirement                                                                                                                     | Actor               | Phase                  | Status              |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------ | ------------------- | ---------------------- | ------------------- |
| FR-EX-010 | The system shall support reviewing submitted ASM expenses through the Web Application.                                                     | Authorized approver | Phase 1                | Confirmed           |
| FR-EX-011 | The system shall support approving submitted expenses.                                                                                     | Authorized approver | Phase 1                | Confirmed           |
| FR-EX-012 | The system shall support rejecting submitted expenses.                                                                                     | Authorized approver | Phase 1                | Confirmed           |
| FR-EX-013 | The system shall maintain expense approval status and decision information.                                                                | System              | Phase 1                | Confirmed           |
| FR-EX-014 | The system shall support modifying or confirming the reimbursable amount for variable expenses where permitted by approved business rules. | Authorized approver | Phase 1                | Needs Clarification |
| FR-EX-015 | The system shall notify the ASM about expense approval outcomes.                                                                           | System              | Phase 1 / Future Phase | Confirmed           |
| FR-EX-016 | The system shall maintain expense history.                                                                                                 | System              | Phase 1                | Needs Clarification |

**Scope exclusion:** Financial processing, accounting, payroll, and automated reimbursement are outside the current project scope.

## 13. Reporting & Analytics (RP)

### 13.1 Farm and Batch Reports

| ID        | Functional Requirement                                                              | Actor            | Phase   | Status    |
| --------- | ----------------------------------------------------------------------------------- | ---------------- | ------- | --------- |
| FR-RP-001 | The system shall provide farm-level monitoring reports.                             | Admin / SM / ASM | Phase 1 | Confirmed |
| FR-RP-002 | The system shall provide farm visit history reports.                                | Admin / SM / ASM | Phase 1 | Confirmed |
| FR-RP-003 | The system shall provide production and feed usage information in relevant reports. | Admin / SM       | Phase 1 | Confirmed |
| FR-RP-004 | The system shall provide mortality and performance information in relevant reports. | Admin / SM       | Phase 1 | Confirmed |
| FR-RP-005 | The system shall provide batch-level monitoring reports.                            | Admin / SM / ASM | Phase 1 | Confirmed |
| FR-RP-006 | The system shall support viewing historical batch performance.                      | Admin / SM / ASM | Phase 1 | Confirmed |

### 13.2 Dashboards and Filters

| ID        | Functional Requirement                                                                      | Actor            | Phase   | Status              |
| --------- | ------------------------------------------------------------------------------------------- | ---------------- | ------- | ------------------- |
| FR-RP-007 | The system shall provide dashboards for relevant user roles.                                | Admin / SM / ASM | Phase 1 | Confirmed           |
| FR-RP-008 | The system shall support ASM-wise and geographical reporting where approved.                | Admin / SM       | Phase 1 | Needs Clarification |
| FR-RP-009 | The system shall support filtering reports by relevant dates.                               | Authorized users | Phase 1 | Needs Clarification |
| FR-RP-010 | The system shall support filtering reports by Farm Type and Feed Type where required.       | Authorized users | Phase 1 | Needs Clarification |
| FR-RP-011 | The system shall support management reporting based on authorized geographical assignments. | Admin / SM       | Phase 1 | Confirmed           |
| FR-RP-012 | The system shall support relevant performance and analytical parameters.                    | Admin            | Phase 1 | Needs Clarification |
| FR-RP-013 | The system shall support map-based visit visualization where approved.                      | Admin / SM       | Phase 1 | Needs Clarification |
| FR-RP-014 | The system shall support exporting or printing reports where approved.                      | Authorized users | Phase 1 | Missing             |

The complete report inventory, dashboard KPIs, report filters, grouping, export formats, and calculation definitions require stakeholder confirmation.

## 14. Audit & Logging (AU)

| ID        | Functional Requirement                                                                              | Actor  | Phase   | Status    |
| --------- | --------------------------------------------------------------------------------------------------- | ------ | ------- | --------- |
| FR-AU-001 | The system shall maintain audit records of user creation and modification.                          | System | Phase 1 | Confirmed |
| FR-AU-002 | The system shall maintain audit records of user login and logout activities.                        | System | Phase 1 | Confirmed |
| FR-AU-003 | The system shall maintain audit records of farm creation and modification.                          | System | Phase 1 | Confirmed |
| FR-AU-004 | The system shall maintain audit records of approval activities.                                     | System | Phase 1 | Confirmed |
| FR-AU-005 | The system shall maintain audit records of role and access changes.                                 | System | Phase 1 | Confirmed |
| FR-AU-006 | Each audit record shall contain the relevant user, date/time, action, and module information.       | System | Phase 1 | Confirmed |
| FR-AU-007 | The system shall maintain previous and updated values where applicable.                             | System | Phase 1 | Confirmed |
| FR-AU-008 | Audit records shall be read-only for normal application users.                                      | System | Phase 1 | Confirmed |
| FR-AU-009 | Access to audit information shall be restricted to authorized users.                                | System | Phase 1 | Confirmed |
| FR-AU-010 | The system shall audit access to sensitive information according to approved security requirements. | System | Phase 1 | Confirmed |
| FR-AU-011 | The system shall support the approved audit retention and archival policy.                          | System | Phase 1 | Missing   |

The exact audit event list, retention period, archival rules, and authorized audit viewers must be confirmed.

## 15. Notifications & Approvals (NT)

### 15.1 In-app Notifications

| ID        | Functional Requirement                                                              | Actor            | Phase                  | Status    |
| --------- | ----------------------------------------------------------------------------------- | ---------------- | ---------------------- | --------- |
| FR-NT-001 | The system shall generate in-app notifications for relevant system events.          | System           | Phase 1                | Confirmed |
| FR-NT-002 | The system shall notify relevant ASMs about visit plan approval outcomes.           | System           | Phase 1 / Future Phase | Confirmed |
| FR-NT-003 | The system shall notify relevant ASMs about new farm approval outcomes.             | System           | Phase 1 / Future Phase | Confirmed |
| FR-NT-004 | The system shall notify relevant ASMs about expense approval outcomes.              | System           | Phase 1 / Future Phase | Confirmed |
| FR-NT-005 | The system shall notify relevant Sales Managers about pending visit plan approvals. | System           | Phase 1                | Confirmed |
| FR-NT-006 | The system shall notify relevant Sales Managers about pending new farm approvals.   | System           | Phase 1                | Confirmed |
| FR-NT-007 | The system shall notify relevant Sales Managers about pending expense approvals.    | System           | Phase 1                | Confirmed |
| FR-NT-008 | Users shall be able to view their notification history.                             | Authorized users | Phase 1 / Future Phase | Confirmed |
| FR-NT-009 | The system shall display relevant approval statuses in the application.             | System           | Phase 1 / Future Phase | Confirmed |

### 15.2 Additional Notifications

| ID        | Functional Requirement                                                                                    | Actor         | Phase                  | Status              |
| --------- | --------------------------------------------------------------------------------------------------------- | ------------- | ---------------------- | ------------------- |
| FR-NT-010 | The system shall support administrator-created messages to relevant field users where approved.           | Administrator | Phase 1 / Future Phase | Needs Clarification |
| FR-NT-011 | The system shall support notification attachments or images where approved.                               | Administrator | Phase 1 / Future Phase | Needs Clarification |
| FR-NT-012 | The system shall support alerts for upcoming visits and other relevant operational events where approved. | System        | Phase 1 / Future Phase | Needs Clarification |
| FR-NT-013 | The system shall support notification delivery through approved channels.                                 | System        | Phase 1                | Needs Clarification |

Notification channels, recipients, triggers, reminders, and escalation rules require confirmation.

## 16. Offline Capability (OF) – Future Phase

The following requirements relate to the subsequent Mobile Application delivery. They are not part of the initial Web Application delivery.

| ID        | Functional Requirement                                                                           | Actor  | Phase        | Status              |
| --------- | ------------------------------------------------------------------------------------------------ | ------ | ------------ | ------------------- |
| FR-OF-001 | The Mobile Application shall support offline operation when network connectivity is unavailable. | ASM    | Future Phase | Confirmed           |
| FR-OF-002 | The Mobile Application shall support storing relevant captured data locally while offline.       | ASM    | Future Phase | Confirmed           |
| FR-OF-003 | The Mobile Application shall support an Initial Sync operation.                                  | ASM    | Future Phase | Confirmed           |
| FR-OF-004 | Initial Sync shall download the approved master data required for field operations.              | System | Future Phase | Confirmed           |
| FR-OF-005 | The Mobile Application shall support a user-initiated Midday Sync operation.                     | ASM    | Future Phase | Confirmed           |
| FR-OF-006 | Midday Sync shall upload newly captured data and retrieve approved critical updates.             | System | Future Phase | Confirmed           |
| FR-OF-007 | The system shall support automatic synchronization when approved triggers occur.                 | System | Future Phase | Confirmed           |
| FR-OF-008 | The Mobile Application shall support a user-initiated Day-End Sync operation.                    | ASM    | Future Phase | Needs Clarification |
| FR-OF-009 | The system shall maintain synchronization status information.                                    | System | Future Phase | Confirmed           |
| FR-OF-010 | The system shall support handling synchronization failures and retries.                          | System | Future Phase | Needs Clarification |
| FR-OF-011 | The system shall detect conflicting updates between local and central data.                      | System | Future Phase | Missing             |
| FR-OF-012 | The system shall apply approved conflict-resolution rules during synchronization.                | System | Future Phase | Missing             |
| FR-OF-013 | The system shall support the approved GPS behaviour when the device is offline.                  | System | Future Phase | Needs Clarification |

The detailed offline data model, synchronization schedule, conflict-resolution strategy, retry behaviour, and day-end completion rules must be defined before Mobile Application implementation.

## 17. Requirements Outside the Original SRS (XT)

The following capabilities were identified in the Project Proposal, legacy feature list, or Figma prototypes. They are not sufficiently established in the original SRS and must not be treated as approved scope.

| ID        | Capability                                                                                                        | Source Evidence                              | Status              |
| --------- | ----------------------------------------------------------------------------------------------------------------- | -------------------------------------------- | ------------------- |
| FR-XT-001 | Attendance management, including daily check-in and check-out.                                                    | Legacy feature list and Mobile prototype     | Needs Clarification |
| FR-XT-002 | Farmer feedback and complaint management.                                                                         | Legacy feature list and prototypes           | Needs Clarification |
| FR-XT-003 | Company master management.                                                                                        | Legacy feature list and Web prototype        | Needs Clarification |
| FR-XT-004 | Separate Farm Monitor and Farm Evaluation processes.                                                              | Legacy feature list and prototype references | Needs Clarification |
| FR-XT-005 | Reassignable role-based accounts or position-based accounts.                                                      | Web prototype                                | Needs Clarification |
| FR-XT-006 | Day-End locking and Administrator unlocking.                                                                      | Legacy feature list                          | Needs Clarification |
| FR-XT-007 | Additional visit types such as Routine Check, Feed Quality Review, Batch Placement, and Pre-placement Inspection. | Web prototype                                | Needs Clarification |

These items require explicit scope decisions before being added to the approved functional baseline.

## 18. Cross-Module Business Rules and Dependencies

| ID     | Rule / Dependency                                                                                     | Related Modules     | Status                             |
| ------ | ----------------------------------------------------------------------------------------------------- | ------------------- | ---------------------------------- |
| BR-001 | Farm FMS is designed for breeder farm monitoring involving livestock/poultry.                         | FM, VC, BM          | Confirmed                          |
| BR-002 | General crop-based agricultural management is outside the project scope.                              | All                 | Confirmed                          |
| BR-003 | Only Administrator, SM, and ASM are primary system roles.                                             | UM, all modules     | Confirmed                          |
| BR-004 | A separate Supervisor system role shall not be created.                                               | UM, all modules     | Confirmed                          |
| BR-005 | Users shall access only the data and functions permitted by their roles and assignments.              | UM, AH, all modules | Confirmed                          |
| BR-006 | Farm monitoring information shall be associated with the relevant farm.                               | FM, VC, BM          | Confirmed                          |
| BR-007 | Batch/Flock information shall be associated with the relevant farm.                                   | FM, BM              | Confirmed                          |
| BR-008 | Visit information shall be retained for historical reference.                                         | VC, RP              | Confirmed                          |
| BR-009 | FCR, mortality, and age calculations shall follow approved business formulas.                         | BM, RP              | Needs Clarification                |
| BR-010 | Itinerary and expense approvals shall follow the approved responsibilities of the three system roles. | IP, EX, UM          | Needs Clarification                |
| BR-011 | Questionnaire responses shall be associated with the relevant farm visit.                             | VQ, VC              | Confirmed                          |
| BR-012 | Mobile offline synchronization shall follow approved synchronization and conflict-resolution rules.   | OF                  | Future Phase – Needs Clarification |

## 19. Open Clarifications

The following matters must be resolved before the corresponding requirements can be finalized.

| Priority | Area                | Required Clarification                                                               |
| -------- | ------------------- | ------------------------------------------------------------------------------------ |
| P1       | User Management     | Final permission boundaries of Administrator, SM, and ASM.                           |
| P1       | Area & Hierarchy    | Final geographical hierarchy and ASM assignment rules.                               |
| P1       | Farm Management     | New Farm Approval, rejection, and resubmission workflow.                             |
| P1       | Itinerary           | Planning period, geographical restrictions, and approval responsibilities.           |
| P1       | Visit Questionnaire | Fixed visit fields versus configurable questionnaire questions.                      |
| P1       | Batch/Flock         | Batch lifecycle, closure, and calculation inputs.                                    |
| P1       | Offline             | Synchronization scope and conflict handling before Mobile development.               |
| P2       | Farm Management     | Farm classification, dealer association, GPS, and duplicate detection.               |
| P2       | Master Data         | Complete master data inventory and ownership.                                        |
| P2       | Questionnaire       | Versioning, publishing, conditional questions, and historical responses.             |
| P2       | Visit Capture       | Complete visit field inventory, mandatory fields, and attachments.                   |
| P2       | Expense             | Approval responsibilities, amount modification, and rejection behaviour.             |
| P2       | Reporting           | Report inventory, KPIs, filters, and export formats.                                 |
| P2       | Notifications       | Channels, recipients, triggers, and reminders.                                       |
| P3       | Audit               | Retention, archival, and detailed audit viewing permissions.                         |
| P3       | Additional Scope    | Attendance, feedback, day-end lock, and other capabilities outside the original SRS. |

The detailed clarification questions and their decision owners shall be maintained in the Business Clarification Register.

## 20. Source Documents

| Reference | Document                                               | Usage                                                                  |
| --------- | ------------------------------------------------------ | ---------------------------------------------------------------------- |
| S1        | Software Requirement Specification v1.0 – 15 July 2026 | Primary source for functional requirements.                            |
| S2        | Project Proposal v1.1 – 13 July 2026                   | Project scope and supporting requirements.                             |
| S3        | Farm_FMS_Module_01_Database_Specification_v0.2.xlsx    | Approved Security & Access database baseline only.                     |
| S4        | DataBase Design.xlsx                                   | Legacy system reference only.                                          |
| S5        | Figma Web Prototype – 02 November 2026                 | UI reference only.                                                     |
| S6        | Figma Mobile Prototype – 02 November 2026              | UI reference only.                                                     |
| S7        | Farm_FMS_Requirements_Traceability_Matrix_v0.1.xlsx    | Supporting analysis; unverified decisions are not treated as approved. |
| S8        | Farm_FMS_Database_Gap_Analysis_v0.1.xlsx               | Supporting database gap analysis.                                      |
| S9        | FarmFMS_Architecture_Decisions_v2.md                   | Technical architecture decisions.                                      |
| S10       | Software Requirements Specification – Version 0.2      | Current working requirements baseline.                                 |

### Source Limitations

1. The original SRS and Proposal copies supplied for analysis contain blank signature and approval date fields. Their formal approval status cannot be verified from those copies.
2. The original SRS context diagram and UI screen section include image-only content that was not available as extracted text.
3. Figma prototypes are illustrative references and do not independently establish approved requirements.
4. Existing questionnaire forms were not supplied.
5. The legacy database design is historical evidence and is not an approved database baseline for the new system.
6. The approved database specification currently covers Module 01 Security & Access only.

## 21. Document Review and Approval

**Current Status:** Draft – Pending Stakeholder Review

This document is based on the available SRS, Project Proposal, approved Module 01 database specification, supporting analysis, and the confirmed project decisions recorded during requirements review.

The requirements marked Needs Clarification or Missing must be reviewed before they are treated as implementation-ready requirements.

| Review              | Responsibility                | Status  |
| ------------------- | ----------------------------- | ------- |
| Prepared By         | Project Team                  | Draft   |
| Requirements Review | Project Requester             | Pending |
| Business Review     | CGE Business Representative   | Pending |
| Technical Review    | PMS Technical Representative  | Pending |
| Final Approval      | Authorized CGE Representative | Pending |

**Approval note:** Review or confirmation in this ChatGPT conversation does not constitute formal CGE stakeholder approval.
