# DigitalPulse — Content Publishing & Distribution Engine

## Purpose

Implement a production-ready Content Publishing & Distribution Engine for DigitalPulse.

The engine must take a canonical DigitalPulse `ContentItem`, prepare platform-specific variants, obtain required approvals, publish through official provider capabilities, verify the result, record the outcome, and support scheduling, monitoring, analytics, and audit.

The three primary publishing destinations are:

1. **DigitalPulse Public Content Hub**
2. **Customer Website**
3. **Google Business Profile (GBP)** — this is a core DigitalPulse selling point and must receive first-class product treatment.

Additional destinations:

4. LinkedIn
5. Facebook
6. Instagram
7. YouTube
8. WhatsApp, where officially supported and consented

---

# 1. NON-NEGOTIABLE IMPLEMENTATION RULES

- Read the DigitalPulse master specification and all applicable `.cursor/rules` before implementation.
- Use **Claude Opus 5.5** for this implementation.
- Use **CodeGraph** before code changes to retrieve only the relevant symbols, dependencies, routes, services, entities, migrations, tests, and provider adapters.
- Use **Graphify** before AI generation/publishing decisions to retrieve relevant Business Identity, verified facts, Services, Projects, Media, Locations, ContentItem context, permissions, findings, approvals, and platform capabilities.
- After AI calls, update Graphify with useful entities, decisions, dependencies, outcomes, and verified facts.
- Do not dump the entire repository into the model context when CodeGraph/Graphify can provide targeted context.
- Do not create fake, simulated, hard-coded, mock-success, or placeholder provider integrations.
- Do not claim a post was published unless the official provider confirms the operation.
- Do not mark an action as successful merely because an API request was sent.
- Do not bypass tenant, business, location, role, permission, approval, or entitlement checks.
- Do not store provider access tokens or secrets in plaintext.
- Do not publish unverified business claims.
- Do not invent testimonials, awards, certifications, project results, statistics, customer names, addresses, prices, or other factual claims.
- Do not copy the full canonical article into platforms that require a shorter platform-specific post.
- Do not declare this feature complete until UI, API, domain logic, database, provider integration, approval, execution, verification, audit, and automated tests work together.

---

# 2. PRODUCT OBJECTIVE

DigitalPulse should allow a business to move from:

**Create → Optimize → Approve → Schedule → Publish → Verify → Monitor**

Example:

A business creates an article:

> “Complete Conference Room AV Guide for Modern Businesses”

DigitalPulse should be able to:

- publish the canonical article to the DigitalPulse public Content Hub;
- publish it to the customer's website if an official CMS/API connection exists;
- create an optimized Google Business Profile post;
- allow the user to review the Google post;
- publish it to the selected Google Business Profile location(s);
- verify that Google accepted the publication;
- store the external provider ID and publication status;
- create variants for LinkedIn, Facebook, Instagram, YouTube, and WhatsApp where supported;
- schedule or publish those variants according to connection capabilities and approval policy;
- show the entire distribution lifecycle in one place.

---

# 3. CANONICAL CONTENT MODEL

The canonical source of truth is the DigitalPulse `ContentItem`.

A ContentItem should support, as applicable:

- TenantId
- BusinessId
- title
- slug
- summary
- canonical content/body
- excerpt
- content type
- category
- tags
- author
- featured media
- supporting media
- related services
- related projects
- related business facts
- SEO metadata
- AEO metadata
- status
- visibility
- approval state
- created/updated/published timestamps
- created/updated by
- version/revision information

The canonical article must remain independent from platform-specific representations.

---

# 4. PLATFORM DISTRIBUTION MODEL

Every publication target must be represented as a distribution target.

Recommended conceptual model:

```text
ContentItem
    |
    +-- ContentDistribution
             |
             +-- Platform
             +-- Connection
             +-- Business
             +-- BusinessLocation
             +-- ContentVariant
             +-- Status
             +-- ScheduledAt
             +-- PublishedAt
             +-- ExternalId
             +-- Verification
             +-- Failure
```

Use existing DigitalPulse entities such as:

- `ContentItem`
- `ContentVariant`
- `PlatformConnection`
- `Action`
- `ActionAttempt`
- `ApprovalRequest`
- `ApprovalDecision`
- `Verification`
- `Business`
- `BusinessLocation`
- `AuditEvent`

Extend the schema only where required.

Avoid duplicate lifecycle concepts when an existing entity can safely be reused.

---

# 5. GOOGLE BUSINESS PROFILE — PRIMARY SELLING POINT

Google Business Profile publishing must be treated as a first-class DigitalPulse capability.

The product should clearly communicate:

> **Turn every article into a Google Business Profile update that reaches customers where they search for your business.**

Do not make Google merely another item in a generic social-media list.

Google publishing should have:

- dedicated UI;
- dedicated optimization workflow;
- dedicated preview;
- location selection;
- approval;
- scheduling where officially supported;
- publication;
- verification;
- external post ID/status;
- failure handling;
- audit trail;
- analytics where provider capabilities allow.

The implementation must use the **official supported Google Business Profile API/capability** available to the application.

If a required Google capability is unavailable for the configured account/API:

- do not fake the capability;
- do not silently skip the operation;
- expose the capability state;
- offer Assisted or Manual publishing;
- explain exactly what the user must do;
- never show “Published” unless Google confirms publication.

Provider capabilities must be discovered/configured rather than assumed.

---

# 6. GOOGLE ARTICLE → GOOGLE BUSINESS PROFILE WORKFLOW

Implement the following workflow:

```text
Article
   ↓
Generate Google Post
   ↓
AI Optimization
   ↓
Google Preview
   ↓
Select Business Location(s)
   ↓
Validate Content + Policy + Claims
   ↓
Approval
   ↓
Schedule or Publish
   ↓
Google Business Profile
   ↓
Provider Confirmation
   ↓
Verification
   ↓
Store External ID + Result
   ↓
Monitor
```

The Google variant must be a purpose-built Google Business Profile post.

It must NOT simply copy the entire article.

Generate a concise platform-specific post containing, as supported:

- strong opening;
- useful business-relevant message;
- article value proposition;
- approved factual claims only;
- relevant call-to-action;
- destination URL when supported;
- media when supported;
- location context when appropriate.

Respect the current official Google Business Profile API capabilities and content rules.

---

# 7. GOOGLE LOCATION SUPPORT

A business may have multiple locations.

The publishing workflow must allow:

- one location;
- multiple selected locations;
- all eligible locations.

Before publication, verify:

- the selected location belongs to the current Business;
- the current Tenant owns the Business;
- the connection is authorized;
- the provider supports the required operation;
- the location is eligible;
- the user has permission;
- plan entitlement allows the action.

Never allow cross-tenant or cross-business publishing.

Each location publication should have an independently trackable result.

Example:

```text
Article
 ├── Google Location A → Published
 ├── Google Location B → Published
 └── Google Location C → Failed
```

Do not collapse these into one misleading global success state.

---

# 8. WEBSITE PUBLISHING

Support customer website publishing through official integrations.

Examples may include:

- WordPress
- supported CMS APIs
- supported website platforms
- DigitalPulse-hosted Content Hub

For each website connection:

```text
Connect
→ HealthCheck
→ GetCapabilities
→ Prepare
→ Preview
→ Approve
→ Publish
→ Verify
```

If a website does not have an official supported publishing integration:

- provide Assisted/Manual mode;
- provide the generated article and metadata;
- provide copy/export actions;
- clearly show that DigitalPulse did not publish automatically.

Never claim automatic website publication without provider confirmation.

---

# 9. DIGITALPULSE PUBLIC CONTENT HUB

Every eligible tenant/business should be able to publish approved content to the DigitalPulse public Content Hub.

Example:

```text
https://digitalpulse.app/business/{business-slug}/blog
```

Article:

```text
https://digitalpulse.app/business/{business-slug}/blog/{article-slug}
```

Requirements:

- SEO-friendly URL;
- canonical URL;
- metadata;
- Open Graph metadata;
- structured data where appropriate;
- responsive design;
- fast loading;
- accessible content;
- featured image;
- author information where configured;
- publication date;
- category/tag navigation;
- related content;
- related services;
- related projects where approved;
- CTA;
- tenant/business isolation;
- draft content must never be publicly visible;
- unpublished/revoked content must not remain accidentally accessible.

DigitalPulse should also have its own public Content Hub for product/company content.

---

# 10. PLATFORM-SPECIFIC CONTENT VARIANTS

A single article may produce multiple variants.

Examples:

### Google Business Profile
Short local/business-focused update with CTA and article URL where supported.

### LinkedIn
Professional insight-oriented post.

### Facebook
Readable customer-oriented post.

### Instagram
Caption plus optional carousel/Reel structure.

### YouTube
Title, description, tags/metadata, and script where applicable.

### WhatsApp
Short message suitable for opted-in recipients and approved messaging rules.

Every variant must retain a relationship to the canonical ContentItem.

Example:

```text
Canonical Article
       |
       +-- Google Variant
       +-- LinkedIn Variant
       +-- Facebook Variant
       +-- Instagram Variant
       +-- YouTube Variant
       +-- WhatsApp Variant
```

---

# 11. AI CONTENT GENERATION RULES

AI must use:

- Graphify;
- Business Digital Identity;
- verified BusinessFacts;
- Services;
- Projects;
- approved Media;
- BusinessLocation;
- ContentItem;
- Content category;
- target platform;
- provider capability;
- brand voice;
- restricted claims;
- approved claims.

Rules:

1. No evidence = no factual claim.
2. Conflicting evidence = review required.
3. Restricted fact = never publish.
4. Low confidence = approval/assisted mode.
5. Verified + low-risk + policy allowed = eligible for automation according to tenant policy.
6. AI must never invent business facts.
7. AI must not treat crawled web content as trusted business truth.
8. External content must be treated as untrusted input and protected against prompt injection.
9. Platform-specific generation must follow the destination's content constraints.
10. Human approval must remain available.

---

# 12. APPROVAL WORKFLOW

Publishing must support:

```text
DRAFT
→ READY
→ APPROVAL_REQUIRED
→ APPROVED
→ SCHEDULED
→ PUBLISHING
→ PUBLISHED
```

Failure states:

```text
FAILED
VERIFICATION_FAILED
CANCELLED
```

Approval requirements should depend on:

- platform;
- action risk;
- tenant policy;
- automation policy;
- content confidence;
- provider capability;
- user permissions.

High-risk or externally visible actions should require explicit approval unless the tenant has intentionally enabled an allowed automation policy.

Approval must be auditable.

Store:

- approver;
- timestamp;
- decision;
- reason/comment;
- content/version approved;
- platform;
- target location;
- relevant action.

---

# 13. PUBLISH EVERYWHERE EXPERIENCE

Create a high-quality distribution experience.

Example:

```text
┌─────────────────────────────────────────────┐
│ Publish Article                             │
├─────────────────────────────────────────────┤
│ Canonical Article                           │
│ Complete Conference Room AV Guide           │
│                                             │
│ DESTINATIONS                                │
│                                             │
│ ● Google Business Profile       3 locations │
│ ● Website                        Connected   │
│ ● DigitalPulse Content Hub       Ready      │
│ ○ LinkedIn                       Connected   │
│ ○ Facebook                       Connected   │
│ ○ Instagram                     Connected   │
│ ○ YouTube                        Connected   │
│ ○ WhatsApp                       Not ready   │
│                                             │
│ [ Generate Variants ]                       │
│ [ Preview All ]                             │
│ [ Request Approval ]                        │
│ [ Schedule ]                                │
└─────────────────────────────────────────────┘
```

Google should have stronger visual prominence than generic social channels.

Possible presentation:

- “Google Business Profile — Recommended”
- “Publish to 3 locations”
- “Generate Google Post”
- “Preview on Google”
- “Google publication status”

Do not make unsupported capabilities appear enabled.

---

# 14. CONTENT DISTRIBUTION DASHBOARD

Create a distribution dashboard for every ContentItem.

Show:

- canonical content;
- publication status;
- platform;
- location;
- connection;
- variant;
- approval state;
- scheduled time;
- publication time;
- external ID;
- verification state;
- error;
- retry option where safe;
- audit history.

Example:

```text
Article
────────────────────────────────────────────

Google Business Profile
✓ Pune Location       Published
✓ Mumbai Location     Published
⚠ Nashik Location     Failed

Website
✓ Published

DigitalPulse
✓ Published

LinkedIn
✓ Published

Facebook
Scheduled

Instagram
Approval Required

YouTube
Not Connected
```

---

# 15. CONTENT CALENDAR

Provide a calendar for scheduled content.

Calendar should support:

- day/week/month views;
- platform filtering;
- business filtering;
- location filtering;
- status filtering;
- drag/drop rescheduling only where safe;
- approval status;
- scheduled time;
- publication result;
- retry/cancel where supported.

Do not allow rescheduling an already published item as if it were still scheduled.

---

# 16. ACTION EXECUTION

Use the existing DigitalPulse action architecture.

Publishing should create an Action with:

- TenantId;
- BusinessId;
- ContentItemId;
- ContentVariantId;
- Platform;
- PlatformConnectionId;
- BusinessLocationId where applicable;
- operation;
- idempotency key;
- requested by;
- approval reference;
- status.

ActionAttempt should record:

- attempt number;
- start/end;
- provider request metadata safe for logging;
- normalized provider response;
- failure code;
- retryability;
- correlation ID.

Never log secrets or access tokens.

---

# 17. IDEMPOTENCY

Publishing must be idempotent.

Do not create duplicate posts because:

- the browser was refreshed;
- a worker retried;
- a network timeout occurred;
- the provider response arrived late;
- a user clicked Publish twice.

Use a deterministic idempotency key based on appropriate identifiers such as:

```text
TenantId
BusinessId
ContentItemId
ContentVariantId
Platform
Connection
Location
ContentVersion
Operation
```

The exact key design should be appropriate to the provider.

---

# 18. VERIFICATION

Verification must happen after publishing.

Do not treat:

```text
HTTP 200
```

as automatically equivalent to:

```text
Published
```

Verification should confirm the provider result through the supported API/read capability where available.

Store:

- verification status;
- checked timestamp;
- provider object/post ID;
- normalized provider status;
- failure reason;
- provider URL where available;
- verification attempts.

Example:

```text
Publish request
      ↓
Provider response
      ↓
External ID obtained
      ↓
Verification
      ↓
Confirmed Published
```

If verification is unavailable:

- clearly show the limitation;
- use the provider response only to the extent officially justified;
- do not fabricate verification.

---

# 19. FAILURE HANDLING

Normalize provider failures.

Examples:

- authentication failure;
- expired connection;
- permission denied;
- unsupported operation;
- invalid content;
- invalid location;
- rate limit;
- duplicate;
- provider unavailable;
- network timeout;
- validation error.

For each failure show:

- user-friendly explanation;
- technical category;
- whether retry is safe;
- suggested action;
- provider reference when available.

Do not expose sensitive provider payloads.

---

# 20. RETRY STRATEGY

Retry only when the failure is safely retryable.

Examples:

Retry candidates:

- transient network error;
- provider temporary outage;
- rate limit according to provider guidance.

Do not blindly retry:

- invalid content;
- permission denied;
- invalid location;
- unsupported operation;
- failed business validation.

Retries must remain idempotent.

---

# 21. AUTOMATION / AUTOPILOT

Support tenant-configurable automation.

Example policy:

```text
If:
    Content is approved
    AND platform is connected
    AND platform supports publishing
    AND confidence >= configured threshold
    AND content contains no restricted claims
    AND action is low risk
    AND entitlement allows action

Then:
    publish automatically
    verify
    record result
```

Google Business Profile autopilot must be separately configurable because it is an externally visible business action.

Provide:

- enabled/disabled;
- approval requirement;
- allowed platforms;
- allowed content types;
- allowed locations;
- scheduling window;
- rate limits;
- maximum automated actions;
- emergency disable switch.

---

# 22. ENTITLEMENTS

Publishing must respect DigitalPulse subscription entitlements.

Use configured entitlement values, not hard-coded plan names.

Potential checks:

- MAX_CONNECTIONS;
- ACTIONS_PER_MONTH;
- AI_GENERATIONS_PER_MONTH;
- MAX_BUSINESSES;
- MAX_LOCATIONS;
- MONITORING_FREQUENCY_HOURS;
- STORAGE_GB;
- WHITE_LABEL.

If an action is blocked because of entitlement:

- explain why;
- do not partially execute;
- show the relevant upgrade/configuration path.

---

# 23. SECURITY

Implement:

- server-side authorization;
- tenant isolation;
- business isolation;
- location isolation;
- role/permission checks;
- OAuth/API credential protection;
- Azure Key Vault for secrets;
- encrypted sensitive connection data;
- secure webhook validation;
- replay protection where applicable;
- rate limiting;
- SSRF protection for URLs;
- safe HTML/content sanitization;
- secure media validation;
- audit logging;
- CSP/security headers;
- dependency/container scanning.

Never trust:

- browser-supplied TenantId;
- browser-supplied BusinessId;
- browser-supplied provider capability;
- browser-supplied approval;
- browser-supplied publication status.

All critical authorization and state transitions must be server-side.

---

# 24. DATABASE REQUIREMENTS

Use SQL Server/Azure SQL and EF Core migrations.

Maintain relational integrity.

Required relationships must have:

- foreign keys;
- unique constraints where appropriate;
- check constraints where appropriate;
- tenant/business scoping;
- indexes for common queries.

Potential indexes:

- ContentDistribution(ContentItemId)
- ContentDistribution(Platform, Status)
- ContentDistribution(BusinessId, Status)
- ContentDistribution(ScheduledAt)
- ContentDistribution(ExternalId)
- Action(ContentItemId)
- Action(Status)
- Verification(ActionId)
- ContentVariant(ContentItemId, Platform)

Do not use arrays or CSV fields for relational data.

Use JSON only where provider-specific payloads or genuinely flexible configuration require it.

All timestamps are UTC.

Money uses `decimal(19,4)` where applicable.

URLs use an appropriate `nvarchar(2048)`-sized field.

---

# 25. API / VERTICAL SLICE DESIGN

Implement feature-oriented vertical slices.

Suggested features:

```text
Content/Publishing
Content/Distribution
Content/Distribution/Preview
Content/Distribution/GenerateVariants
Content/Distribution/Approve
Content/Distribution/Schedule
Content/Distribution/Publish
Content/Distribution/Verify
Content/Distribution/Retry
Content/Distribution/Cancel
Content/Distribution/Calendar
Content/Distribution/History
```

Use:

- request;
- validator;
- handler;
- response;
- authorization;
- domain behavior;
- persistence;
- integration boundary.

Follow existing DigitalPulse API conventions.

---

# 26. PROVIDER ADAPTERS

Every external platform must use the provider abstraction.

The adapter should expose appropriate capabilities such as:

```text
Connect
HealthCheck
GetCapabilities
Discover
Read
Generate
Create
Update
Delete
Publish
Metrics
Verify
```

Only expose operations actually supported by the provider.

Capability matrix example:

```text
Provider                 Create  Update  Publish  Verify
---------------------------------------------------------
DigitalPulse Hub           Yes      Yes      Yes      Yes
Website/CMS                 Yes      Yes      Yes      Yes*
Google Business Profile    Yes*      —       Yes*     Yes*
LinkedIn                    Yes*      —       Yes*     Yes*
Facebook                    Yes*      —       Yes*     Yes*
Instagram                   Yes*      —       Yes*     Yes*
YouTube                     Yes*      —       Yes*     Yes*
WhatsApp                    Yes*      —       Yes*     Yes*
```

`*` means capability must be determined from the actual official API/account/connection and must not be assumed.

---

# 27. GOOGLE-SPECIFIC CAPABILITY HANDLING

Do not hard-code:

```text
Google = always publishable
```

Instead:

```text
Provider
    ↓
ProviderCapability
    ↓
PlatformConnection
    ↓
Capability check
    ↓
Available / Unsupported / Requires Configuration
```

The UI must respond to the actual capability state.

Possible states:

- Connected
- Healthy
- Publish Supported
- Publish Not Supported
- Requires Reauthorization
- Requires Configuration
- Assisted Only
- Manual Only

---

# 28. ANALYTICS

Track distribution metrics where officially available.

Possible metrics:

- publication count;
- successful publications;
- failed publications;
- verification success;
- clicks;
- impressions;
- engagement;
- CTA interactions;
- content performance;
- platform performance.

Do not invent metrics.

If a provider does not expose a metric, show:

> Not available from this platform.

---

# 29. AUDIT TRAIL

Record important events:

- variant generated;
- approval requested;
- approval granted;
- approval rejected;
- scheduled;
- publish requested;
- publish started;
- publish succeeded;
- publish failed;
- verification succeeded;
- verification failed;
- retry;
- cancellation;
- connection changed;
- automation policy changed.

Audit entries should identify:

- TenantId;
- BusinessId;
- user/system actor;
- event type;
- entity;
- entity ID;
- timestamp;
- correlation ID;
- safe metadata.

Never store credentials or secrets in audit records.

---

# 30. FRONTEND DESIGN

The publishing experience must match the DigitalPulse design system.

Do not create a generic CRUD admin screen.

Use:

- React + TypeScript;
- Fluent UI as component foundation;
- Tailwind CSS for layout/tokens;
- TanStack Query;
- Zustand only where state truly needs it.

The experience should feel:

- premium;
- editorial;
- visual;
- intelligent;
- trustworthy;
- fast;
- polished;
- animated but restrained.

The Google Business Profile publishing workflow should be visually prominent.

Recommended UI elements:

- content preview;
- platform cards;
- Google location selector;
- platform capability indicators;
- variant preview;
- approval drawer;
- publish timeline;
- status indicators;
- distribution graph;
- calendar;
- verification state;
- failure/retry panel.

---

# 31. ANIMATION

Use the centralized DigitalPulse motion system.

Include:

- page-load reveal;
- staggered content;
- platform card entrance;
- variant generation animation;
- approval transition;
- publishing progress;
- success transition;
- failure transition;
- timeline reveal;
- calendar transitions;
- hover micro-interactions.

Use:

- transform;
- opacity;
- GPU-friendly animation.

Respect:

```text
prefers-reduced-motion
```

Do not create distracting animations.

---

# 32. GRAPHICAL INFORMATION DESIGN

The distribution UI should visually communicate:

```text
Content
   ↓
Variants
   ↓
Approvals
   ↓
Platforms
   ↓
Locations
   ↓
Publication
   ↓
Verification
```

Consider:

- distribution flow;
- platform status graph;
- publication timeline;
- location matrix;
- success/failure visualization;
- content-to-platform relationship.

Avoid decorative graphics that do not convey information.

---

# 33. GRAPHIFY WORKFLOW

Before generating platform variants:

```text
Graphify Retrieve
    ↓
Business
BusinessLocation
ContentItem
BusinessFacts
Services
Projects
Media
BrandVoice
ApprovedClaims
RestrictedClaims
PlatformCapabilities
Permissions
    ↓
AI Generation
```

After generation:

```text
AI Output
    ↓
Graphify Update
    ↓
Record:
- generated variant
- claims used
- evidence references
- confidence
- decisions
- platform constraints
- approval requirement
```

Before publishing, Graphify should help establish the relevant context, but SQL/domain state remains the source of truth for transactional state.

---

# 34. CODEGRAPH WORKFLOW

Before implementation:

```text
CodeGraph
    ↓
Find:
- ContentItem
- ContentVariant
- PlatformConnection
- Action
- ActionAttempt
- ApprovalRequest
- ApprovalDecision
- Verification
- Provider adapters
- Provider capability model
- Existing Content Hub
- Existing UI routes
- Existing tests
```

Then implement only the necessary changes.

After changes:

```text
CodeGraph Impact Analysis
    ↓
Run targeted tests
    ↓
Run full relevant test suite
    ↓
Run security checks
```

Do not modify unrelated modules unnecessarily.

---

# 35. TESTING REQUIREMENTS

Every layer must be tested.

## Unit tests

Test:

- content validation;
- platform variant rules;
- approval rules;
- capability checks;
- location authorization;
- entitlement checks;
- idempotency;
- retry classification;
- status transitions.

## Integration tests

Test:

- database persistence;
- tenant isolation;
- business isolation;
- location isolation;
- action lifecycle;
- approval lifecycle;
- verification lifecycle.

## Provider contract tests

Test adapter behavior against supported provider contracts.

Use safe test/sandbox environments where available.

Do not replace real provider integration with fake success logic.

## API tests

Test:

- authentication;
- authorization;
- validation;
- publishing;
- verification;
- retry;
- cancellation;
- scheduling.

## Frontend tests

Test:

- destination selection;
- Google location selection;
- variant preview;
- approval;
- scheduling;
- publish state;
- failures;
- retry;
- tenant isolation in UI state.

## E2E tests

At minimum:

```text
Create Content
→ Generate Google Variant
→ Preview
→ Approve
→ Publish
→ Verify
→ Show Published
```

Also test:

```text
Publish to Website
Publish to DigitalPulse Hub
Multi-location Google publication
Provider failure
Retry
Entitlement blocked
Unauthorized user
Unsupported provider capability
```

---

# 36. OBSERVABILITY

Use:

- OpenTelemetry;
- Application Insights;
- correlation IDs;
- structured logs;
- metrics;
- distributed tracing.

Track:

- generation duration;
- publishing duration;
- provider latency;
- publication success rate;
- verification success rate;
- provider failures;
- retry count;
- queue latency;
- scheduled publication delays.

Never log:

- access tokens;
- refresh tokens;
- API keys;
- passwords;
- private business secrets.

---

# 37. BACKGROUND PROCESSING

Use durable background processing for:

- scheduled publishing;
- retry;
- verification;
- analytics collection;
- monitoring.

Use Azure Service Bus / Worker / Functions according to the existing DigitalPulse architecture.

Workers must be:

- idempotent;
- retry-safe;
- cancellation-aware;
- tenant-aware;
- observable.

Do not perform long-running provider workflows directly inside a normal HTTP request if the architecture requires durable background execution.

---

# 38. USER EXPERIENCE FOR GOOGLE

Create a clear Google-focused workflow.

Example:

```text
CONTENT HUB
    ↓
Select Article
    ↓
Distribute
    ↓
┌──────────────────────────────────────────┐
│ GOOGLE BUSINESS PROFILE                  │
│                                          │
│ Turn this article into a Google update  │
│                                          │
│ Locations                                │
│ ☑ Pune                                   │
│ ☑ Mumbai                                 │
│ ☐ Nashik                                 │
│                                          │
│ AI Optimization                          │
│ ✓ Business facts verified                │
│ ✓ CTA generated                          │
│ ✓ Location matched                       │
│ ✓ Destination URL validated              │
│                                          │
│ [ Preview Google Post ]                  │
│ [ Request Approval ]                     │
└──────────────────────────────────────────┘
```

After publishing:

```text
GOOGLE BUSINESS PROFILE

✓ Pune       Published
✓ Mumbai     Published
⚠ Nashik     Failed

[View Details] [Retry Failed]
```

---

# 39. ASSISTED / MANUAL MODE

When automation is unsupported:

```text
Assisted Publishing
```

should provide:

- final generated content;
- media;
- destination URL;
- location;
- platform instructions;
- copy button;
- open provider button where appropriate;
- completion checklist.

Status should remain:

```text
ASSISTED
```

or another explicit non-automated state.

Never change it to:

```text
PUBLISHED
```

without provider confirmation.

---

# 40. CONTENT → DISTRIBUTION → MONITORING LOOP

The feature must integrate with the broader DigitalPulse operating system:

```text
Digital Pulse Check
        ↓
Finding
        ↓
Content Opportunity
        ↓
Content Generation
        ↓
Approval
        ↓
Distribution
        ↓
Google / Website / Social
        ↓
Verification
        ↓
Monitoring
        ↓
Insights
        ↓
Next Content Opportunity
```

This should become a continuous improvement loop rather than a one-time publishing tool.

---

# 41. IMPLEMENTATION ORDER

**Overall: 60% (6/10 phases).**

Implement in this order:

## Phase A — completed (100%)
Audit Existing Implementation

Inspected 2026-09-26 against the live tree (CodeGraph). Content Hub already owns `ContentItem`, variants, calendar, HUB publish, and capability-honest `ContentDistribution`. Social compose + official adapters own short posts. Website adapter is Assisted (no invented CMS write). Google live write exists only when `ExternalAccount` is a `locations/` path. Actions already have idempotency, approval, retry, and verification. Do not duplicate those lifecycles.

### Requirement-to-code matrix

| Requirement | Status | File/Class | Action | Test |
|---|---|---|---|---|
| Canonical ContentItem | Existing | `ContentItem`, hub CRUD | Keep as source | Content Hub Phase 2 |
| Platform variants | Existing | `CreateHubVariantsHandler` | Keep distinct packs | Phase 9 |
| Hub publication | Existing | `PublishHubContentHandler`, public hub | Keep | Phase 12 |
| Website publication | Existing | `WEBSITE` channel + Assisted adapter | CMS write only if official adapter confirms | — |
| Assisted/Manual fallback | Existing | `DistributeHubContentHandler.Hold` | Keep honest hold | Phase 11 |
| GBP first-class | Existing | Distribution desk + location rows | Keep | — |
| Google capability real | Existing | Adapter returns null without `locations/` | Do not hard-code success | LivePlatformPathTests |
| Google location selection | Existing | `ResolveLocationsAsync` | UI selection | Phase F |
| Google-specific generation | Existing | `GenerateSocialDraftHandler` + social agent | Keep through orchestrator | AiPhase9 |
| Google preview | Existing | Distribution desk Google pack | Keep | — |
| Google approval | Existing | Social approve + hub submit/approve | Reuse | Phase 10 |
| Google official publish | Existing | `GoogleAdapter.LivePublish` | Call only with live location path; never fake Published | Phase D |
| Google verification | Partial | `VerificationStatus` on distribution | Official confirm before Verified | Phase C/D |
| Multi-location | Existing | one `ContentDistribution` per location | Dedicated GBP preview | Phase F |
| LinkedIn/FB/IG/YT capability | Existing | catalog + `PublishAsync` | Keep | — |
| WhatsApp consent | Existing | WhatsApp Cloud API + opt-in | Keep out of generic social post | WhatsApp handlers |
| Publish Everywhere | Existing | `PublishEverywhereHubContentHandler` | UI button | Phase F |
| Scheduling / calendar | Existing | `ContentCalendarEntry`, ContentPage | Worker release due rows | Phase G |
| Idempotency | Existing | `BuildKey` + existing-row reuse | Keep | — |
| Retry / cancel | Existing | Retry/Cancel/Verify handlers | Worker retry | Phase G |
| Failure normalization | Existing | `FailureReason` + Hold | Keep | Phase 11 |
| Approval/audit | Existing | ApprovalRequest + OperationsAudit | Keep | Phase 16 |
| Entitlement | Existing | plan + monthly distribution count | Keep | — |
| Tenant/business isolation | Existing | query filters + BusinessAccess | Add location isolation | Phase I |
| Secrets | Existing | connection grants, empty appsettings | Keep | — |
| AI + Graphify | Existing | orchestrator + AttachContent | Reuse | AI Phases |
| Observability | Existing | `OperationsAudit` on distribute/retry/cancel/verify | Keep | — |
| Worker jobs | Missing | Release is on-demand API | Background due + retry | Phase G |
| Docs | Missing | — | `docs/publishing/` | Phase J |

### Reuse — do not duplicate

`ContentItem`, `ContentVariant`, `ContentDistribution`, `ContentCalendarEntry`, `ApprovalRequest`, `WorkAction`, `IPlatformAdapter`, `IAiOrchestrator`, `BusinessLocation`, Content Hub UI panes.

## Phase B — completed (100%)
Database

Implement missing relational entities, fields, constraints, indexes, and migrations. `ContentDistribution` now stores location, idempotency, attempts, and verification.

## Phase C — completed (100%)
Domain/Application

Implemented:

- location-aware `ContentDistributionEngine` (one Google row per owned location);
- Publish Everywhere fan-out (HUB/WEBSITE/GOOGLE/LinkedIn/Facebook/Instagram/YouTube; WhatsApp stays consent-only);
- idempotent place, retry, cancel, and verify-without-inventing-an-ID;
- plan entitlement before fan-out;
- `OperationsAudit` on distribute/retry/cancel/verify.

## Phase D — completed (100%)
Provider Adapters

Distribution now consults `IPlatformAdapterCatalog`. Official `PublishAsync` runs only when the adapter can publish and a live grant exists. Published is recorded only when the adapter returns `Published`.

Prioritize:

Prioritize:

1. DigitalPulse Content Hub
2. Website/CMS
3. Google Business Profile
4. LinkedIn
5. Facebook
6. Instagram
7. YouTube
8. WhatsApp

Do not claim unsupported provider operations.

## Phase E — completed (100%)
AI

`CreateHubVariantsHandler` now retrieves Graphify context through `IAiContextBuilder` and may replace the Google pack with a live orchestrator draft. Restricted output is discarded. Variants stay shorter than the canonical article. Graphify is updated via `AttachContentAsync`.

## Phase F — completed (100%)
Frontend

Distribution desk is Google-first: location checkboxes, GBP preview, Publish Everywhere, retry/cancel/verify, and honest holds.

Implement:

- publish flow;
- Google-first experience;
- multi-location selection;
- previews;
- approvals;
- scheduling;
- distribution dashboard;
- calendar;
- verification;
- failures/retry.

## Phase G — Background Jobs

Implement scheduling, retry, verification, and monitoring.

## Phase H — Testing

Run all applicable test categories.

## Phase I — Security Review

Perform tenant isolation, authorization, secret handling, SSRF, webhook, rate-limit, content-security, and dependency reviews.

## Phase J — Final Completeness Audit

Do not declare complete until every requirement is:

```text
IMPLEMENTED
TESTED
VERIFIED
```

---

# 42. ACCEPTANCE CRITERIA

The feature is complete only when all are true:

- [x] Canonical ContentItem exists and remains the source content.
- [x] Platform-specific variants exist.
- [x] DigitalPulse Content Hub publication works.
- [ ] Customer website publication works where an official provider is connected.
- [x] Assisted/Manual fallback works where automatic website publication is unsupported.
- [x] Google Business Profile is treated as a first-class publishing destination.
- [x] Google capability detection is real and not hard-coded.
- [x] Google location selection works.
- [x] Google-specific post generation works.
- [x] Google preview works.
- [x] Google approval works.
- [x] Google publication uses the official supported provider API/capability.
- [x] Google publication is not falsely marked successful.
- [x] Google verification is implemented where supported.
- [x] Multi-location publication tracks each location independently.
- [x] LinkedIn distribution is capability-driven.
- [x] Facebook distribution is capability-driven.
- [x] Instagram distribution is capability-driven.
- [x] YouTube distribution is capability-driven.
- [x] WhatsApp distribution is capability-driven and consent-aware.
- [x] Publish Everywhere workflow works.
- [x] Scheduling works.
- [x] Calendar works.
- [x] Idempotency works.
- [x] Retry handling works.
- [x] Failure normalization works.
- [x] Approval/audit trail works.
- [x] Entitlement checks work.
- [x] Tenant isolation works.
- [x] Business isolation works.
- [x] Location isolation works.
- [x] Role/permission checks work.
- [x] Provider secrets are protected.
- [x] AI uses verified business facts.
- [x] Graphify is integrated.
- [x] CodeGraph is used for implementation/context reduction.
- [x] Verification is persisted.
- [x] Observability is implemented.
- [ ] Automated tests pass.
- [x] No TODO placeholders remain.
- [x] No fake integrations remain.
- [x] No known critical/high defects remain.
- [ ] Documentation is updated.

---

# 43. FINAL E2E SCENARIO

Use this as the final demonstration.

Business:

**AV Professionals**

Article:

**Complete Conference Room AV Guide for Modern Businesses**

Flow:

```text
1. Create Article
2. Generate SEO/AEO metadata
3. Approve Article
4. Open Distribution
5. Select:
   - DigitalPulse Content Hub
   - Customer Website
   - Google Business Profile
   - LinkedIn
   - Facebook
   - Instagram
   - YouTube
6. Generate platform-specific variants
7. Preview all variants
8. Select Google locations
9. Request/obtain required approval
10. Publish
11. Verify each provider result
12. Show distribution status
13. Record external IDs
14. Record audit events
15. Monitor publication
16. Display analytics where available
17. Feed results back into DigitalPulse Insights
```

Expected result:

```text
DigitalPulse Content Hub       ✓ Published
Customer Website               ✓ Published
Google Pune                    ✓ Published
Google Mumbai                  ✓ Published
Google Nashik                  ✓ Published
LinkedIn                       ✓ Published
Facebook                       ✓ Published
Instagram                      ✓ Published
YouTube                        ✓ Published
```

If any provider is unsupported or fails, the UI must show the actual state rather than pretending everything succeeded.

---

# 44. FINAL INSTRUCTION TO CURSOR / CLAUDE OPUS 5.5

Do not merely scaffold this feature.

Do not create screenshots-only UI.

Do not create mock provider responses and call the feature complete.

Do not implement only the database.

Do not implement only the API.

Do not implement only Google UI.

Implement the complete production workflow:

**Content → Variant → Capability → Approval → Schedule → Publish → Provider Confirmation → Verify → Monitor → Audit**

Prioritize **Google Business Profile publishing** as a major DigitalPulse differentiator, while keeping the architecture provider-neutral and extensible.

Use existing DigitalPulse architecture, database, security, approval, action, verification, Graphify, CodeGraph, Content Hub, and design-system capabilities wherever possible.

Before coding, produce a requirement-to-code matrix.

After coding, run the complete implementation audit.

Only report **COMPLETE** when every acceptance criterion is implemented, tested, and verified.
