# DigitalPulse — AI Final Architecture & Implementation Prompt

## Purpose

Use this prompt as the **final AI architecture implementation specification** for DigitalPulse.

The existing DigitalPulse Master Specification already defines:

- AI Orchestrator
- provider-neutral AI abstraction
- specialized agents
- Graphify
- retrieval/search abstraction
- AI validation
- confidence handling
- cost/usage controls
- AI evaluation
- approval/autopilot
- tenant isolation
- CodeGraph development workflow

This document does **not** replace those requirements.

It closes the remaining implementation-level gaps around:

- AI provider abstraction
- provider/model configuration
- AI orchestration
- agent architecture
- prompt management
- structured outputs
- context assembly
- Graphify integration
- retrieval/RAG
- guardrails
- AI safety
- validation
- confidence
- model routing
- token/cost controls
- usage metering
- caching
- retries
- observability
- evaluation
- testing
- tenant configuration
- secret management
- AI-to-action boundaries
- production readiness

## Critical instruction

Before changing code:

1. Read the DigitalPulse Master Specification.
2. Read `.cursor/rules/ai-development.mdc`.
3. Read architecture, backend, database, security, integrations and testing rules.
4. Use CodeGraph to inspect the existing AI/search/configuration/provider code.
5. Use Graphify to inspect the existing business knowledge/context model.
6. Audit what is already implemented.
7. **Do not duplicate existing architecture.**
8. Implement only missing or incomplete capabilities.
9. Preserve existing contracts unless a documented architectural improvement is required.
10. Produce a requirement-to-code matrix before implementation.

The goal is **100% implementation**, not scaffolding.

---

# 1. FINAL AI ARCHITECTURE

DigitalPulse AI must follow this architecture:

```text
                         DIGITALPULSE
                              │
                              ▼
                    ┌──────────────────┐
                    │ AI Orchestrator  │
                    └────────┬─────────┘
                             │
             ┌───────────────┼────────────────┐
             │               │                │
             ▼               ▼                ▼
       Policy/Auth      Context Builder    Usage Control
             │               │                │
             │        ┌──────┴──────┐         │
             │        │             │         │
             │        ▼             ▼         │
             │     Graphify      Retrieval    │
             │     Context       / RAG        │
             │        │             │         │
             └────────┴──────┬──────┘         │
                             ▼                 │
                      Prompt Builder           │
                             │                 │
                             ▼                 │
                       Guardrails              │
                             │                 │
                             ▼                 │
                       Model Router            │
                             │                 │
                    ┌────────┴────────┐        │
                    ▼                 ▼        │
                 OpenAI          Azure OpenAI  │
                    │                 │        │
                    └────────┬────────┘        │
                             ▼                 │
                    Structured AI Result       │
                             │                 │
              ┌──────────────┼──────────────┐  │
              ▼              ▼              ▼  │
          Validation      Confidence     Safety│
              │              │              │  │
              └──────────────┼──────────────┘  │
                             ▼                 │
                     Business Policy           │
                             │                 │
                 ┌───────────┴───────────┐     │
                 ▼                       ▼     │
              Approval                Autopilot│
                 │                       │     │
                 └───────────┬───────────┘     │
                             ▼                 │
                           Action              │
                             │                 │
                             ▼                 │
                        Verification           │
                             │                 │
                             ▼                 │
                         Graphify              │
                             │                 │
                             ▼                 │
                           Audit              │
                             │                 │
                             ▼                 │
                         Metrics              │
```

---

# 2. ARCHITECTURAL BOUNDARY

Feature modules must never directly instantiate or call a vendor AI SDK.

Do NOT allow:

```csharp
new OpenAIClient(...)
```

inside:

- Content;
- Google;
- SEO;
- Social;
- Reputation;
- Competitor;
- Business Identity;
- Automation;
- API controllers.

Instead:

```text
Feature
  ↓
AI Orchestrator
  ↓
AI Provider abstraction
  ↓
Provider implementation
```

This is mandatory.

---

# 3. AI LAYERS

Use these conceptual layers.

## Layer 1 — Feature

Examples:

```text
GenerateArticle
GenerateGooglePost
AnalyzeSEO
AnalyzeCompetitor
ClassifyReview
GenerateBusinessIdentity
GenerateRecommendation
```

## Layer 2 — AI Orchestrator

Responsible for:

- authorization context;
- AI request lifecycle;
- context assembly;
- model selection;
- prompt selection;
- guardrails;
- provider invocation;
- structured response validation;
- confidence;
- usage tracking;
- retry;
- observability.

## Layer 3 — Agents

Specialized domain orchestration.

Recommended agents:

```text
ResearchAgent
BusinessIdentityAgent
SEOAgent
ContentAgent
SocialAgent
ReputationAgent
PortfolioAgent
CompetitorAgent
AutomationAgent
```

Agents should reuse the same provider abstraction.

They are not required to represent different AI models.

## Layer 4 — Provider

Provider-specific implementations:

```text
OpenAIProvider
AzureOpenAIProvider
```

Future providers can be added without changing feature/domain logic.

---

# 4. CORE INTERFACES

Implement equivalent abstractions if they do not already exist.

```csharp
public interface IAIProvider
{
    Task<AIProviderResult> GenerateAsync(
        AIProviderRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IAIOrchestrator
{
    Task<AIResult> ExecuteAsync(
        AIRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IAIContextBuilder
{
    Task<AIContext> BuildAsync(
        AIContextRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IPromptService
{
    Task<PromptDefinition> GetAsync(
        string promptKey,
        string version,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IAIModelRouter
{
    Task<AIModelSelection> SelectAsync(
        AIRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IAIResponseValidator
{
    Task<ValidationResult> ValidateAsync(
        AIResponse response,
        AIRequest request,
        CancellationToken cancellationToken);
}
```

```csharp
public interface IAIUsageMeter
{
    Task RecordAsync(
        AIUsage usage,
        CancellationToken cancellationToken);
}
```

Use existing project conventions if equivalent abstractions already exist.

---

# 5. AI REQUEST CONTRACT

AI requests should contain enough metadata for safe orchestration.

Conceptually:

```text
AIRequest
├── TenantId
├── BusinessId
├── UserId
├── Feature
├── Agent
├── Operation
├── ContextRequirements
├── PromptKey
├── PromptVersion
├── OutputSchema
├── RiskLevel
├── RequestedModel
├── TokenBudget
├── Temperature
├── CorrelationId
└── CancellationToken
```

Do not allow a browser/client to override security-sensitive fields.

Tenant, user, authorization, entitlement and risk information must be determined server-side.

---

# 6. AI RESPONSE CONTRACT

AI responses should be structured whenever the application needs to act on them.

Conceptually:

```text
AIResult
├── Success
├── Output
├── StructuredData
├── Model
├── Provider
├── PromptVersion
├── Confidence
├── Claims
├── EvidenceReferences
├── Usage
├── Duration
├── Validation
├── RequiresApproval
├── SafetyFlags
└── CorrelationId
```

Avoid relying on uncontrolled free-form model text for business decisions.

---

# 7. PROVIDER CONFIGURATION

Provider selection must be configuration-driven.

Example:

```json
{
  "AI": {
    "Provider": "OpenAI",
    "DefaultModel": "configured-per-environment",
    "Temperature": 0.2,
    "MaxOutputTokens": 4000,
    "TimeoutSeconds": 60,
    "Retry": {
      "MaxAttempts": 3
    }
  }
}
```

Do not hard-code production model names in business logic.

Provider-specific configuration should be isolated.

Example:

```text
AI
├── Provider
├── DefaultModel
├── Timeout
├── Retry
├── TokenBudget
└── Providers
    ├── OpenAI
    └── AzureOpenAI
```

---

# 8. SECRET MANAGEMENT

Never store these in:

- source code;
- Git;
- frontend;
- appsettings committed to source control;
- database plaintext;
- logs.

Secrets include:

- API keys;
- Azure OpenAI credentials;
- provider tokens;
- connection secrets.

Use:

```text
Development
→ User Secrets / environment variables

Test
→ CI secret store

Staging
→ Azure Key Vault

Production
→ Azure Key Vault
```

The browser must never receive an AI provider secret.

---

# 9. MODEL ROUTING

Implement a model-routing abstraction.

The application should be able to select a model based on:

- feature;
- complexity;
- latency requirement;
- output type;
- token budget;
- tenant policy;
- environment;
- cost policy.

Example:

```text
Simple classification
        ↓
Low-cost model

Content generation
        ↓
Standard model

Complex business reasoning
        ↓
High-capability model
```

Do not implement arbitrary model selection from browser input.

The server decides whether a requested model is permitted.

---

# 10. PROMPT MANAGEMENT

Prompts must be versioned.

Recommended structure:

```text
AI/
└── Prompts/
    ├── BusinessIdentity/
    ├── Content/
    ├── GoogleBusinessProfile/
    ├── SEO/
    ├── Social/
    ├── Reputation/
    ├── Competitor/
    └── Automation/
```

Every AI result should be traceable to:

```text
PromptKey
PromptVersion
Model
Provider
```

Never silently change a production prompt without versioning.

Prompt versions should support:

```text
Draft
Active
Retired
```

Do not expose internal system prompts to tenants.

---

# 11. CONTEXT ASSEMBLY

Never send the entire business database to the model.

Context must be selected.

Example:

```text
Generate Google post
        ↓
Context Builder
        ↓
Business
BusinessLocation
ContentItem
Services
ApprovedFacts
Projects
Media
BrandVoice
RestrictedClaims
PlatformCapabilities
Permissions
        ↓
Minimal relevant context
        ↓
AI
```

The context builder should be feature-aware.

---

# 12. GRAPHIFY INTEGRATION

Graphify is the business knowledge/context graph.

Before AI:

```text
AI Request
   ↓
Graphify Retrieval
   ↓
Relevant entities + relationships
```

Potential entities:

```text
Business
Location
Service
Brand
Project
Finding
Content
Platform
Fact
Permission
Approval
```

Potential relationships:

```text
Business → HAS_SERVICE → Service
Business → HAS_LOCATION → Location
Project → USES_SERVICE → Service
Project → HAS_MEDIA → Media
Content → BASED_ON → Project
Finding → RELATES_TO → Business
```

Graphify must return only context relevant to the operation.

After AI:

```text
AI result
   ↓
Graphify update
```

Record useful:

- extracted entities;
- relationships;
- evidence references;
- decisions;
- outcomes;
- confidence;
- action results.

Graphify does not replace SQL transactional state.

---

# 13. RETRIEVAL / RAG

Use the existing provider-neutral search abstraction.

Required concepts:

```text
ISearchProvider
IVectorSearchProvider
IEmbeddingProvider
IRetrievalService
IKnowledgeIndexManager
```

Supported providers remain configurable:

```text
Azure AI Search
OpenSearch
Elasticsearch
PostgreSQL/vector
InMemory
Custom
```

Retrieval must be:

- tenant-scoped;
- business-scoped where appropriate;
- permission-aware;
- relevance-ranked;
- traceable.

Never retrieve another tenant's information.

---

# 14. KNOWLEDGE SOURCES

AI may use:

### Trusted sources

- approved BusinessFacts;
- Business Digital Identity;
- approved Services;
- approved Projects;
- approved Media;
- approved content;
- authorized platform snapshots;
- verified findings;
- approved claims.

### Untrusted sources

- arbitrary web pages;
- crawled text;
- external comments;
- external reviews;
- user-generated content;
- imported documents that have not been trusted.

Untrusted content must never override canonical business facts.

Treat external content as potential prompt injection.

---

# 15. EVIDENCE-BASED GENERATION

Mandatory rules:

```text
No evidence
    ↓
No factual claim

Conflicting evidence
    ↓
Human review

Restricted fact
    ↓
Never publish

Low confidence
    ↓
Approval / Assisted

Verified + low risk + policy approved
    ↓
Eligible for Autopilot
```

For generated claims, retain evidence references where practical.

Example:

```json
{
  "claim": "AV Professionals provides conference-room AV solutions",
  "sourceType": "BusinessService",
  "sourceId": "service-id",
  "confidence": 0.97
}
```

---

# 16. AI GUARDRAILS

Implement application-level guardrails before and after the model call.

## Input guardrails

Check:

- authorization;
- tenant scope;
- business scope;
- data classification;
- prompt injection indicators;
- restricted data;
- token limits;
- provider availability.

## Output guardrails

Check:

- required schema;
- factual claims;
- evidence;
- restricted claims;
- unsafe content;
- unexpected instructions;
- external URLs;
- sensitive data;
- platform constraints;
- confidence.

AI output must never directly bypass domain validation.

---

# 17. AI OUTPUT VALIDATION

For structured generation:

```text
AI
 ↓
Deserialize
 ↓
Schema validation
 ↓
Business validation
 ↓
Evidence validation
 ↓
Policy validation
 ↓
Confidence
 ↓
Approved result
```

Invalid structured output should be rejected or safely regenerated according to configured retry policy.

Never silently accept malformed AI output.

---

# 18. CONFIDENCE MODEL

AI confidence must not be treated as proof of truth.

Use confidence as one signal combined with evidence.

Example:

```text
Evidence strength
+
Source trust
+
Conflict state
+
AI confidence
+
Business policy
=
Decision
```

A high AI confidence score cannot override missing evidence or conflicting canonical facts.

---

# 19. AI → ACTION BOUNDARY

AI must not directly execute external actions.

Correct:

```text
AI
 ↓
Recommendation / Content
 ↓
Domain validation
 ↓
Approval / Automation Policy
 ↓
Action
 ↓
Provider Adapter
 ↓
External Platform
 ↓
Verification
```

Incorrect:

```text
AI
 ↓
Google API
```

This rule is mandatory.

---

# 20. GOOGLE BUSINESS PROFILE EXAMPLE

For a Google Business Profile post:

```text
User selects Article
        ↓
GooglePostAgent
        ↓
Graphify
        ↓
Verified Business + Location + Services + Article
        ↓
Prompt
        ↓
AI Provider
        ↓
Structured GooglePost
        ↓
Evidence Validation
        ↓
Google Policy/Application Validation
        ↓
Approval
        ↓
Action
        ↓
Google Adapter
        ↓
Provider confirmation
        ↓
Verification
```

AI creates the post.

The Google adapter publishes it.

Verification confirms the result.

---

# 21. CONTENT REPURPOSING

One canonical source should produce platform-specific variants.

Example:

```text
Canonical Article
       │
       ├── Google Business Profile
       ├── Website
       ├── DigitalPulse Content Hub
       ├── LinkedIn
       ├── Facebook
       ├── Instagram
       ├── YouTube
       └── WhatsApp
```

Each variant should have:

- platform;
- prompt version;
- model;
- source ContentItem;
- generated timestamp;
- confidence;
- approval state;
- publication state.

Never treat all platforms as the same content format.

---

# 22. AGENT DESIGN

Agents should focus on domain behavior.

## ResearchAgent

Responsible for:

- research planning;
- source retrieval;
- evidence collection;
- research synthesis.

## BusinessIdentityAgent

Responsible for:

- identity normalization;
- fact conflict detection;
- canonical identity suggestions.

## SEOAgent

Responsible for:

- SEO analysis;
- keyword/topic opportunities;
- metadata;
- internal linking recommendations;
- AEO opportunities.

## ContentAgent

Responsible for:

- articles;
- case studies;
- guides;
- FAQs;
- content briefs.

## SocialAgent

Responsible for:

- LinkedIn;
- Facebook;
- Instagram;
- YouTube;
- platform variants.

## ReputationAgent

Responsible for:

- review classification;
- theme extraction;
- sentiment where appropriate;
- response drafting.

## PortfolioAgent

Responsible for:

- project storytelling;
- case studies;
- portfolio content.

## CompetitorAgent

Responsible for:

- public competitor content analysis;
- opportunity detection;
- comparison evidence.

## AutomationAgent

Responsible for:

- action recommendations;
- automation eligibility;
- next-best-action proposals.

Agents must not bypass authorization or action boundaries.

---

# 23. AI USAGE METERING

Every model invocation should record usage.

Recommended fields:

```text
TenantId
BusinessId
UserId
Feature
Agent
Provider
Model
PromptKey
PromptVersion
InputTokens
OutputTokens
TotalTokens
EstimatedCost
DurationMs
Success
FailureCategory
CorrelationId
CreatedAtUtc
```

Use the existing `UsageRecord` architecture where appropriate.

Do not duplicate usage entities if the existing model can support AI usage.

---

# 24. AI ENTITLEMENTS

Respect:

```text
AI_GENERATIONS_PER_MONTH
```

and other applicable plan entitlements.

Flow:

```text
AI Request
 ↓
Entitlement Check
 ↓
Usage Check
 ↓
Execute
 ↓
Record Usage
```

Avoid consuming quota for requests rejected before provider invocation unless the product policy explicitly defines otherwise.

Handle concurrent requests safely so users cannot exceed limits through race conditions.

---

# 25. TOKEN AND COST CONTROL

Optimize cost through:

- Graphify context selection;
- retrieval;
- context compression;
- prompt reuse;
- caching where safe;
- model routing;
- token budgets;
- output limits;
- duplicate-request detection;
- incremental analysis.

Never send:

- entire database;
- entire repository;
- unnecessary business history;
- unrelated tenant data.

This is especially important for SaaS scale.

---

# 26. CACHING

AI results may be cached only where safe.

Cache candidates:

- deterministic analysis;
- repeated read-only classification;
- reusable retrieval results;
- stable platform metadata.

Do not cache tenant-sensitive results across tenants.

Cache keys must include appropriate scope:

```text
Tenant
Business
Feature
Input fingerprint
Prompt version
Model
Relevant configuration
```

Invalidate when source data changes.

---

# 27. RETRIES

Provider retries must be bounded.

Retry candidates:

- transient network failure;
- temporary provider outage;
- rate-limit response where provider guidance allows;
- safe transient errors.

Do not blindly retry:

- invalid request;
- policy rejection;
- authentication failure;
- invalid structured output without a controlled retry strategy.

All retries must remain observable.

---

# 28. TIMEOUTS AND CANCELLATION

Every AI request must support:

```csharp
CancellationToken
```

Configure:

- provider timeout;
- overall orchestration timeout;
- cancellation propagation.

Do not allow abandoned requests to continue consuming resources unnecessarily.

---

# 29. ASYNC PROCESSING

Long-running AI workflows should use durable background processing where appropriate.

Examples:

- large research tasks;
- bulk content generation;
- multi-platform repurposing;
- large competitor analysis;
- scheduled AI jobs.

Architecture:

```text
API
 ↓
Command
 ↓
Queue
 ↓
Worker
 ↓
AI Orchestrator
 ↓
Provider
 ↓
Persist Result
 ↓
Notify User
```

Do not put long-running AI workflows into synchronous HTTP requests unnecessarily.

---

# 30. OBSERVABILITY

Every AI request should have:

- correlation ID;
- structured logging;
- duration;
- provider;
- model;
- feature;
- success/failure;
- token usage;
- retry count.

Metrics:

```text
AI Requests
AI Success Rate
AI Failure Rate
AI Latency
Tokens Used
Estimated Cost
Provider Errors
Model Errors
Validation Failures
Safety Blocks
Approval Rate
Regeneration Rate
```

Never log:

- API keys;
- access tokens;
- refresh tokens;
- secrets;
- private prompts where prohibited;
- sensitive customer data unnecessarily.

---

# 31. AI AUDIT

Important AI decisions must be auditable.

Record:

- who requested the AI operation;
- tenant/business;
- feature;
- prompt version;
- model;
- result status;
- approval requirement;
- safety decision;
- action created;
- final action result.

Do not store unnecessary sensitive model input/output indefinitely.

Apply appropriate retention policies.

---

# 32. HUMAN APPROVAL

AI-generated externally visible content should support approval.

Example:

```text
AI Generated
     ↓
Validation
     ↓
Approval Required?
   /        \
 YES         NO
  ↓           ↓
Approval    Policy
  ↓           ↓
 Action     Action
```

Approval state must be independent from AI confidence.

A high-confidence AI response can still require approval because the action is high-risk.

---

# 33. AUTOPILOT

Autopilot may execute only when:

```text
Verified evidence
+
Low risk
+
Policy allowed
+
Capability available
+
Entitlement available
+
Authorization valid
+
No restricted claim
+
No unresolved conflict
```

The AI model does not decide by itself whether it can bypass approval.

The domain policy engine decides.

---

# 34. AI EVALUATION FRAMEWORK

Create repeatable AI evaluation datasets for important agents.

Evaluate:

- factuality;
- evidence grounding;
- structured-output validity;
- instruction following;
- platform compliance;
- hallucination rate;
- unsafe output;
- tenant isolation;
- restricted-claim leakage;
- consistency;
- regression.

Example:

```text
Evaluation Dataset
        ↓
Agent
        ↓
Model
        ↓
Expected criteria
        ↓
Evaluation
        ↓
PASS / FAIL
```

Prompt/model changes should be evaluated before production rollout where practical.

---

# 35. AI TESTING

## Unit tests

Test:

- context selection;
- policy decisions;
- model routing;
- entitlement checks;
- claim validation;
- confidence handling;
- prompt selection.

## Integration tests

Test:

- AI orchestrator;
- provider adapter;
- Graphify;
- retrieval;
- persistence;
- usage metering.

## Contract tests

Validate provider response mapping.

## Safety tests

Test:

- prompt injection;
- malicious crawled content;
- restricted claims;
- cross-tenant retrieval;
- secret leakage;
- unsafe URLs;
- unauthorized action attempts.

## Evaluation tests

Run representative datasets for each important agent.

---

# 36. AI DEVELOPMENT WITH CODEGRAPH

Before changing AI code:

```text
CodeGraph
 ↓
AI Orchestrator
 ↓
Provider interfaces
 ↓
Provider implementations
 ↓
Agents
 ↓
Prompt infrastructure
 ↓
Retrieval
 ↓
Graphify integration
 ↓
Usage
 ↓
Tests
```

After changes:

```text
Impact Analysis
 ↓
Targeted Tests
 ↓
Integration Tests
 ↓
Security Tests
 ↓
AI Evaluation
```

Do not modify unrelated features.

---

# 37. AI DEVELOPMENT WITH GRAPHIFY

Before AI implementation/testing:

```text
Graphify
 ↓
Find:
Business
Services
Projects
Facts
Content
Platform
Permissions
```

After implementation:

```text
Graphify
 ↓
Update:
AI capability
Agent relationships
Prompt/version relationships
Feature dependencies
Outcome relationships
```

Graphify is not the source of truth for:

- authorization;
- SQL transactions;
- approval records;
- action execution;
- audit integrity.

---

# 38. DATABASE INTEGRATION

Use existing DigitalPulse entities where possible.

Potential entities include:

```text
UsageRecord
BusinessFact
ContentItem
ContentVariant
ApprovalRequest
Action
ActionAttempt
Verification
AuditEvent
```

Add dedicated AI tables only when required.

Potential optional entities:

```text
AIPromptDefinition
AIPromptVersion
AIExecution
AIExecutionClaim
AIExecutionEvidence
AIModelConfiguration
AIEvaluationRun
AIEvaluationCase
```

Before adding them, inspect the existing schema and avoid duplication.

All tenant-owned AI records must be tenant-scoped.

---

# 39. MULTI-TENANT AI ISOLATION

This is mandatory.

```text
Tenant A
   ↓
Graphify A
   ↓
Retrieval A
   ↓
AI Context A

Tenant B
   ↓
Graphify B
   ↓
Retrieval B
   ↓
AI Context B
```

Never allow:

```text
Tenant A → Tenant B context
```

Test this explicitly.

Tenant isolation must be enforced server-side.

---

# 40. FRONTEND AI EXPERIENCE

AI UI should communicate:

- what AI is doing;
- what information it used;
- confidence where meaningful;
- evidence;
- generated content;
- why an approval is required;
- what action will happen.

Avoid generic:

> “AI is thinking…”

Prefer contextual states:

```text
Understanding business context…
Checking verified facts…
Preparing Google Business Profile content…
Validating claims…
Ready for approval…
```

Do not expose chain-of-thought or private internal reasoning.

Show concise explanations and evidence instead.

---

# 41. AI RESULT UX

For a generated recommendation:

```text
Recommendation
────────────────────────────

Create a Google Business Profile update
for the new conference-room project.

Why:
• Project is approved for publication
• Service is verified
• Location is verified
• No restricted claims detected

Evidence:
✓ Project
✓ Service
✓ Business location

Confidence:
High

[Review] [Approve] [Dismiss]
```

This is preferable to exposing internal reasoning.

---

# 42. PROVIDER FAILURE FALLBACK

If the primary AI provider fails:

```text
Provider unavailable
        ↓
Configured fallback?
     /       \
   Yes        No
    ↓          ↓
Fallback     Clear error
provider
```

Fallback must be explicitly configured and policy-approved.

Do not silently switch providers if doing so could change:

- data residency;
- contractual requirements;
- cost;
- privacy;
- model behavior.

Record provider selection in telemetry.

---

# 43. DATA PRIVACY

AI requests must follow DigitalPulse data classification.

Do not send unnecessary:

- personal data;
- secrets;
- credentials;
- payment data;
- private customer information;
- unrelated tenant data.

Minimize context.

Where required, redact or transform sensitive information before provider invocation.

---

# 44. AI FEATURE FLAGS

Use feature flags/configuration for:

- provider rollout;
- model rollout;
- new agent;
- prompt version;
- fallback provider;
- experimental capability.

Example:

```text
AI:ContentGeneration
AI:GooglePostGeneration
AI:CompetitorAgent
AI:Autopilot
```

Feature flags must not bypass security or authorization.

---

# 45. AI ADMIN / OPERATIONS VIEW

Provide internal operational visibility where appropriate.

Show:

- provider health;
- model;
- request volume;
- latency;
- error rate;
- token usage;
- estimated cost;
- evaluation status;
- failed AI operations.

Do not expose customer secrets.

Tenant users should only see their own permitted usage and results.

---

# 46. DOCUMENTATION

Update:

```text
docs/ai/
```

with:

- architecture;
- providers;
- configuration;
- prompt management;
- agents;
- Graphify;
- retrieval;
- guardrails;
- security;
- usage;
- evaluations;
- troubleshooting.

Create ADRs for significant decisions such as:

- provider selection;
- model routing;
- fallback strategy;
- prompt storage;
- Graphify integration;
- retrieval architecture.

---

# 47. IMPLEMENTATION ORDER

**Overall: 30% (3/10 phases).**

Implement in this order after auditing existing code.

## Phase 1 — completed (100%)
AI Architecture Audit

Inspected 2026-09-26 against the live DigitalPulse tree (CodeGraph + source). Feature modules already talk to `IAiProvider`; no vendor SDK is constructed in Content, Google, SEO, Social, or API endpoints. Live OpenAI is isolated in Infrastructure. Development composition is an honest hold when `Ai:OpenAi:ApiKey` is empty — it is not a fake live model.

### Requirement-to-code matrix

| Requirement | Status | File/Class | Action | Test |
|---|---|---|---|---|
| Provider abstraction | Existing | `IAiProvider`, `AiCompletionRequest` | Keep; do not add a second SDK surface | `AiOrchestratorTests` |
| OpenAI isolated | Existing | `OpenAiProvider` | Keep HTTP client in Infrastructure | none dedicated |
| Azure OpenAI | Missing | — | Add isolated provider + config | Phase 2 |
| Provider selection | Partial | `Infrastructure/DependencyInjection` chooses OpenAI vs Development by API key | Honor `Ai:Provider` + Azure | Phase 2 |
| Model server-controlled | Partial | `Ai:OpenAi:Model` | Route by agent/budget, never from browser | Phase 3/7 |
| Secrets | Existing | `Ai:OpenAi:ApiKey` via config/user-secrets; not returned to UI | Keep empty in committed appsettings | — |
| Orchestrator | Partial | `RunAiHandler` is the run path | Extract `IAiOrchestrator` so features stop calling the vendor abstraction | Phase 3 |
| Agents | Existing | `AiAgentCatalog` (10 agents) | Versioned prompts per agent | Phase 5 |
| Prompt management | Partial | `RunAiHandler.BuildPrompt` | Versioned catalog (`identity.v1`, `content.v1`, …) | Phase 3/5 |
| Structured outputs | Missing | free-text `AiCompletionResponse.Output` | Optional JSON envelope + schema check | Phase 3/6 |
| Response validation | Existing | `AiPolicy.Evaluate` | Keep evidence/conflict/restricted rules | `AiOrchestratorTests` |
| Context builder | Partial | `RetrieveEvidenceAsync` inside `RunAiHandler` | Extract `AiContextBuilder` | Phase 4 |
| Graphify | Existing | `GraphifySync`, `GraphNode`/`GraphEdge` | Keep attach/rebuild/decision | Content Hub Phase 15 |
| Retrieval tenant-scoped | Existing | `ISearchProvider.SearchAsync(tenantId, businessId, …)` | Keep | Phase 4 isolation test |
| Evidence grounding | Existing | `AiEvidence` + prompt lines | Keep | `AiOrchestratorTests` |
| Restricted claims | Existing | `AiPolicy` + `FactStatus.Restricted` | Keep | `Restricted_fact_in_output_never_passes` |
| Prompt-injection defenses | Missing | `ContentGuard` only bans sexual copy | Detect ignore-instructions / jailbreak | Phase 6 |
| Confidence | Existing | `AiConfidence` on `AiRun` | Keep; never treat as proof | `AiOrchestratorTests` |
| Approval / no direct action | Existing | Run stores hold; no adapter execute | Keep Action/Verification boundary | Phase 9/10 |
| Autopilot policy | Partial | Catalog says Phase 10 | Domain policy: never auto-publish | Phase 9/10 |
| Usage metering | Existing | `UsageMeter` + `UsageKind.AiGeneration` | Add token/cost fields | Phase 7 |
| Entitlement | Existing | `EntitlementRules.EnsureCanRunAi` | Keep | Phase 7 |
| Token/cost controls | Missing | run count only | Tokens on completion + estimate | Phase 7 |
| Retry | Missing | OpenAI single POST | Retry wrapper, then honest hold | Phase 2 |
| Timeout/cancel | Partial | HttpClient 30s + CT | Configurable timeout | Phase 2 |
| Background execution | Existing | Worker host exists; AI is request-scoped | Hold unless a job is required | Phase 10 |
| Observability | Partial | `AiAuditEvent` stages | Keep; add docs | Phase 10 |
| Evaluation | Partial | `AiEvaluation` + unit policy tests | Dataset / safety / regression | Phase 8 |
| Feature → AI layer | Partial | Content + WhatsApp call `IAiProvider` directly | Route through orchestrator | Phase 9 |
| Google post via AI | Partial | Social compose exists; not orchestrated | Social/Google agent path | Phase 9 |
| Isolation tests | Partial | tenant filters exist; no AI-run isolation test | Add | Phase 10 |
| `docs/ai/` | Missing | README only mentions orchestrator | Write architecture notes | Phase 10 |
| Fake live provider | Existing hold | `DevelopmentAiProvider.IsLive = false` | Keep as development hold, never mark live | Phase 2 |

### Audit findings (do not duplicate)

- Reuse `IAiProvider`, `AiPolicy`, `AiAgentCatalog`, `GraphifySync`, `ISearchProvider`, `UsageMeter`, `EntitlementRules`, `AiRun`/`AiEvaluation`/`AiAuditEvent`.
- Do not add a second Graphify product. Repo Graphify (`graphify-out/`) is not `dp.GraphNode`.
- Do not invent Search Console volume, views, or unofficial scrapes inside agents.
- TypeSafe/Jev is not a live provider in this repo; do not wire it as production intelligence.

## Phase 2 — completed (100%)
Provider Abstraction

Implement/complete:

- IAIProvider;
- provider implementations;
- configuration;
- secret handling;
- timeout;
- retry;
- cancellation.

## Phase 3 — completed (100%)
Orchestrator

Implement/complete:

- IAIOrchestrator;
- context builder;
- model router;
- prompt service;
- response validation;
- usage metering.

## Phase 4 — Graphify + Retrieval

Implement/complete:

- context retrieval;
- tenant scoping;
- relevant entity selection;
- retrieval;
- evidence references;
- Graphify updates.

## Phase 5 — Agents

Implement/complete required agents.

## Phase 6 — Guardrails

Implement:

- input validation;
- output validation;
- evidence rules;
- restricted claims;
- prompt-injection defenses;
- policy validation.

## Phase 7 — Cost and Usage

Implement:

- token tracking;
- cost estimation;
- entitlement enforcement;
- rate limits;
- caching;
- model routing.

## Phase 8 — AI Evaluation

Implement:

- evaluation datasets;
- regression tests;
- quality checks;
- safety tests.

## Phase 9 — Feature Integration

Verify AI integration with:

- Business Identity;
- Digital Pulse Check;
- Website/SEO;
- Content Hub;
- Google Business Profile;
- Social;
- Projects;
- Reputation;
- Competitors;
- Actions/Autopilot.

## Phase 10 — Production Hardening

Verify:

- security;
- tenant isolation;
- resilience;
- observability;
- cost controls;
- secrets;
- deployment;
- documentation.

---

# 48. FINAL ACCEPTANCE CRITERIA

The AI architecture is complete only when all applicable items are PASS:

- [x] AI provider abstraction exists.
- [x] Feature modules do not directly call vendor SDKs.
- [x] OpenAI integration is provider-isolated.
- [x] Azure OpenAI integration is provider-isolated where configured.
- [x] Provider selection is configurable.
- [x] Model selection is server-controlled.
- [x] Secrets are protected.
- [x] AI Orchestrator is implemented.
- [x] Agents are implemented where required.
- [x] Prompt management is versioned.
- [x] Structured outputs are supported.
- [x] Response validation exists.
- [ ] Context builder exists.
- [x] Graphify integration exists.
- [x] Retrieval is provider-neutral.
- [x] Retrieval is tenant-scoped.
- [x] Evidence grounding exists.
- [x] Restricted-claim protection exists.
- [ ] Prompt-injection defenses exist.
- [x] Confidence handling exists.
- [x] Approval integration exists.
- [ ] Autopilot respects domain policy.
- [x] AI cannot directly execute external actions.
- [x] Usage metering exists.
- [x] AI entitlement enforcement exists.
- [ ] Token/cost controls exist.
- [x] Retry handling exists.
- [x] Timeout/cancellation exists.
- [ ] Background execution exists where required.
- [x] Observability exists.
- [x] Auditability exists.
- [ ] AI evaluation exists.
- [x] AI security tests exist.
- [ ] Cross-tenant isolation tests exist.
- [ ] Google Business Profile generation works through the AI layer.
- [x] Content Hub generation works through the AI layer.
- [ ] Platform-specific content generation works.
- [ ] Documentation is updated.
- [x] No fake AI/provider implementation remains.
- [x] No TODO/placeholder remains for required scope.
- [x] No known critical/high defect remains.
- [ ] All required tests pass.

---

# 49. FINAL END-TO-END TEST

Use this final scenario:

```text
Business:
AV Professionals

Project:
Corporate Conference Room Installation

User:
Generate a Google Business Profile post.
```

Expected:

```text
User Request
     ↓
Authorization
     ↓
Entitlement Check
     ↓
Graphify Context
     ↓
Retrieve:
  Business
  Location
  Project
  Services
  Approved Facts
  Brand Voice
  Google Capability
     ↓
GooglePostAgent
     ↓
Prompt Version
     ↓
Model Router
     ↓
AI Provider
     ↓
Structured Result
     ↓
Schema Validation
     ↓
Evidence Validation
     ↓
Business Policy Validation
     ↓
Confidence
     ↓
Approval Required
     ↓
ContentVariant
     ↓
Action
     ↓
Google Adapter
     ↓
Google
     ↓
Verification
     ↓
Audit
     ↓
Usage Record
     ↓
Graphify Outcome
```

The AI layer must stop at the **content/recommendation/decision boundary**.

The Action/Provider/Verification layers remain responsible for actual external execution.

---

# 50. FINAL CURSOR / CLAUDE OPUS 5.5 INSTRUCTION

Do not simply create interfaces.

Do not create mock AI responses.

Do not hard-code fake model responses.

Do not hard-code provider success.

Do not expose API keys.

Do not place vendor SDK calls inside feature modules.

Do not send the entire repository or database into AI.

Do not allow AI to bypass authorization, approval, entitlement, policy, or verification.

Do not expose chain-of-thought.

Do not treat AI confidence as factual proof.

Do not allow crawled/untrusted content to override verified business facts.

Do not declare the AI architecture complete because an LLM returned text.

Implement and verify:

**Context → Retrieval → Prompt → Model → Structured Result → Validation → Policy → Approval/Autopilot → Action → Verification → Audit → Usage → Evaluation**

Use **Graphify** to reduce and structure business context.

Use **CodeGraph** to reduce and structure code context during development.

Keep SQL as the transactional source of truth.

Keep provider integrations behind abstractions.

Keep external execution behind Actions and provider adapters.

Keep human approval available for externally visible/high-risk actions.

Finally, produce:

1. Requirement-to-code matrix.
2. Files/classes changed.
3. Database changes.
4. Provider configuration.
5. AI agents implemented.
6. Prompt versions implemented.
7. Graphify integration.
8. Retrieval integration.
9. Security findings.
10. Test results.
11. AI evaluation results.
12. Remaining gaps.

Do not report `COMPLETE` until every required acceptance criterion is **PASS**.
