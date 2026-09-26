# DigitalPulse AI

Feature modules call `IAiOrchestrator`. They never construct a vendor SDK.

```text
Feature → Orchestrator → Context builder + Graphify + retrieval
        → Versioned prompt → Guardrails → Model router → IAiProvider
        → Validation → Hold / assisted draft
Action + official adapter remain responsible for any external post.
```

## Providers

| `Ai:Provider` | When it is live |
|---|---|
| `Development` (default) | Never. Evidence-only hold. |
| `OpenAI` | `Ai:OpenAi:ApiKey` is set. |
| `AzureOpenAI` | Endpoint + `Ai:AzureOpenAi:ApiKey` are set. |

Secrets stay in user-secrets, environment variables, or Key Vault. Committed `appsettings.json` keys are empty. The browser never receives a provider key.

Timeouts and retries are configured under `Ai:TimeoutSeconds` and `Ai:Retry:MaxAttempts`. Failures hold; DigitalPulse does not invent a completion.

## Models

`IAiModelRouter` picks the model on the server (`Ai:DefaultModel`, `Ai:Models:Low`, `Ai:Models:Standard`, or `Ai:Models:{agent}`). The client cannot choose a model.

## Prompts and agents

Prompts are versioned as `{agent}.v1` in `AiPromptCatalog`. Required agents: research, identity, seo, content, social, reputation, portfolio, competitor, automation, whatsapp. None of them may execute an external action.

## Graphify and retrieval

`AiContextBuilder` loads tenant-scoped facts, knowledge, Graphify nodes, and `ISearchProvider` hits. Repo Graphify (`graphify-out/`) is not the product graph.

## Guardrails

- Authorization and entitlement before a run
- Prompt-injection rejection
- `AiPolicy`: no evidence → no claim; restricted facts never publish; conflicts need review
- Content safety on output

## Usage

Each run records `UsageKind.AiGeneration`. Token counts are audited when the provider returns them. Cost cents stay 0 until official rates are configured. Identical orchestrations are cached for 15 minutes.

## Evaluation

`AiEvaluationCases.Regression` is executed in unit tests. Safety cases cover injection and restricted copy.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| ProviderName = Development | No live key, or `Ai:Provider` is Development |
| Held / NeedsReview | Missing evidence, restricted fact, conflict, injection, or structured JSON missing |
| 404 on another business | Tenant and business filters |
| Google draft is Assisted | Expected. Publish still needs approval + official adapter |
