# CGE Farm Monitoring System

# Software Requirements Specification (SRS)

| Document Information | Details                                        |
| -------------------- | ---------------------------------------------- |
| Document Title       | Software Requirements Specification            |
| Project Name         | CGE Farm Monitoring System                     |
| Abbreviation         | Farm FMS                                       |
| Document Version     | 0.2                                            |
| Prepared Date        | 04 November 2026                               |
| Document Status      | Draft – Pending Formal Stakeholder Approval    |
| Business Owner       | Ceylon Grain Elevators PLC (CGE)               |
| Solution Provider    | Prima Management Services (Pvt.) Limited (PMS) |
| Related Document     | Project Scope & Overview – Version 0.2         |

---

## 1. Introduction

### 1.1 Purpose

The purpose of this Software Requirements Specification (SRS) is to define the functional and non-functional requirements of the CGE Farm Monitoring System.

This document establishes the expected system behaviour, user roles, business capabilities, operational requirements, and system constraints. It will serve as a reference for solution design, development, testing, and stakeholder validation.

The initial delivery will focus on the Web Application. The Mobile Application will be developed in a subsequent phase using the same backend services.

### 1.2 Project Overview

Farm FMS is a centralized digital platform designed to support breeder farm monitoring operations involving livestock/poultry.

The system will support farm information management, area and hierarchy management, itinerary planning, visit monitoring, batch/flock tracking, questionnaire-based data capture, expense management, reporting, and administrative activities.

The overall solution consists of:

- Web Application – First delivery.
- Backend API – Shared application services.
- Centralized database – Shared business data.
- Mobile Application – Subsequent delivery.

### 1.3 Intended Audience

**CGE stakeholders**

- Higher management.
- Sales and business representatives.
- Administrative representatives.
- IT representatives.
- Relevant operational stakeholders.

**PMS stakeholders**

- Project management.
- Project team lead.
- Business analysts.
- Software engineers.
- Quality assurance engineers.
- Technical and IT representatives.

### 1.4 Definitions and Abbreviations

| Term        | Description                                                 |
| ----------- | ----------------------------------------------------------- |
| CGE         | Ceylon Grain Elevators PLC                                  |
| PMS         | Prima Management Services (Pvt.) Limited                    |
| FMS         | Farm Monitoring System                                      |
| SM          | Sales Manager                                               |
| ASM         | Area Sales Manager                                          |
| RBAC        | Role-Based Access Control                                   |
| FCR         | Feed Conversion Ratio                                       |
| API         | Application Programming Interface                           |
| GPS         | Global Positioning System                                   |
| UI          | User Interface                                              |
| UAT         | User Acceptance Testing                                     |
| Batch/Flock | A group of livestock/poultry monitored as a production unit |
| Itinerary   | Planned schedule of farm visits                             |

### 1.5 Requirement Status Definitions

| Status              | Meaning                                                                                  |
| ------------------- | ---------------------------------------------------------------------------------------- |
| Confirmed           | Supported by the primary SRS or an explicitly confirmed project decision.                |
| Needs Clarification | Requirement exists, but its meaning, ownership, or behaviour is ambiguous or incomplete. |
| Pending Definition  | The source identifies a capability, but detailed rules are not yet available.            |
| Future Phase        | Intended for a subsequent delivery rather than the first Web Application delivery.       |

---

## 2. Overall Description

### 2.1 Product Perspective

Farm FMS will replace the existing legacy Farm Monitoring System with a modern centralized solution.

The system will provide a Web Application for management and administrative operations. A Mobile Application will be introduced in a subsequent phase to support field-level operations.

Both applications will use a shared backend API and centralized database.

### 2.2 Product Functions

The system is expected to provide:

1. User authentication and access management.
2. Area and organizational hierarchy management.
3. Farm and breeder information management.
4. Master data management.
5. Itinerary planning and approval.
6. Visit questionnaire configuration.
7. Farm visit data capture.
8. Batch/flock monitoring and calculations.
9. Expense management.
10. Reporting and analytics.
11. Audit and activity logging.
12. Notifications and approvals.
13. Mobile offline data capture and synchronization in the subsequent phase.

### 2.3 User Classes and Roles

The confirmed Farm FMS system role structure consists of exactly three roles.

| User Role          | Abbreviation | Description                                                                                                           |
| ------------------ | ------------ | --------------------------------------------------------------------------------------------------------------------- |
| Administrator      | Admin        | Responsible for system administration, user management, access configuration, and authorized system-level activities. |
| Sales Manager      | SM           | Responsible for management oversight, reviewing operational information, approvals, and reporting.                    |
| Area Sales Manager | ASM          | Responsible for assigned-area farm monitoring activities, visit-related operations, and data capture.                 |

**Role structure decision:**

- Farm FMS will contain only Administrator, Sales Manager, and Area Sales Manager as its primary system roles.
- A separate Supervisor role will not be created.
- No separate Supervisor login, permission group, or user designation will be introduced.
- References to Supervisor in the original Project Proposal and Figma prototypes are not adopted as additional Farm FMS system roles.
- Approval responsibilities and detailed permissions must be defined within the three confirmed roles.

The exact permissions and responsibilities of each role will be maintained in the Role & Permission Matrix.

### 2.4 Operating Environment

The system is expected to operate in:

- Web browser environment for the Web Application.
- Android mobile environment for the subsequent Mobile Application.
- Centralized application hosting environment.
- Microsoft SQL Server database environment.
- Separate Development, Testing, and Production environments.

Detailed supported browser versions, operating system versions, hosting specifications, and infrastructure requirements require final confirmation.

### 2.5 Design and Implementation Constraints

The confirmed technical direction is:

| Component               | Technology                        |
| ----------------------- | --------------------------------- |
| Backend                 | .NET 10 ASP.NET Core Web API      |
| Backend Architecture    | Clean Architecture                |
| Web Application         | Blazor Web App (.NET 10)          |
| UI Components           | Radzen and Bootstrap              |
| Database                | Microsoft SQL Server              |
| Application Data Access | Dapper with Stored Procedures     |
| Schema Management       | EF Core Code First and migrations |
| Mobile Integration      | Shared backend API                |

Additional constraints:

- The Web Application is the first delivery.
- The Mobile Application is a subsequent delivery.
- The system is designed for breeder farm monitoring, not general crop-based agriculture.
- External system integrations are excluded unless formally approved.
- Business calculations must follow approved business formulas.

### 2.6 Assumptions and Dependencies

- CGE stakeholders will provide business requirements and validation.
- Relevant users will provide accurate farm and monitoring information.
- Required business formulas and parameters will be provided and confirmed.
- Historical data will be provided where migration or historical analysis is required.
- Relevant stakeholders will participate in testing and UAT.
- Required infrastructure and deployment environments will be available.

---

## 3. Functional Requirements

### 3.1 User Management (UM)

The system shall provide user authentication, account administration, and access control.

| ID        | Requirement                                                                                   | Status             |
| --------- | --------------------------------------------------------------------------------------------- | ------------------ |
| FR-UM-001 | The system shall allow registered users to authenticate using their assigned credentials.     | Confirmed          |
| FR-UM-002 | The system shall enforce role-based access control.                                           | Confirmed          |
| FR-UM-003 | The system shall support Administrator, Sales Manager, and Area Sales Manager roles only.     | Confirmed          |
| FR-UM-004 | The system shall allow authorized administrators to create and manage user accounts.          | Confirmed          |
| FR-UM-005 | The system shall allow authorized administrators to assign roles to users.                    | Confirmed          |
| FR-UM-006 | The system shall restrict user access according to assigned permissions and responsibilities. | Confirmed          |
| FR-UM-007 | The system shall support user account activation and deactivation.                            | Confirmed          |
| FR-UM-008 | The system shall maintain relevant user login history and account activity records.           | Confirmed          |
| FR-UM-009 | The system shall support password management according to the approved security design.       | Pending Definition |
| FR-UM-010 | The system shall maintain user session information and enforce the approved session policy.   | Pending Definition |

**Clarifications:**

- Confirm whether users can hold multiple roles.
- Confirm how permissions are combined when multiple roles are assigned.
- Confirm password recovery, reset, and password visibility requirements.
- Finalize the responsibilities and permission boundaries of the three roles.

### 3.2 Area & Hierarchy Management (AH)

The system shall support geographical and organizational hierarchy management.

| ID        | Requirement                                                                                      | Status                                              |
| --------- | ------------------------------------------------------------------------------------------------ | --------------------------------------------------- |
| FR-AH-001 | The system shall maintain geographical area information required for farm monitoring operations. | Confirmed                                           |
| FR-AH-002 | The system shall support the geographical hierarchy required by the business.                    | Confirmed – hierarchy structure needs clarification |
| FR-AH-003 | The system shall support assigning users to relevant geographical areas.                         | Confirmed                                           |
| FR-AH-004 | The system shall restrict users to the data and operational areas authorized for them.           | Confirmed                                           |
| FR-AH-005 | The system shall allow authorized users to maintain area and assignment information.             | Pending Definition                                  |

**Clarifications:**

- Confirm the hierarchy structure, including Province, District, Region, and Territory.
- Confirm whether an ASM can be assigned to multiple Regions or Territories.
- Confirm whether reporting hierarchy and geographical hierarchy are separate structures.

### 3.3 Farm Management (FM)

The system shall provide centralized management of breeder farm information.

| ID        | Requirement                                                                   | Status              |
| --------- | ----------------------------------------------------------------------------- | ------------------- |
| FR-FM-001 | The system shall allow authorized users to register breeder farm information. | Confirmed           |
| FR-FM-002 | The system shall maintain farm identification and basic farm details.         | Confirmed           |
| FR-FM-003 | The system shall maintain farm owner and contact information.                 | Confirmed           |
| FR-FM-004 | The system shall maintain geographical information associated with a farm.    | Confirmed           |
| FR-FM-005 | The system shall support the farm classification required by the business.    | Pending Definition  |
| FR-FM-006 | The system shall allow authorized users to view and update farm information.  | Confirmed           |
| FR-FM-007 | The system shall support farm activation and deactivation.                    | Confirmed           |
| FR-FM-008 | The system shall maintain historical farm information where required.         | Pending Definition  |
| FR-FM-009 | The system shall support the required farm registration and approval process. | Needs Clarification |
| FR-FM-010 | The system shall support farm and breed mapping where applicable.             | Confirmed           |

**Farm information identified in the original requirements includes:**

- Farm Code.
- Farm Name.
- Address.
- Owner.
- Contact Information.
- Dealer Indicator.
- Province.
- District.
- Region.
- Territory.
- Active Status.

**Clarifications:**

- Confirm mandatory and optional farm fields.
- Confirm farm classification and dealer association rules.
- Confirm duplicate farm detection rules.
- Confirm farm registration approval and rejection workflow.
- Confirm GPS/location capture requirements.

### 3.4 Master Data Management (MD)

The system shall maintain the master information required by farm monitoring operations.

| ID        | Requirement                                                                                                    | Status             |
| --------- | -------------------------------------------------------------------------------------------------------------- | ------------------ |
| FR-MD-001 | The system shall support maintaining master data required by the farm monitoring process.                      | Confirmed          |
| FR-MD-002 | The system shall support breeder farm and breed mapping information.                                           | Confirmed          |
| FR-MD-003 | The system shall support maintaining relevant configuration and reporting parameters.                          | Confirmed          |
| FR-MD-004 | The system shall restrict master data maintenance to authorized users.                                         | Pending Definition |
| FR-MD-005 | The system shall maintain the relevant master data required for questionnaire and batch monitoring operations. | Pending Definition |

The complete master data inventory, fields, validation rules, and ownership must be finalized separately.

### 3.5 Itinerary Planning & Approval (IP)

The system shall support planning and reviewing farm visits.

| ID        | Requirement                                                                                    | Status                                        |
| --------- | ---------------------------------------------------------------------------------------------- | --------------------------------------------- |
| FR-IP-001 | The system shall support creating farm visit itineraries.                                      | Confirmed                                     |
| FR-IP-002 | The system shall allow users to include relevant farms in an itinerary.                        | Confirmed                                     |
| FR-IP-003 | The system shall support viewing planned itineraries.                                          | Confirmed                                     |
| FR-IP-004 | The system shall support reviewing and approving itineraries by authorized users.              | Confirmed – approver role needs clarification |
| FR-IP-005 | The system shall support the required itinerary rejection process.                             | Needs Clarification                           |
| FR-IP-006 | The system shall maintain itinerary status information.                                        | Pending Definition                            |
| FR-IP-007 | The system shall support viewing itinerary history.                                            | Pending Definition                            |
| FR-IP-008 | The system shall restrict itinerary information according to user assignments and permissions. | Confirmed                                     |

**Clarifications:**

- Confirm whether itineraries are daily, weekly, or monthly.
- Confirm whether recurring itineraries are required.
- Confirm whether users can modify approved itineraries.
- Confirm itinerary rejection and resubmission rules.
- Confirm which of the three roles creates, reviews, and approves itineraries.

### 3.6 Visit Questionnaire Configuration (VQ)

The system shall support configurable questionnaires for farm monitoring activities.

| ID        | Requirement                                                                              | Status              |
| --------- | ---------------------------------------------------------------------------------------- | ------------------- |
| FR-VQ-001 | The system shall support defining questionnaires for farm visit activities.              | Confirmed           |
| FR-VQ-002 | The system shall support associating questionnaires with relevant farm types.            | Confirmed           |
| FR-VQ-003 | The system shall support defining questions and their expected response types.           | Confirmed           |
| FR-VQ-004 | The system shall support configuring questionnaire sections where required.              | Pending Definition  |
| FR-VQ-005 | The system shall support the questionnaire flow required for farm visit data capture.    | Confirmed           |
| FR-VQ-006 | The system shall allow authorized users to maintain questionnaire definitions.           | Pending Definition  |
| FR-VQ-007 | The system shall support the required questionnaire publishing and availability process. | Needs Clarification |
| FR-VQ-008 | The system shall retain questionnaire responses associated with relevant farm visits.    | Confirmed           |

**Clarifications:**

- Confirm questionnaire versioning requirements.
- Confirm whether published questionnaires can be modified.
- Confirm whether historical responses must retain the questionnaire version used at the time of capture.
- Confirm conditional questions and dynamic question behaviour.
- Obtain the existing questionnaire forms that must be replicated.

### 3.7 Visit Data Capture (VC)

The system shall support recording farm monitoring information during farm visits.

| ID        | Requirement                                                                                       | Status              |
| --------- | ------------------------------------------------------------------------------------------------- | ------------------- |
| FR-VC-001 | The system shall support recording farm visit information.                                        | Confirmed           |
| FR-VC-002 | The system shall support capturing relevant farm monitoring data.                                 | Confirmed           |
| FR-VC-003 | The system shall support recording visit observations through the required questionnaire flow.    | Confirmed           |
| FR-VC-004 | The system shall support capturing relevant batch/flock information during visits.                | Confirmed           |
| FR-VC-005 | The system shall support recording feed usage and related monitoring information.                 | Confirmed           |
| FR-VC-006 | The system shall support capturing relevant images where required.                                | Needs Clarification |
| FR-VC-007 | The system shall support capturing GPS/location information where required.                       | Needs Clarification |
| FR-VC-008 | The system shall support planned and unplanned farm visits where permitted by the business rules. | Confirmed           |
| FR-VC-009 | The system shall maintain historical visit information.                                           | Confirmed           |
| FR-VC-010 | The system shall support viewing previously recorded visit information.                           | Confirmed           |

**Clarifications:**

- Confirm the complete list of visit data fields.
- Confirm mandatory fields and validation rules.
- Confirm whether visit capture is based entirely on configurable questionnaires or includes fixed system fields.
- Confirm visit submission, review, and approval requirements.
- Confirm image and GPS requirements.

### 3.8 Batch/Flock Monitoring & Calculations (BM)

The system shall support monitoring batch/flock information and relevant performance indicators.

| ID        | Requirement                                                                                                                       | Status                                |
| --------- | --------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| FR-BM-001 | The system shall support maintaining batch/flock information associated with breeder farms.                                       | Confirmed                             |
| FR-BM-002 | The system shall support recording relevant batch/flock monitoring data.                                                          | Confirmed                             |
| FR-BM-003 | The system shall support tracking batch/flock information across farm visits.                                                     | Confirmed                             |
| FR-BM-004 | The system shall support monitoring feed usage and related performance information.                                               | Confirmed                             |
| FR-BM-005 | The system shall support calculating relevant monitoring indicators, including FCR and mortality, according to approved formulas. | Confirmed – formulas pending approval |
| FR-BM-006 | The system shall support age determination where required by the business rules.                                                  | Confirmed                             |
| FR-BM-007 | The system shall support maintaining historical batch/flock information.                                                          | Confirmed                             |
| FR-BM-008 | The system shall support the required batch/flock lifecycle management process.                                                   | Needs Clarification                   |

**Clarifications:**

- Confirm batch creation and identification rules.
- Confirm whether multiple active batches can exist for one farm.
- Confirm batch closure and reopening rules.
- Confirm how historical batch information is maintained.
- Confirm the formulas and required inputs for all calculations.
- Confirm the distinction between Farm Monitoring and Farm Evaluation workflows.

### 3.9 Expense Management (EX)

The system shall support managing expenses associated with field operations.

| ID        | Requirement                                                                             | Status                                        |
| --------- | --------------------------------------------------------------------------------------- | --------------------------------------------- |
| FR-EX-001 | The system shall support recording expenses associated with relevant field activities.  | Confirmed                                     |
| FR-EX-002 | The system shall support defining fixed and variable expense categories where required. | Confirmed                                     |
| FR-EX-003 | The system shall support submitting expense information for review.                     | Confirmed                                     |
| FR-EX-004 | The system shall support expense review and approval by authorized users.               | Confirmed – approver role needs clarification |
| FR-EX-005 | The system shall maintain expense information and relevant status details.              | Pending Definition                            |
| FR-EX-006 | The system shall support viewing relevant expense history.                              | Pending Definition                            |

**Clarifications:**

- Confirm expense categories and required fields.
- Confirm expense approval responsibilities among the three roles.
- Confirm whether attachments or image evidence are mandatory.
- Confirm whether approvers can modify submitted expense amounts.
- Confirm rejection, resubmission, and cancellation rules.

Financial processing, accounting, payroll, and automated reimbursements remain outside the project scope unless formally changed.

### 3.10 Reporting & Analytics (RP)

The system shall provide reporting and dashboard capabilities for operational and management use.

| ID        | Requirement                                                                                             | Status    |
| --------- | ------------------------------------------------------------------------------------------------------- | --------- |
| FR-RP-001 | The system shall provide reports relating to farm monitoring activities.                                | Confirmed |
| FR-RP-002 | The system shall provide relevant farm and visit information for management oversight.                  | Confirmed |
| FR-RP-003 | The system shall support reporting on batch/flock monitoring information.                               | Confirmed |
| FR-RP-004 | The system shall provide dashboards for relevant users.                                                 | Confirmed |
| FR-RP-005 | The system shall support reporting based on authorized geographical and organizational access.          | Confirmed |
| FR-RP-006 | The system shall support the reporting parameters and calculation criteria defined by authorized users. | Confirmed |
| FR-RP-007 | The system shall provide access to historical monitoring information where required.                    | Confirmed |

**Clarifications:**

- Confirm the complete report inventory.
- Confirm report filters, grouping, and date ranges.
- Confirm export formats.
- Confirm dashboard KPIs and calculation definitions.
- Confirm map-based reporting and visualizations.

### 3.11 Audit & Logging (AU)

The system shall maintain records of important user and system activities.

| ID        | Requirement                                                                   | Status    |
| --------- | ----------------------------------------------------------------------------- | --------- |
| FR-AU-001 | The system shall maintain audit records of relevant system activities.        | Confirmed |
| FR-AU-002 | The system shall record relevant user login activities.                       | Confirmed |
| FR-AU-003 | The system shall maintain traceability of important data changes.             | Confirmed |
| FR-AU-004 | The system shall record the user responsible for relevant audited activities. | Confirmed |
| FR-AU-005 | The system shall allow authorized users to review audit information.          | Confirmed |

**Clarifications:**

- Confirm the complete list of audited modules and actions.
- Confirm whether previous and updated values must be stored.
- Confirm audit retention requirements.
- Confirm which of the three roles can view audit records.

### 3.12 Notifications & Approvals (NT)

The system shall support notifications and approval-related communication.

| ID        | Requirement                                                                 | Status             |
| --------- | --------------------------------------------------------------------------- | ------------------ |
| FR-NT-001 | The system shall support notifications for relevant operational activities. | Confirmed          |
| FR-NT-002 | The system shall support notifications related to approval processes.       | Confirmed          |
| FR-NT-003 | The system shall support relevant approval workflows.                       | Confirmed          |
| FR-NT-004 | The system shall notify appropriate users about relevant workflow events.   | Pending Definition |
| FR-NT-005 | The system shall maintain relevant notification information.                | Pending Definition |

**Clarifications:**

- Confirm notification channels.
- Confirm notification triggers and recipients.
- Confirm whether notifications are required in the Web Application, Mobile Application, email, or multiple channels.
- Confirm reminder and escalation requirements.

### 3.13 Offline Capability (OF)

Offline capability is planned primarily for the subsequent Mobile Application delivery.

| ID        | Requirement                                                                                                 | Status                             |
| --------- | ----------------------------------------------------------------------------------------------------------- | ---------------------------------- |
| FR-OF-001 | The Mobile Application shall support the approved offline data capture requirements.                        | Future Phase                       |
| FR-OF-002 | The Mobile Application shall support storing relevant field data while network connectivity is unavailable. | Future Phase                       |
| FR-OF-003 | The Mobile Application shall support synchronizing stored data when connectivity becomes available.         | Future Phase                       |
| FR-OF-004 | The system shall maintain the required synchronization status.                                              | Future Phase                       |
| FR-OF-005 | The system shall support the approved synchronization conflict-handling rules.                              | Needs Clarification – Future Phase |

The offline data model, conflict resolution, retry behaviour, synchronization ownership, and failure handling must be defined before Mobile Application implementation.

---

## 4. Non-Functional Requirements

The following requirements are derived from the original SRS and Project Proposal. Original numeric values are preserved pending validation.

### 4.1 Performance

| ID      | Requirement                                                                                    | Status                                   |
| ------- | ---------------------------------------------------------------------------------------------- | ---------------------------------------- |
| NFR-001 | The system should respond to user input within 2 seconds.                                      | Confirmed in original SRS                |
| NFR-002 | The average response time should remain below 1.5 seconds under the specified test conditions. | Confirmed in original SRS                |
| NFR-003 | The system should support at least 1,000 concurrent users.                                     | Confirmed – sizing validation required   |
| NFR-004 | The system should support at least 100 transactions per minute.                                | Confirmed – workload validation required |

### 4.2 Availability and Recovery

| ID      | Requirement                                                              | Status                                                 |
| ------- | ------------------------------------------------------------------------ | ------------------------------------------------------ |
| NFR-005 | The system should support recovery within 5 minutes following a failure. | Confirmed – recovery definition requires clarification |
| NFR-006 | The system should support daily automated database backups.              | Confirmed                                              |
| NFR-007 | Backup and recovery tests should be conducted quarterly.                 | Confirmed                                              |

### 4.3 Security

| ID      | Requirement                                                                               | Status             |
| ------- | ----------------------------------------------------------------------------------------- | ------------------ |
| NFR-008 | The system shall enforce role-based access control.                                       | Confirmed          |
| NFR-009 | The system shall protect data in transit using encryption.                                | Confirmed          |
| NFR-010 | The system shall protect stored data using appropriate encryption mechanisms.             | Confirmed          |
| NFR-011 | The system shall maintain audit information for login attempts and sensitive-data access. | Confirmed          |
| NFR-012 | Authentication and credential protection shall follow the approved security design.       | Pending Definition |

The exact cryptographic algorithms, password storage/retrieval design, session management, and key management requirements must be finalized in the Security Design document.

### 4.4 Maintainability and Quality

| ID      | Requirement                                                                           | Status                                               |
| ------- | ------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| NFR-013 | The application shall follow applicable company coding standards.                     | Confirmed                                            |
| NFR-014 | The project shall maintain version control for source code and relevant deliverables. | Confirmed                                            |
| NFR-015 | The project shall target at least 80% automated unit-test coverage.                   | Confirmed – measurement scope requires clarification |

### 4.5 Compatibility

| ID      | Requirement                                                                            | Status                   |
| ------- | -------------------------------------------------------------------------------------- | ------------------------ |
| NFR-016 | The Web Application shall support the approved desktop operating systems and browsers. | Needs Clarification      |
| NFR-017 | The Mobile Application shall support the approved Android platform.                    | Confirmed – future phase |
| NFR-018 | The Mobile Application shall support the approved offline operating requirements.      | Confirmed – future phase |

The original SRS references Windows 8+, macOS 11, Ubuntu 20.04, Chrome, Firefox, and Edge. The exact supported versions and browser combinations must be reviewed and confirmed.

### 4.6 Accessibility and Usability

| ID      | Requirement                                                                                | Status                               |
| ------- | ------------------------------------------------------------------------------------------ | ------------------------------------ |
| NFR-019 | The system shall consider WCAG 2.0 accessibility requirements.                             | Confirmed in original SRS            |
| NFR-020 | The system should provide a consistent and usable interface for the supported user groups. | Pending detailed acceptance criteria |

### 4.7 Scalability

| ID      | Requirement                                                                      | Status                                         |
| ------- | -------------------------------------------------------------------------------- | ---------------------------------------------- |
| NFR-021 | The system should support horizontal scalability.                                | Confirmed                                      |
| NFR-022 | Server CPU utilization should not exceed 70% during the specified peak workload. | Confirmed – test conditions require definition |

### 4.8 Deployment and Infrastructure

| ID      | Requirement                                                                                       | Status                        |
| ------- | ------------------------------------------------------------------------------------------------- | ----------------------------- |
| NFR-023 | The solution shall support separate Development, Testing, and Production environments.            | Confirmed                     |
| NFR-024 | The proposed deployment approach shall support on-premises deployment.                            | Confirmed in Project Proposal |
| NFR-025 | Relevant documents and uploaded files shall be stored using the approved secure storage approach. | Confirmed at high level       |

---

## 5. External Interface Requirements

### 5.1 User Interface

The system shall provide a Web Application interface for the first delivery.

The Mobile Application interface will be developed in a subsequent phase.

The UI/UX Specification will define:

- Screen inventory.
- Navigation structure.
- Forms and field definitions.
- Input validation.
- Data tables and filters.
- Dashboard components.
- User interaction behaviour.
- Responsive layout requirements.

The supplied Figma prototypes are reference material only and do not independently establish approved functional requirements.

### 5.2 Software Interfaces

The system shall use a backend API to expose application functionality to the Web Application.

The API shall also support the subsequent Mobile Application.

Detailed endpoint definitions, request/response formats, authentication mechanisms, and error handling will be specified in the API Specification.

### 5.3 Database Interface

The system shall use Microsoft SQL Server as its centralized relational database.

Application CRUD and business operations shall use Dapper and Stored Procedures. EF Core Code First shall be used for schema management and migrations according to the confirmed architecture decisions.

Detailed database entities, procedures, and data constraints will be defined in the Database Design Specification.

### 5.4 Hardware and Device Interfaces

The Mobile Application is expected to use relevant mobile device capabilities such as GPS and image capture where approved.

Purchase or maintenance of mobile devices, GPS hardware, or network infrastructure is outside the project scope.

---

## 6. Data Requirements

The system is expected to manage the following broad categories of information:

| Data Category      | Description                                                         |
| ------------------ | ------------------------------------------------------------------- |
| User Data          | User accounts, roles, permissions, and session-related information. |
| Geography Data     | Relevant geographical areas and organizational assignments.         |
| Farm Data          | Breeder farm details, ownership, contacts, and location.            |
| Breed Data         | Breeder and breed mapping information.                              |
| Itinerary Data     | Planned farm visits and related status information.                 |
| Visit Data         | Farm visit details and monitoring observations.                     |
| Questionnaire Data | Questionnaire definitions, questions, and captured responses.       |
| Batch/Flock Data   | Batch information, monitoring data, and lifecycle history.          |
| Feed Data          | Feed usage and related monitoring information.                      |
| Expense Data       | Expense details and approval-related information.                   |
| Reporting Data     | Information used for reports, dashboards, and calculations.         |
| Audit Data         | User actions and relevant historical changes.                       |
| Notification Data  | Notification and approval-related records.                          |

The detailed fields, data types, mandatory rules, relationships, and retention policies will be defined in the Database Design Specification.

---

## 7. Business Rules

The following business rules are identified at a high level. Detailed rules require business confirmation.

| ID     | Business Rule                                                                                            | Status                            |
| ------ | -------------------------------------------------------------------------------------------------------- | --------------------------------- |
| BR-001 | The system shall be used for breeder farm monitoring involving livestock/poultry.                        | Confirmed                         |
| BR-002 | General crop-based agricultural management is outside the project scope.                                 | Confirmed                         |
| BR-003 | Farm FMS shall support Administrator, Sales Manager, and Area Sales Manager as its primary system roles. | Confirmed                         |
| BR-004 | A separate Supervisor system role shall not be introduced.                                               | Confirmed project decision        |
| BR-005 | Users shall only access information and functions permitted by their roles and assignments.              | Confirmed                         |
| BR-006 | Farm monitoring information shall be associated with relevant farm records.                              | Confirmed                         |
| BR-007 | Batch/flock monitoring information shall be associated with relevant farms.                              | Confirmed                         |
| BR-008 | Farm visit information shall be maintained for historical reference.                                     | Confirmed                         |
| BR-009 | FCR, mortality, and other automated calculations shall follow approved business formulas.                | Pending Definition                |
| BR-010 | Itinerary and expense approvals shall follow the approved responsibilities of the three system roles.    | Needs Clarification               |
| BR-011 | Questionnaire responses shall be associated with the relevant farm visit.                                | Confirmed                         |
| BR-012 | Offline synchronization shall follow the approved synchronization and conflict-resolution rules.         | Future Phase – Pending Definition |

---

## 8. Acceptance Criteria

The system will be evaluated against approved requirements and agreed test cases.

High-level acceptance criteria include:

1. Authorized users can authenticate and access permitted functions.
2. The system supports only the three confirmed primary roles.
3. No separate Supervisor role or Supervisor login is introduced.
4. User permissions and data access restrictions are enforced.
5. Authorized users can maintain breeder farm information.
6. Relevant geographical and organizational information can be maintained.
7. Farm visit itineraries can be managed according to approved workflows.
8. Farm visit information can be captured and viewed.
9. Questionnaire definitions and responses can be managed according to approved rules.
10. Batch/flock information can be maintained and monitored.
11. Approved business calculations produce expected results.
12. Expenses can be recorded and processed according to approved rules.
13. Relevant reports and dashboards are accessible to authorized users.
14. Audit information is maintained for required activities.
15. The system meets approved performance, security, availability, and usability requirements.
16. The Web Application passes agreed functional testing and UAT criteria.

Detailed test scenarios and expected results will be maintained in the Test Strategy and Test Cases documents.

---

## 9. Requirements Traceability

Every approved requirement should be traceable to its design, implementation, and test case.

The following identifiers will be used:

| Prefix | Description                                |
| ------ | ------------------------------------------ |
| FR-UM  | User Management functional requirement     |
| FR-AH  | Area & Hierarchy functional requirement    |
| FR-FM  | Farm Management functional requirement     |
| FR-MD  | Master Data functional requirement         |
| FR-IP  | Itinerary Planning functional requirement  |
| FR-VQ  | Visit Questionnaire functional requirement |
| FR-VC  | Visit Data Capture functional requirement  |
| FR-BM  | Batch Monitoring functional requirement    |
| FR-EX  | Expense Management functional requirement  |
| FR-RP  | Reporting functional requirement           |
| FR-AU  | Audit functional requirement               |
| FR-NT  | Notifications functional requirement       |
| FR-OF  | Offline Capability functional requirement  |
| NFR    | Non-functional requirement                 |
| BR     | Business rule                              |
| CQ     | Business clarification                     |

A separate Requirements Traceability Matrix will map each requirement to its source, approval status, design reference, implementation module, and test case.

---

## 10. Open Clarifications and Requirement Risks

The following matters remain unresolved and must be tracked in the Business Clarification Register.

| ID     | Priority | Clarification Area                                                              |
| ------ | -------- | ------------------------------------------------------------------------------- |
| CQ-002 | P2       | Whether an additional Analyst role is required.                                 |
| CQ-003 | P1       | Final permission boundaries and responsibilities of Administrator, SM, and ASM. |
| CQ-004 | P1       | Verification of previously claimed approved decisions.                          |
| CQ-005 | P1       | Formal approval status of original project documents.                           |
| CQ-006 | P1       | Geographical hierarchy structure.                                               |
| CQ-007 | P1       | User and geographical assignment rules.                                         |
| CQ-008 | P1       | Itinerary planning period and structure.                                        |
| CQ-009 | P1       | Itinerary creation and assignment responsibilities.                             |
| CQ-011 | P1       | Itinerary approval and rejection behaviour.                                     |
| CQ-012 | P1       | Itinerary modification and resubmission rules.                                  |
| CQ-013 | P1       | Farm visit approval and rejection workflow.                                     |
| CQ-014 | P1       | Farm Monitoring versus Farm Evaluation workflow differences.                    |
| CQ-015 | P1       | Fixed visit fields versus configurable questionnaire fields.                    |
| CQ-016 | P1       | Questionnaire versioning and historical response rules.                         |
| CQ-019 | P2       | Availability of existing questionnaire forms.                                   |
| CQ-021 | P1       | Batch lifecycle and closure rules.                                              |
| CQ-022 | P1       | Batch data capture and monitoring structure.                                    |
| CQ-023 | P1       | Batch history and multiple active batch rules.                                  |
| CQ-024 | P1       | Offline synchronization and conflict resolution.                                |
| CQ-025 | P2       | Synchronization retry and failure handling.                                     |
| CQ-026 | P2       | GPS/location capture requirements.                                              |
| CQ-027 | P2       | Farm and visit location validation rules.                                       |
| CQ-036 | P2       | Credential and password protection requirements.                                |
| CQ-042 | P3       | Performance, compatibility, and test-sizing assumptions.                        |

The complete clarification list remains in the Business Clarification Register. The table above is a summary, not a replacement for that register.

The separate Supervisor role question is considered resolved by the project requester's clarification. The remaining approval routing and responsibility questions must be answered using the three confirmed roles.

---

## 11. Source Documents and References

| Reference | Source                                                                  | Usage                                                          |
| --------- | ----------------------------------------------------------------------- | -------------------------------------------------------------- |
| S1        | PMS – CGE FMS – Software Requirement Specification, v1.0, 15 July 2026  | Primary source for functional and non-functional requirements. |
| S2        | PMS – CGE Farm Monitoring System – Project Proposal, v1.1, 13 July 2026 | Project scope, capabilities, and delivery context.             |
| S3        | Farm_FMS_Module_01_Database_Specification_v0.2.xlsx                     | Approved Module 01 Security & Access database baseline only.   |
| S4        | DataBase Design.xlsx                                                    | Legacy system reference only.                                  |
| S5        | Figma Web Prototype, 02 November 2026                                   | UI reference only; not an approved requirements source.        |
| S6        | Figma Mobile Prototype, 02 November 2026                                | UI reference only; not an approved requirements source.        |
| S7        | Functional_Requirements_Analysis.md, v0.1                               | Supporting functional analysis; not approved.                  |
| S8        | Business_Workflow_Analysis.md, v0.1                                     | Supporting workflow analysis; not approved.                    |
| S9        | UI_Screen_Requirement_Mapping.md, v0.1                                  | Supporting screen analysis; not approved.                      |
| S10       | Business_Clarification_Register.md, v0.1                                | Outstanding business clarifications.                           |
| S11       | FarmFMS_Architecture_Decisions_v2.md                                    | Confirmed technical architecture decisions.                    |

### Source Limitations

- The supplied original SRS has blank signature and approval date fields; formal approval cannot be verified.
- The original SRS context diagram and UI screen section contain image-only content that could not be reviewed in the available extracted source.
- The supplied Figma prototypes are illustrative references and do not independently establish approved functional requirements.
- Existing questionnaire forms were not supplied.
- First-pass analysis documents are supporting material and are not automatically approved business requirements.

---

## 12. Document Review and Approval

**Current Status:** Draft – Pending Formal Stakeholder Approval

The role structure in this revision has been confirmed by the project requester as:

- Administrator.
- Sales Manager (SM).
- Area Sales Manager (ASM).

A separate Supervisor role is explicitly excluded from the Farm FMS role model.

The original non-functional requirement numeric values have been preserved pending validation. Mobile requirements remain documented as a subsequent delivery phase. Unresolved requirements remain explicitly identified for business clarification.

| Review / Approval   | Name              | Designation                   | Date             | Status    |
| ------------------- | ----------------- | ----------------------------- | ---------------- | --------- |
| Prepared By         | TBD               | Project Team                  | TBD              | Draft     |
| Requirements Review | Project Requester | Project Stakeholder           | 04 November 2026 | Confirmed |
| Business Review     | TBD               | CGE Business Representative   | TBD              | Pending   |
| Technical Review    | TBD               | PMS Technical Representative  | TBD              | Pending   |
| Final Approval      | TBD               | Authorized CGE Representative | TBD              | Pending   |
