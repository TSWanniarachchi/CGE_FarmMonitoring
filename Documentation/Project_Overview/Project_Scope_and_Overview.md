# CGE Farm Monitoring System

# Project Scope & Overview

| Document Information | Details                                        |
| -------------------- | ---------------------------------------------- |
| Document Title       | Project Scope & Overview                       |
| Project Name         | CGE Farm Monitoring System                     |
| Abbreviation         | Farm FMS                                       |
| Document Version     | 0.2                                            |
| Prepared Date        | 04 November 2026                               |
| Document Status      | Approved                                       |
| Business Owner       | Ceylon Grain Elevators PLC (CGE)               |
| Solution Provider    | Prima Management Services (Pvt.) Limited (PMS) |

---

## 1. Executive Summary

The CGE Farm Monitoring System (Farm FMS) is a digital solution intended to modernize and centralize breeder farm monitoring operations conducted by Ceylon Grain Elevators PLC (CGE).

The existing Farm Monitoring System was developed in 2008 and has become outdated in terms of technology, usability, scalability, and data visibility. The proposed system aims to replace the legacy solution with a modern platform that supports farm information management, farm visit planning, field-level data collection, batch/flock lifecycle monitoring, expense management, reporting, and management oversight.

The overall solution consists of a Web Application and a Mobile Application. The agreed delivery sequence is to develop and deliver the Web Application first, followed by the Mobile Application in a subsequent phase.

The solution will use a centralized backend API and database so that both applications can share business functionality and data.

The system is specifically designed for breeder farm monitoring involving livestock/poultry operations. General agricultural and crop-based farm management is outside the project scope.

## 2. Project Background and Business Context

CGE has been using a legacy Farm Monitoring System to support its breeder farm monitoring and related operational activities.

The existing solution has limitations in technology, usability, scalability, and data visibility. The business requires a modernized system to improve the way farm information, farm visits, production data, and monitoring activities are recorded and accessed.

The proposed Farm FMS is intended to provide a centralized digital platform for managing breeder farm information and monitoring activities.

The confirmed business scope is breeder farm monitoring (livestock/poultry). The system is not intended to function as a general agricultural or crop-management system.

## 3. Problem Statement

The existing system and associated manual processes create challenges in maintaining consistent farm information, recording field-level monitoring data, tracking batch performance, and providing timely information for management decisions.

The key business problems identified in the project sources include:

- Limitations of the existing legacy application.
- Dependence on manual or paper-based farm visit data collection.
- Difficulties in centralizing farm and breeder information.
- Limited visibility into farm monitoring activities and historical records.
- Need for structured batch/flock lifecycle tracking.
- Need for standardized data collection and reporting.
- Need for improved approval workflows and auditability.

The project aims to address these challenges through a centralized and modern Farm Monitoring System.

## 4. Project Objectives

The primary objectives are:

1. Replace the existing legacy Farm Monitoring System with a modern solution.
2. Centralize breeder farm and farmer information.
3. Digitize farm visit planning and field-level data collection.
4. Support batch/flock lifecycle monitoring.
5. Improve the accuracy and consistency of farm monitoring data.
6. Support feed usage and farm performance tracking.
7. Automate relevant calculations, including Feed Conversion Ratio (FCR), mortality, and age determination where defined by approved business rules.
8. Provide management with centralized reports and dashboards.
9. Improve approval processes and operational visibility.
10. Maintain audit history for important system activities and data changes.

## 5. Project Scope

### 5.1 Overall Scope

The overall Farm FMS project covers the design, development, testing, and deployment of a Web Application and a Mobile Application for breeder farm monitoring operations.

The solution is intended to support:

- Breeder farm and farmer information management.
- Farm and breed mapping.
- Area and hierarchy management.
- Farm visit planning and approval.
- Farm visit data collection.
- Visit questionnaire management.
- Batch/flock lifecycle monitoring.
- Feed usage and performance tracking.
- Expense management.
- Reporting and analytics.
- User and access management.
- Notifications, approvals, and audit logging.
- Offline data capture and synchronization for the Mobile Application, subject to detailed requirements.

### 5.2 Web Application – First Delivery

The Web Application is the first delivery target of the project.

It is intended to provide centralized management and administrative capabilities, including:

- User and access management.
- Area and hierarchy management.
- Farm master data management.
- Breeder farm and breed mapping.
- Itinerary and visit oversight.
- Questionnaire configuration.
- Batch and farm monitoring information management.
- Expense oversight and approval capabilities as defined by approved role permissions.
- Reporting and dashboards.
- Audit and system administration capabilities.

The detailed functionality and permissions of each user role will be defined in the approved Software Requirements Specification and related functional documents.

### 5.3 Mobile Application – Subsequent Delivery

The Mobile Application is planned for a subsequent delivery phase, after the initial Web Application delivery.

The overall project vision includes mobile capabilities such as:

- Secure user authentication.
- Farm visit planning and visit execution.
- GPS-based location capture.
- Image capture where required.
- Dynamic visit questionnaires.
- Farm and batch/flock data capture.
- Expense data entry.
- Offline data capture and synchronization.
- Access to relevant farm and visit information.

The detailed mobile functionality, offline behaviour, synchronization rules, and user permissions must be finalized in the relevant requirements documents before mobile development begins.

The backend API must be designed to support both the initial Web Application and the subsequent Mobile Application.

### 5.4 Out of Scope

The original Project Proposal identifies the following items as out of scope unless formally approved through a change request:

- Purchase or maintenance of mobile devices, GPS hardware, or network infrastructure.
- Internet connectivity improvements in rural or farm locations.
- Integration with external ERP, accounting, CRM, or inventory platforms.
- Advanced analytics, predictive models, or AI-based recommendations.
- Financial processing, accounting, payroll, or automated reimbursements.
- External farmer portals or public-facing dashboards.
- General crop-based agricultural management.

Any additional functionality outside the approved scope must follow the agreed change-control process.

## 6. High-Level Functional Areas

The following functional areas have been identified from the project requirements and supporting analysis.

| No. | Functional Area               | High-Level Description                                                             |
| --- | ----------------------------- | ---------------------------------------------------------------------------------- |
| 1   | User Management               | Manage system users, roles, access, and account-related activities.                |
| 2   | Area & Hierarchy Management   | Maintain geographical areas, hierarchy information, and relevant user assignments. |
| 3   | Farm Management               | Maintain breeder farm information and related farm details.                        |
| 4   | Itinerary Planning & Approval | Support planning, reviewing, and approving farm visits.                            |
| 5   | Visit Questionnaire           | Configure and manage structured questionnaires for farm visits.                    |
| 6   | Visit Data Capture            | Record farm visit information and field-level observations.                        |
| 7   | Batch/Flock Monitoring        | Maintain batch/flock information and monitor lifecycle and performance.            |
| 8   | Expense Management            | Record and manage expenses associated with field activities.                       |
| 9   | Reporting & Analytics         | Provide operational reports and management dashboards.                             |
| 10  | Audit & Logging               | Maintain traceability of important system activities and changes.                  |
| 11  | Notifications & Approvals     | Support relevant notifications and approval processes.                             |
| 12  | Offline Capability            | Support mobile data capture and synchronization when connectivity is available.    |

These are high-level functional areas, not finalized module specifications. Detailed functionality and boundaries will be established in the requirements documentation.

## 7. Stakeholders and User Roles

### 7.1 Business Stakeholders

The primary organizations involved are:

- **Ceylon Grain Elevators PLC (CGE):** Business owner and client organization.
- **Prima Management Services (Pvt.) Limited (PMS):** Solution provider responsible for system development and implementation.

The original SRS identifies management, divisional representatives, administrative representatives, and IT representatives from CGE, as well as management, project leadership, software engineering, QA, and business analysis stakeholders from PMS.

### 7.2 System User Roles

The original SRS identifies the following main user roles:

| User Role                | High-Level Responsibility                                                           |
| ------------------------ | ----------------------------------------------------------------------------------- |
| Administrator            | System administration and user/access management.                                   |
| Sales Manager (SM)       | Management oversight, reviewing operational activities, approvals, and reporting.   |
| Area Sales Manager (ASM) | Field-level farm monitoring activities, visit-related operations, and data capture. |

The exact permissions, responsibilities, and application access for each role will be finalized in the Role & Permission Matrix and Software Requirements Specification.

**Pending clarification:** The original SRS and Project Proposal use different terminology for certain field and approval roles, including Supervisor and Area Sales Manager. This must be reconciled before the detailed role model is approved.

## 8. High-Level Business Capabilities

The system is expected to provide the following business capabilities:

- Maintain centralized breeder farm and related master information.
- Organize farm information using geographical and organizational hierarchies.
- Plan and manage farm visits.
- Capture structured farm monitoring information.
- Maintain historical farm visit records.
- Monitor batch/flock information and relevant performance indicators.
- Track feed usage and related farm performance information.
- Manage field-related expenses and approvals.
- Provide management reporting and dashboards.
- Maintain audit records and operational traceability.
- Support mobile field operations in the subsequent delivery phase.

Detailed business rules, calculations, and workflow transitions will be documented separately.

## 9. Expected Business Benefits

The project proposal identifies the following expected outcomes:

1. Increased digital capture of farm visit information and reduced dependence on paper-based forms.
2. Reduced data processing and reporting time.
3. Improved data accuracy and consistency.
4. Better visibility of farm visit information for supervisors and management.
5. Standardized data collection across farm types and regions.
6. Improved access to automated reports and dashboards.
7. Improved user adoption and satisfaction.
8. Better availability of information for operational decision-making.

These are expected benefits. Their achievement must be evaluated against agreed project success criteria.

## 10. High-Level Solution Overview

The solution will be developed as a centralized system consisting of:

- A Web Application for management and administrative activities.
- A backend API responsible for business operations and application integration.
- A centralized Microsoft SQL Server database.
- A subsequent Mobile Application that will consume the same backend API.

### 10.1 Confirmed Technical Direction

| Component               | Selected Technology / Approach    |
| ----------------------- | --------------------------------- |
| Backend                 | .NET 10 ASP.NET Core Web API      |
| Backend Architecture    | Clean Architecture                |
| Web Frontend            | Blazor Web App (.NET 10)          |
| UI Components           | Radzen and Bootstrap              |
| Database                | Microsoft SQL Server              |
| Application Data Access | Dapper with Stored Procedures     |
| Schema Management       | EF Core Code First and migrations |
| Mobile Integration      | Backend API                       |

The detailed architecture, security design, database design, and API specifications will be documented in their respective documents.

## 11. Delivery Approach and Project Phases

The project will follow a phased delivery approach.

| Phase                                    | Description                                                                           |
| ---------------------------------------- | ------------------------------------------------------------------------------------- |
| Phase 1 – Project Initiation & Planning  | Confirm project scope, objectives, stakeholders, and project governance.              |
| Phase 2 – Requirements & Solution Design | Finalize requirements, workflows, UI/UX, architecture, database, and security design. |
| Phase 3 – Web Application Development    | Develop and validate the Web Application and its backend capabilities.                |
| Phase 4 – Testing & Validation           | Perform functional testing, integration testing, and user acceptance testing.         |
| Phase 5 – Web Deployment & Training      | Deploy the approved Web Application and provide relevant user training.               |
| Subsequent Phase – Mobile Application    | Develop and deliver the Mobile Application using the established backend API.         |

The detailed development roadmap, milestones, responsibilities, and timelines will be maintained in the Development Roadmap document.

## 12. Assumptions and Dependencies

### 12.1 Assumptions

The original Project Proposal identifies the following assumptions:

- Area Sales Managers will be responsible for maintaining accurate and up-to-date information.
- System administrators and supervisors will define relevant parameters and conduct approvals.
- Internal stakeholders will be available for requirements validation and testing.
- Relevant historical data may be provided where required for analysis.

### 12.2 Dependencies

The project depends on:

- Availability of accurate business data and relevant data-feed processes.
- Confirmation of business formulas and calculation rules.
- Timely stakeholder feedback and approvals.
- Availability of historical data where required.
- Confirmation of business workflows and user responsibilities.
- Availability of required development, testing, and deployment environments.

## 13. Constraints and Limitations

The project must consider the following constraints:

- The system is specifically intended for breeder farm monitoring (livestock/poultry), not general crop-based agriculture.
- The Web Application is the first delivery target.
- The Mobile Application is planned for a subsequent phase.
- Mobile field operations may depend on network availability and the approved offline synchronization design.
- External system integrations are excluded unless separately approved.
- Detailed business calculations must be based on confirmed business rules.
- Final role permissions must be approved by the relevant business stakeholders.

## 14. Risks and Open Decisions

The following matters require clarification or approval before the related detailed designs are finalized.

| ID    | Topic                     | Open Decision / Risk                                                                                    |
| ----- | ------------------------- | ------------------------------------------------------------------------------------------------------- |
| OD-01 | User Roles                | Reconcile Supervisor and Area Sales Manager terminology and responsibilities.                           |
| OD-02 | Approval Responsibilities | Confirm which roles are responsible for farm, itinerary, and expense approvals.                         |
| OD-03 | Geographical Hierarchy    | Confirm the Region, Territory, and user-assignment model.                                               |
| OD-04 | Farm Management           | Confirm the complete farm registration, classification, and approval rules.                             |
| OD-05 | Batch/Flock Management    | Confirm batch lifecycle, data capture, and closure rules.                                               |
| OD-06 | Calculations              | Confirm business formulas and calculation rules, including FCR and mortality.                           |
| OD-07 | Visit Questionnaires      | Confirm questionnaire configuration, versioning, and publishing rules.                                  |
| OD-08 | Offline Synchronization   | Confirm synchronization, conflict handling, and data ownership rules for the future Mobile Application. |
| OD-09 | Reporting                 | Confirm required reports, filters, calculations, and management access.                                 |
| OD-10 | Project Approval          | Verify the formal approval status of the supplied SRS and Project Proposal.                             |

These items must be tracked and resolved through the Business Clarification Register. This document does not independently approve or resolve them.

## 15. References

| Reference | Document                                                                | Purpose                                                       |
| --------- | ----------------------------------------------------------------------- | ------------------------------------------------------------- |
| REF-01    | PMS – CGE FMS – Software Requirement Specification, v1.0, 15 July 2026  | Primary business requirements source.                         |
| REF-02    | PMS – CGE Farm Monitoring System – Project Proposal, v1.1, 13 July 2026 | Project background, scope, objectives, and delivery approach. |
| REF-03    | Functional_Requirements_Analysis.md, v0.1                               | Supporting functional analysis; not approved.                 |
| REF-04    | Business_Workflow_Analysis.md, v0.1                                     | Supporting workflow analysis; not approved.                   |
| REF-05    | UI_Screen_Requirement_Mapping.md, v0.1                                  | Supporting UI analysis; not approved.                         |
| REF-06    | Business_Clarification_Register.md, v0.1                                | Outstanding business clarifications.                          |
| REF-07    | FarmFMS_Architecture_Decisions_v2.md                                    | Current technical architecture decisions.                     |

## 16. Document Review and Approval

**Current Status:** Draft – Pending Formal Stakeholder Approval

The project scope and delivery sequence in this revision have been reviewed and confirmed by the project requester.

This confirmation does not replace formal business approval by the relevant CGE stakeholders.

Approval of this document confirms the agreed project overview and scope baseline. It does not replace the approval required for detailed functional requirements, workflows, architecture, database design, or security specifications.

| Review / Approval    | Name              | Designation                     | Date             | Status    |
| -------------------- | ----------------- | ------------------------------- | ---------------- | --------- |
| Prepared By          | TBD               | Project Team                    | TBD              | Draft     |
| Project Scope Review | Project Requester | Project Stakeholder             | 04 November 2026 | Confirmed |
| Reviewed By          | TBD               | Business Analyst / Project Lead | TBD              | Pending   |
| Business Approval    | TBD               | CGE Business Representative     | TBD              | Pending   |
| Technical Approval   | TBD               | PMS Technical Representative    | TBD              | Pending   |
