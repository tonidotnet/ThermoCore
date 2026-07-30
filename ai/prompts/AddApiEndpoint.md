# Add API Endpoint

## Goal
Add an ASP.NET Core endpoint in `ThermoCore.Api` after the API specification exists.

## Must read
1. `ai/AI_CONTEXT.md`
2. `docs/05_Product/19_WebApi.md` (must be ReadyForImplementation)
3. `docs/07_ProjectManagement/18_CodingRules.md`
4. Existing API project patterns

## Task
Endpoint: `{HTTP_METHOD} {ROUTE}`
Task ID: `{TASK_ID}`

## Requirements
- Keep physics in Core; API only maps DTOs and orchestrates jobs.
- Validate inputs at the boundary; convert display units explicitly.
- Support cancellation for long-running simulation jobs.
- Add OpenAPI metadata and API tests.
- Do not reference Blazor UI types from the API project.

## Deliverables
1. Endpoint + contracts
2. Tests
3. Suggested tracker status update
