# AI Prompt Library

Reusable prompts, templates and agent briefs for ThermoCore.

## How to use

1. Read `AI_CONTEXT.md`, `PROMPT_GUIDE.md` and `IMPLEMENTATION_PLAYBOOK.md`.
2. Open the matching prompt under `prompts/`.
3. Fill placeholders in curly braces.
4. Attach the listed must-read documents.
5. Prefer templates under `templates/` for new files.
6. Use an agent brief under `agents/` when delegating a role.

## Prompts (`ai/prompts/`)

| Prompt | Use when |
|---|---|
| `ImplementComponent.md` | Adding or extending a Core `ISimulationComponent` |
| `WriteTests.md` | Adding unit or integration tests |
| `ReviewCode.md` | Reviewing a diff against architecture and physics rules |
| `ValidatePhysics.md` | Checking balances, units and conservation |
| `PhysicsReview.md` | Deeper physical-model review |
| `NumericalReview.md` | Solvers, tolerances, convergence |
| `ArchitectReview.md` | Layering, dependency and namespace review |
| `PullRequestReview.md` | Pre-merge checklist review |
| `ExpandDocument.md` | Expanding an engineering document to ReadyForImplementation |
| `ImplementAwgFeature.md` | AWG configuration, topology or control work |
| `AddApiEndpoint.md` | ASP.NET Core endpoint work (after API specs) |

## Templates (`ai/templates/`)

| Template | Purpose |
|---|---|
| `csharp/SimulationComponentTemplate.cs` | New Core simulation component skeleton |
| `csharp/RecordTemplate.cs` | Immutable record skeleton |
| `csharp/ComponentTemplate.cs` | Deprecated alias — prefer SimulationComponentTemplate |
| `tests/UnitTestTemplate.cs` | xUnit unit test class |
| `tests/IntegrationTestTemplate.cs` | Graph/engine integration test |
| `docs/EngineeringDocumentTemplate.md` | Engineering specification |
| `docs/ADR_Template.md` | Architecture decision record |
| `architecture/ComponentCardTemplate.md` | Component capability card |
| `github/IssueTemplate.md` | Issue body |
| `github/PullRequestChecklist.md` | PR checklist |
| `AgentTaskTemplate.md` | Scoped agent task brief |
| `AgentReviewTemplate.md` | Agent review report |
| `WorkflowRunTemplate.md` | Workflow execution notes |
| `json/JsonSchemaTemplate.json` | JSON schema starter |

## Agents (`ai/agents/`)

| Agent | Mission |
|---|---|
| `Architect.md` | Architecture and dependency boundaries |
| `Physicist.md` | Physical models and conservation |
| `NumericalEngineer.md` | Solvers and numerical stability |
| `BackendDeveloper.md` | Core / AWG / API implementation |
| `TestEngineer.md` | Unit, integration and regression tests |
| `DocumentationEngineer.md` | Specs and tracker updates |
| `FrontendDeveloper.md` | Blazor / UI (after WEB specs) |
| `Reviewer.md` | Final acceptance review |

## Rules

- Do not invent physics; cite the governing document.
- Keep SI units inside Core and AWG.
- Keep UI, ASP.NET and persistence out of `ThermoCore.Core`.
- Update `docs/00_Project/IMPLEMENTATION_PROGRESS.md` status suggestions after finishing a task.
