---
name: farmfms-backend-slice
description: Layer-by-layer pattern for a Farm FMS backend feature slice (repository → service → controller) following AD-02, AD-05, AD-06, AD-10, AD-13–AD-18 and Reference Mapping B-06/B-10–B-13. Use when the backend-developer implements an approved specification slice.
---

# Farm FMS – Backend feature slice

Precondition: an **approved** spec (G0) and the database-engineer hand-off (tables / procedures / constants exist). Authentication (Step 04) precedes any admin endpoint — never add an anonymous admin API.

## Where things go (AD-03)
| Item | Location |
|---|---|
| Entity (inherits `Domain.Common.AuditableEntity` when it has the audit columns) | `FarmFMS.Domain/<Module>/<Feature>/` |
| `I<Feature>Repository`, `I<Feature>Service`, `<Feature>Service`, `<Feature>Dto` (inherits `AuditableDto`), `<Feature>CreateRequest` / `UpdateRequest`, read models | `FarmFMS.Application/<Module>/<Feature>/` |
| `<Feature>Repository`, `<Module>Procedures` (constants — owned by database-engineer) | `FarmFMS.Infrastructure/Repositories/<Module>/` |
| Controller | `FarmFMS.API/Controllers/<Module>/<Resource>Controller.cs` |
| Registrations | `AddApplication()` / `AddInfrastructure()` (hotspots — only when the orchestrator assigns them) |

## Rules per layer
- **Repository:** inject `IStoredProcedureExecutor` only; one method per procedure; `DynamicParameters` or anonymous objects (no `@` in names); `CancellationToken` through; inserts via `ExecuteForNewIdAsync`, updates/deletes via `ExecuteForRowsAffectedAsync`; return entities or read models, never DTOs (AD-10). No `DbContext`, no inline SQL, no Dapper outside Infrastructure.
- **Service:** returns `Result` / `Result<T>`; normalises input with `StringExtensions`, required values with `NullableExtensions.Required()`; audit IDs from `IAuditActorService` (stop on failure); time from `TimeProvider`; `ILogger<T>` without secrets or parameter values. Translate "0 rows affected" to `NotFound` where the spec says so.
- **Requests / DTOs:** Data Annotations with explicit messages; nullable + `[Required]` for value types; lengths from one constants class shared with EF configurations; **no audit fields in requests**.
- **Controller:** inherit `ApiControllerBase`; kebab-case `api/<module>/<resource>`; `{id:int}`; `OkOrProblem` / `CreatedOrProblem` (201 + `{ id }`) / `NoContentOrProblem`; `CancellationToken` on every action; `[Authorize(Policy = …)]` per the spec (AD-13); **no try/catch**.
- **Tests:** unit tests for service rules and mapping (fakes for repositories); API host tests for routing / status codes; architecture tests must stay green.

Finish with `farmfms-quality-gates` (G1–G3) and a `farmfms-handoff` note.
