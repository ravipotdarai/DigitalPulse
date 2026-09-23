# DigitalPulse — Architecture & Development Master Specification v2.0

**Product:** DigitalPulse  
**Version:** 2.0  
**Status:** Master Development Specification

## 1. Product Vision

DigitalPulse is an **AI Digital Presence Operating System** for businesses.

The platform connects to authorized digital platforms, understands the business, detects digital-presence issues, generates improvements, obtains approval where required, executes supported actions, verifies results, and continuously monitors the online presence.

**Core loop:**

> Connect → Understand → Detect → Create → Approve → Execute → Verify → Monitor

**Positioning:**

> DigitalPulse — Connect your business. Let AI manage its digital presence.

## 2. Core Technology Stack

### Frontend
- React
- TypeScript
- Fluent UI
- Tailwind CSS
- TanStack Query
- Zustand where required
- One responsive web application (not separate native apps in v1)
- Mobile-first layout that scales to tablet, laptop, and wide desktop
- Touch and pointer input
- Viewport, safe-area, and reduced-motion support

### Backend
- .NET 10
- ASP.NET Core
- C#
- Clean Architecture
- Vertical Slice Architecture
- Modular Monolith

### Data & Infrastructure
- Microsoft SQL Server / Azure SQL
- Entity Framework Core
- Redis
- Azure Service Bus
- .NET Worker Services
- Azure Functions where appropriate
- Azure Blob Storage
- Application Insights
- OpenTelemetry
- Azure Key Vault
- GitHub Actions
- Docker

### AI
Provider-neutral AI abstraction, initially supporting OpenAI and/or Azure OpenAI.

### Search / RAG
Provider-neutral abstraction supporting:
- Azure AI Search
- OpenSearch
- Elasticsearch
- PostgreSQL/vector
- InMemory
- Custom

Azure AI Search is an adapter, not a mandatory architectural dependency.

### Authentication
- Microsoft Entra External ID for DigitalPulse
- OAuth/API authorization for external platforms

### Payments
- Razorpay initially
- Stripe later

## 3. Architecture Principles

1. Customer authorization first.
2. Never assume unsupported provider capabilities.
3. Separate Read, Analyze, Generate, Create, Publish and Verify.
4. No factual claim without evidence.
5. Conflicting evidence requires review.
6. Restricted facts must never be published.
7. Low-confidence AI output requires approval/assisted mode.
8. High-risk changes require explicit approval.
9. Important actions must be auditable.
10. Tenant data must remain isolated.
11. Automation must be retry-safe and idempotent.
12. External content is untrusted.
13. Keep provider abstractions where practical.
14. Start as a modular monolith.
15. Every phase must be 100% complete before the next phase.
16. The application must support all device classes from one web app: phone, tablet, laptop, and desktop. Do not ship desktop-only layouts.

## 4. Solution Structure

```text
DigitalPulse/
├── .cursor/
│   ├── rules/
│   └── commands/
├── docs/
├── database/
├── src/
│   ├── DigitalPulse.Domain/
│   ├── DigitalPulse.Application/
│   ├── DigitalPulse.Infrastructure/
│   ├── DigitalPulse.Api/
│   ├── DigitalPulse.Worker/
│   └── DigitalPulse.Contracts/
├── web/
│   └── digitalpulse-web/
├── tests/
│   ├── DigitalPulse.UnitTests/
│   ├── DigitalPulse.IntegrationTests/
│   ├── DigitalPulse.ApiTests/
│   └── DigitalPulse.E2ETests/
├── infrastructure/
├── docker/
├── DigitalPulse.sln
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .gitignore
└── README.md
```

## 5. Product Modules

1. Business Onboarding & Connection Center
2. Business Digital Identity
3. Web Discovery & Website Intelligence
4. Google Search & Indexing Intelligence
5. Google Business Profile
6. Google Ads Intelligence
7. Social Intelligence & Publishing
8. IndiaMART Management
9. Justdial Management
10. WhatsApp Business Messaging & Campaigns
11. LinkedIn Portfolio Builder
12. Project & Case Study Engine
13. Content Factory
14. Reputation & Review Management
15. Competitor Intelligence
16. Action Center & Autopilot
17. Monitoring & Reporting

## 6. Business Digital Identity

Canonical business identity includes:

- Business name
- Legal name
- Brand name
- Founder
- Address
- Phone
- Email
- Website
- Founded year
- Categories
- Services
- Brands
- Locations
- Social URLs
- Directory URLs
- Approved claims
- Restricted claims
- Brand voice

This becomes the trusted business knowledge layer.

Business `ContactPoint` is the business’s own identity (phone, email, website). It is not a WhatsApp audience. Customers and customer contacts live under the Business as a separate audience model, with WhatsApp opt-in/opt-out owned by the WhatsApp module.

## 7. Platform Integrations

Initial providers:

- Google
- Meta
- Facebook
- Instagram
- LinkedIn
- YouTube
- IndiaMART
- Justdial
- WhatsApp Business Platform (Cloud API)
- Website/CMS
- Search Console
- Google Ads

WhatsApp is a first-class DigitalPulse publishing and communication channel. It is not a special-case “post update to a registered mobile number.” The product name for this capability is **WhatsApp Business Messaging & Campaigns**.

DigitalPulse must use the official **WhatsApp Business Platform / Cloud API**. Ordinary WhatsApp automation, unofficial clients, browser scraping, or personal WhatsApp account control is out of scope and must never be implemented.

Merely storing a customer mobile number is not sufficient to send WhatsApp messages. WhatsApp policy requires that the person provided the number and opted in to receive WhatsApp messages from that business. Opt-outs must be respected.

Common adapter contract (`IPlatformAdapter`):

```text
IPlatformAdapter
       │
       ├── GoogleAdapter
       ├── MetaAdapter
       ├── LinkedInAdapter
       ├── YouTubeAdapter
       ├── IndiaMartAdapter
       ├── JustdialAdapter
       └── WhatsAppAdapter

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

Each adapter exposes capabilities such as:

```text
CanRead
CanCreate
CanUpdate
CanDelete
CanPublish
CanGetMetrics
```

`WhatsAppAdapter` extends the common contract with channel-specific operations:

```text
WhatsAppAdapter
    ├── Connect()
    ├── HealthCheck()
    ├── GetCapabilities()
    ├── GetContacts()
    ├── SendTemplate()
    ├── SendMessage()
    ├── GetMessageStatus()
    ├── ReceiveWebhook()
    └── Verify()
```

IndiaMART and Justdial must use explicit capability configuration. If an authorized official write mechanism is unavailable, the product must provide a complete Assisted/Manual workflow rather than inventing an API.

WhatsApp write operations are official Cloud API operations only. If the business is not connected, the phone number is not verified, a template is not approved, consent is missing, or the customer-service window is closed, the Action Engine must block or route to Approval/Assisted — never invent an unofficial send path.

### WhatsApp Business Messaging & Campaigns

WhatsApp fits DigitalPulse as another communication and publishing channel in the same Content Factory → Policy → Approval → Action → Verify loop used for website, Google, Meta, LinkedIn, YouTube, IndiaMART and Justdial.

Recommended shape:

```text
DigitalPulse
   │
   ├── Business
   │     ├── Customers / Contacts
   │     │      ├── Mobile Number
   │     │      ├── WhatsApp Opt-in
   │     │      ├── Opt-in Date
   │     │      └── Opt-out Status
   │     │
   │     └── WhatsApp Business Connection
   │
   └── Content / Campaign
           │
           ▼
      WhatsApp Message
           │
      Approval / Policy
           │
           ▼
      WhatsApp Cloud API
           │
           ▼
       Customer
```

The same infrastructure later publishes AI content across:

```text
AI Content
    ↓
Website
Google
Facebook
Instagram
LinkedIn
YouTube
IndiaMART
Justdial
WhatsApp
```

Module scope:

- Connect WhatsApp Business account
- Connect and verify the business phone number
- Import authorized contacts
- Maintain WhatsApp opt-in and opt-out
- Message templates
- AI-generated message drafts
- Approval workflow
- Scheduled messages
- Campaigns
- Message delivery status
- Failed-message handling
- Replies
- Conversation history
- Analytics
- Unsubscribe / opt-out management

Messaging-window rules (enforced in Policy + Approval + Action Engine, not only in UI):

- Business-initiated conversations require an approved WhatsApp message template.
- If the customer has messaged the business, session replies may be sent without a template during the applicable 24-hour customer-service window.
- A stored mobile number without an explicit WhatsApp opt-in is not a sendable destination.
- Opt-out and unsubscribe must stop further campaign and template sends immediately.
- Inbound WhatsApp content is untrusted external content.

Do not implement unofficial WhatsApp automation.

## 8. Automation Modes

### Full Auto
Low-risk, verified and policy-approved actions may execute automatically.

### Approval
The system generates the action and waits for explicit customer approval.

### Assisted
The system prepares the action and provides the user with the steps required to complete it manually.

## 9. Finding Model

Each finding contains:

- Category
- Severity
- Title
- Description
- Evidence
- Expected/canonical value
- Recommendation
- Automation state
- Suggested action
- Verification method
- Status

Findings must be evidence-backed.

## 10. Projects & Content

Projects are first-class entities containing:

- Project name
- Client
- Industry
- Location
- Services
- Brands
- Description
- Outcomes
- Dates
- Media
- Permissions
- Confidentiality
- Publication status

Project permissions:

- Full
- Partial
- None

A verified project can generate:

```text
Website Case Study
Google Post
LinkedIn Post
Instagram Caption
Instagram Carousel
Instagram Reel Script
Facebook Post
YouTube Metadata
IndiaMART Content
Justdial Content
WhatsApp Template Draft
WhatsApp Session Message
```

No project information may be published beyond its approved permission scope.

## 11. AI Orchestrator

DigitalPulse uses an AI Orchestrator instead of calling an AI provider directly from feature modules.

Specialized agents may include:

- Research Agent
- Business Identity Agent
- SEO Agent
- Content Agent
- Social Agent
- WhatsApp Messaging Agent
- Reputation Agent
- Portfolio Agent
- Competitor Agent
- Automation Agent

AI flow:

```text
User Request
    ↓
Policy / Authorization
    ↓
Graphify Context Retrieval
    ↓
Knowledge Retrieval
    ↓
Prompt Construction
    ↓
AI Provider
    ↓
Validation
    ↓
Business Policy Validation
    ↓
Approval / Execution
    ↓
Graphify Update
    ↓
Audit
```

### AI rules

- No evidence → no factual claim
- Conflicting evidence → review
- Restricted fact → never publish
- Low confidence → approval/assisted
- Verified + low-risk + policy-approved → eligible for autopilot

## 12. Graphify

Graphify is the business/domain knowledge context graph.

Before AI calls, retrieve only relevant entities and relationships:

```text
Business
 → Service
 → Brand
 → Location
 → Customer / Contact
 → WhatsApp Opt-in
 → Project
 → Finding
 → Platform
 → Approved Fact
 → Permission
```

After AI calls, update Graphify with:

- Extracted entities
- Relationships
- Decisions
- Dependencies
- Outcomes
- Verified facts
- Action results

Graphify is a context/relationship layer. It does not replace SQL, authorization, migrations, tests, audit logs or human approval.

## 13. CodeGraph

CodeGraph is used during development.

Before code work:
- Find relevant symbols
- Find dependencies
- Find callers/callees
- Find related tests
- Find impacted modules

After changes:
- Perform impact analysis
- Identify affected tests
- Run targeted tests
- Validate dependencies

## 14. Search Provider Abstraction

Required abstractions:

```text
ISearchProvider
IVectorSearchProvider
IEmbeddingProvider
IRetrievalService
IKnowledgeIndexManager
```

Configuration:

```text
Search:Provider =
    AzureAISearch
    PostgreSQL
    OpenSearch
    Elasticsearch
    InMemory
    Custom
```

All search/vector data must remain tenant-scoped.

## 15. Database Architecture

Database:
- Microsoft SQL Server
- Azure SQL

Schema:

```text
dp
```

Reference entities:
- Currency
- Country
- Provider
- ProviderCapability
- SearchProvider
- SearchProviderConfiguration
- SubscriptionPlan
- EntitlementDefinition
- SubscriptionPlanEntitlement
- Industry
- FactType
- FindingCategory
- ContentType

SaaS:
- Tenant
- AppUser
- Role
- Permission
- RolePermission
- TenantMembership
- Subscription

Business:
- Business
- BusinessLocation
- ContactPoint
- Customer
- CustomerContact
- BusinessCategory
- Service
- Brand
- BusinessBrand
- BusinessFact

Connections:
- PlatformConnection
- PlatformSnapshot
- WhatsAppConnection

WhatsApp:
- WhatsAppContact
- WhatsAppOptIn
- WhatsAppTemplate
- WhatsAppCampaign
- WhatsAppConversation
- WhatsAppMessage
- WhatsAppMessageAttempt

Scans/findings:
- Scan
- Finding
- FindingEvidence

Projects/media:
- Project
- ProjectPermission
- MediaAsset
- ProjectMedia
- ProjectService
- ProjectBrand

Content/governance:
- ContentItem
- ContentVariant
- ApprovalRequest
- ApprovalDecision
- AutomationPolicy
- AutomationRule

Execution:
- Action
- ActionAttempt
- Verification

Monitoring/reporting:
- MonitoringCheck
- MonitoringResult
- Alert
- Competitor
- CompetitorObservation
- Report

Operations:
- UsageRecord
- AuditEvent
- Notification

## 16. Database Rules

1. Use 3NF for transactional data.
2. Use bridge tables for many-to-many relationships.
3. Tenant-owned tables must include `TenantId`.
4. Use foreign keys and appropriate unique/check constraints.
5. Store timestamps in UTC.
6. Use `rowversion` where appropriate.
7. Never store secrets/tokens in plaintext.
8. Use JSON only where genuinely required.
9. Never use CSV/arrays for relational relationships.
10. Monetary values use `decimal(19,4)`.
11. URLs use `nvarchar(2048)`.
12. External provider IDs must be unique in the correct scope.
13. Audit events are append-only.
14. Seeds must be deterministic.
15. Add indexes according to query patterns.
16. Test tenant isolation.

## 17. Multi-Tenancy

Request flow:

```text
User
 ↓
Tenant
 ↓
Business
 ↓
Resource
```

Authorization is server-side.

Never trust a client-provided `TenantId`.

Every tenant-owned query must be tenant-scoped.

Tenant types: `Platform`, `Agency`, `Direct`.

Agency clients are **Businesses under one Agency tenant**, not child tenants.

```text
Platform (DigitalPulse — not a customer tenant)
│
├── Tenant: Your Company          Type = Platform
│
├── Tenant: Agency A              Type = Agency    Plan = Agency
│     ├── Business 1
│     ├── Business 2
│     └── Business 3
│
├── Tenant: Agency B              Type = Agency    Plan = Agency
│     ├── Business 1
│     └── Business 2
│
└── Tenant: Direct Customer       Type = Direct    Plan = Starter | Growth | Business
      └── Business
```

| Rule | Meaning |
|---|---|
| Isolation boundary | Tenant |
| Work object | Business (`TenantId` + `BusinessId`) |
| Agency clients | Extra Business rows on that agency tenant |
| Direct customer | One tenant, usually one business |
| `MAX_AGENCY_CLIENTS` | Max businesses on an Agency tenant |

Seed **Your Company** as `Type = Platform`. It is not self-serve registration.

Register in one transaction: `AppUser` + `Tenant` + `TenantMembership` (Owner) + `Subscription` + first `Business` (required for Direct).

Isolation tests must PASS before Connection Center:

- Agency A cannot `GET` Agency B’s business by ID
- Direct cannot list Agency businesses
- Creating a business beyond plan max fails
- Direct cannot assign plan Agency
- Forged or omitted client `TenantId` does nothing

## 18. Subscription Plans

Initial planning values:

| Plan | Monthly | Annual |
|---|---:|---:|
| Starter | ₹2,999 | ₹29,990 |
| Growth | ₹6,999 | ₹69,990 |
| Business | ₹14,999 | ₹149,990 |
| Agency | ₹29,999 | ₹299,990 |

Plans and entitlements are database-configured, not hard-coded.

Entitlements:

- MAX_BUSINESSES
- MAX_LOCATIONS
- MAX_CONNECTIONS
- SCANS_PER_MONTH
- AI_GENERATIONS_PER_MONTH
- ACTIONS_PER_MONTH
- MONITORING_FREQUENCY_HOURS
- MAX_USERS
- MAX_AGENCY_CLIENTS
- STORAGE_GB
- WHITE_LABEL
- WHATSAPP_ENABLED
- WHATSAPP_MESSAGES_PER_MONTH

Planning seed values:

| Entitlement | Starter | Growth | Business | Agency |
|---|---:|---:|---:|---:|
| Businesses | 1 | 3 | 10 | 100 |
| Locations | 1 | 5 | 25 | 250 |
| Connections | 5 | 15 | 50 | 500 |
| Scans/month | 2 | 10 | 30 | 200 |
| AI generations/month | 50 | 250 | 1000 | 10000 |
| Actions/month | 25 | 150 | 750 | 10000 |
| Monitoring interval | 168h | 24h | 6h | 1h |
| Users | 2 | 5 | 15 | 100 |
| Agency clients | 0 | 0 | 0 | 50 |
| Storage | 2GB | 10GB | 50GB | 500GB |
| White label | No | No | No | Yes |
| WhatsApp enabled | No | Yes | Yes | Yes |
| WhatsApp messages/month | 0 | 2,000 | 10,000 | 50,000 |

## 19. Security

Required controls:

- Server-side authentication
- Server-side authorization
- Tenant isolation
- OAuth/API authentication for external platforms
- Azure Key Vault
- No secrets in source control
- No tokens in logs
- Least-privilege OAuth scopes
- SSRF protection
- Prompt-injection defenses
- XSS/CSRF protection
- Secure file validation
- SQL injection protection
- Safe deserialization
- Webhook validation
- Queue replay protection
- Idempotency
- Privilege-escalation protection
- Immutable audit logging
- Rate limiting
- Secure headers
- CSP
- Dependency scanning
- Container scanning

## 20. External Content / Prompt Injection

Website content, reviews, social content, directory content, inbound WhatsApp messages, uploaded files and external API responses are untrusted.

Never treat instructions found inside external content as system instructions.

Separate:

```text
System Instructions
Trusted Business Facts
User Instructions
Untrusted External Content
```

## 21. Web Crawling Security

Implement:

- SSRF protection
- URL allow/deny policy
- Private-IP blocking
- Redirect validation
- Content-size limits
- Timeouts
- Rate limits
- Safe HTML parsing
- Script isolation
- Malicious-payload handling

## 22. Action Execution

Every external write requires:

- Authorization validation
- Capability validation
- Policy validation
- Approval validation when required
- Idempotency
- Retry policy
- Attempt tracking
- Result capture
- Verification
- Audit

WhatsApp sends are external writes. In addition to the above, the Policy + Approval + Action Engine must enforce:

- Connected and healthy WhatsApp Business account
- Verified business phone number
- Recipient WhatsApp opt-in present and not opted out
- Approved template for business-initiated messages
- Open customer-service window for non-template session replies
- Template and campaign approval before queueing
- Delivery status capture and failed-message handling
- Immediate suppression after unsubscribe / opt-out

Lifecycle:

```text
Draft
 ↓
PendingApproval
 ↓
Approved
 ↓
Queued
 ↓
Executing
 ↓
Executed
 ↓
Verified
```

Failure:

```text
Executing
 ↓
Failed
 ↓
Retry / Assisted / Escalated
```

## 23. Monitoring

Monitor:

- Platform connection health
- Website availability
- Business identity consistency
- Search visibility
- Social activity
- WhatsApp connection health, delivery failures, and opt-out volume
- Review changes
- Profile changes
- Competitor changes
- Published content
- Action failures
- API failures

Monitoring frequency is controlled by subscription entitlement and policy.

## 24. Reporting

Reports should distinguish:

```text
Observed Fact
Recommendation
AI Interpretation
Customer Decision
```

Reports may contain:

- Findings
- Improvements
- Executed actions
- Pending actions
- Failed actions
- Platform health
- Search observations
- Social metrics
- WhatsApp delivery, conversation, and campaign metrics
- Reputation metrics
- Competitor observations
- Recommendations
- Historical changes

## 25. Observability

Use:

- OpenTelemetry
- Application Insights
- Structured logging
- Correlation IDs
- Trace IDs
- Metrics
- Distributed tracing
- Health checks

Never log access tokens, refresh tokens, API secrets or unnecessary sensitive data.

## 26. Background Processing

Long-running work must use durable background processing.

Examples:

- Website scans
- Search analysis
- AI generation
- Bulk content generation
- WhatsApp campaign sends and webhook processing
- Monitoring
- Action execution
- Verification
- Reports

Jobs must be:

- Retry-safe
- Idempotent
- Observable
- Tenant-aware
- Correlated
- Auditable

## 27. Frontend Architecture

Use:

```text
React
TypeScript
Fluent UI
Tailwind CSS
TanStack Query
Zustand where required
```

Ship **one** DigitalPulse web application that runs in the browser on every device class:

```text
Phone     < 640px
Tablet    640px–1023px
Laptop    1024px–1439px
Desktop   ≥ 1440px
```

Do not build separate iOS, Android, or desktop-native clients unless a later phase explicitly adds them. A responsive web app (PWA-capable: installable, offline-safe shell) is the device strategy.

Layout rules:

- Design mobile-first; enhance for larger viewports.
- Navigation adapts: bottom bar or hamburger on phone, rail or top nav on tablet, persistent sidebar on laptop/desktop.
- Data grids become stacked cards or lists on small screens; full grids are allowed from tablet landscape up.
- Detail panels become full-screen routes on phone, split view on tablet+, docked pane on desktop.
- Dialogs, approvals, and confirmations must be completable on a phone (full-screen sheet or route, not a clipped modal).
- Tap targets at least 44px; forms usable with on-screen keyboards; no hover-only actions.
- Respect safe areas (notch, home indicator) and `prefers-reduced-motion`.
- Shared page chrome, command areas, and filters reflow; do not hide primary actions off-screen.

Every page must support:

- Loading
- Empty
- Error
- Success
- Validation
- The four viewport classes above without horizontal scroll of the app shell

Never expose provider credentials to browser code.

## 28. UI/UX

Enterprise SaaS patterns should include:

- Navigation (device-adaptive)
- Command/action areas
- Data grids and equivalent mobile lists
- Filters
- Search
- Detail panels / full-screen detail on phone
- Forms
- Timelines
- Approval dialogs or approval screens on phone
- Confirmation dialogs
- Notifications
- Status indicators
- Finding severity
- Platform health
- Action status
- Audit history

Responsive UX rules:

- Primary tasks (connect, scan, review finding, approve, verify, manage WhatsApp opt-in/campaigns) must be possible on a phone.
- Dense admin tools (large audit tables, bulk filters) may use progressive disclosure on small screens but must remain reachable.
- Touch and mouse/keyboard are both first-class. Keyboard shortcuts are optional extras, never the only path.
- Test each new screen at phone, tablet, and desktop widths before marking UI complete.

Clearly distinguish:

```text
Detected
Recommended
Draft
Pending Approval
Approved
Executing
Executed
Verified
Failed
```

## 29. API

API requirements:

- Versioning
- OpenAPI
- Typed request/response contracts
- Validation
- ProblemDetails
- Correlation IDs
- Authorization
- Tenant resolution
- Idempotency where applicable

Do not expose internal database entities directly as API contracts.

## 30. Error Handling

Normalize provider failures:

```text
Unauthorized
Forbidden
RateLimited
ValidationFailed
NotFound
Conflict
ProviderUnavailable
TransientFailure
PermanentFailure
UnsupportedCapability
```

Retry transient failures only.

## 31. Testing

Required where applicable:

- Unit tests
- Integration tests
- API tests
- Contract tests
- E2E tests
- Security tests
- Migration tests
- AI evaluation tests

CI:

```text
Restore
Build
Format / analyzers
Unit tests
Integration tests
API tests
Frontend tests
Security/dependency checks
Migration validation
Container build
Staging smoke tests
```

## 32. Definition of Done

A phase is complete only when:

- Requirements are implemented
- Database changes are complete
- Backend is complete
- Frontend is complete
- UI is verified on phone, tablet, and desktop viewports
- Integrations are complete or have a complete assisted/manual path
- Background processing is complete
- Tests pass
- Security review passes
- Tenant isolation is verified
- Observability is implemented
- Audit is implemented
- Documentation is updated
- CodeGraph impact analysis is complete
- Graphify updates are complete where applicable
- No acceptance-critical TODOs remain
- No fake integrations remain
- No disabled tests hide defects
- No critical/high defects remain

## 33. Cursor Development Workflow

For every phase:

```text
1. Read Master Specification
2. Read applicable .cursor rules
3. Read acceptance criteria
4. Inspect CodeGraph
5. Inspect Graphify
6. Confirm dependencies
7. Design database changes
8. Implement backend
9. Implement workers/integrations
10. Implement frontend
11. Implement tests
12. Run migrations
13. Run security checks
14. Run CodeGraph impact analysis
15. Update Graphify
16. Update documentation
17. Review phase
18. Test phase
19. Mark phase PASS
20. Start next phase only after PASS
```

## 34. Development Phases

### Phase 0 — Database + Foundation
- Repository
- .NET solution
- Project structure
- Database schema
- Seed data
- EF Core
- Initial migrations
- CI
- Configuration
- Observability foundation
- Cursor rules
- Test foundation

### Phase 1 — Identity + Tenancy
- Authentication
- Users
- Roles
- Permissions
- Tenant creation
- Tenant membership
- Authorization
- Tenant isolation

### Phase 2 — Business Digital Identity
- Business
- Locations
- Contacts
- Customers / customer contacts (mobile number; WhatsApp opt-in is added with the WhatsApp module)
- Categories
- Services
- Brands
- Business facts
- Approved/restricted facts
- Business profile UI

### Phase 3 — Connection Center
- Platform catalog
- OAuth connections
- Connection health
- Capabilities
- Reauthorization
- Diagnostics

### Phase 4 — DigitalPulse Check
- Scan orchestration
- Website discovery
- Platform discovery
- Identity comparison
- Findings
- Evidence
- Severity
- Recommendations
- Audit/reporting

### Phase 5 — Website + Search
- Website intelligence
- SEO
- Search integration
- Search visibility
- Search Console
- AEO-related analysis
- Search provider abstraction

### Phase 6 — Google / Meta / Social
- Google integrations
- Facebook
- Instagram
- LinkedIn
- YouTube
- Social content
- Metrics
- Publishing where supported
- Verification
- Do not treat WhatsApp as a Meta social post in this phase. WhatsApp Business Messaging & Campaigns is its own later phase.

### Phase 7 — IndiaMART / Justdial
- Provider adapters
- Capability discovery
- Authorized reads
- Official supported writes
- Assisted/manual fallback
- Verification
- Monitoring

### Phase 8 — Projects + Content
- Project management
- Project permissions
- Media
- Case studies
- Content factory
- Multi-platform variants, including WhatsApp template/session drafts
- Approval workflow

### Phase 9 — AI Orchestrator
- AI provider abstraction
- AI orchestration
- Specialized agents
- Business Knowledge Base
- Graphify context workflow
- Retrieval
- AI validation
- Confidence handling
- AI evaluation

### Phase 10 — Actions + Autopilot
- Action center
- Automation policies
- Approval
- Full Auto
- Assisted mode
- Idempotent execution
- Retry
- Verification
- Audit

### Phase 11 — WhatsApp Business Messaging & Campaigns
- WhatsApp Business Platform / Cloud API adapter (`WhatsAppAdapter`)
- WhatsApp Business account connect
- Business phone number connect/verify
- `WhatsAppConnection` health and capabilities
- Customer / authorized contact import
- WhatsApp opt-in and opt-out
- Approved message templates
- AI-generated message drafts
- Campaigns and scheduled messages
- Policy + Approval + Action Engine enforcement (consent, template, 24-hour window)
- Delivery status and failed-message handling
- Inbound replies, conversation history, and webhooks
- Unsubscribe / opt-out management
- Analytics
- No unofficial WhatsApp automation

### Phase 12 — Monitoring + Reporting
- Scheduled monitoring
- Alerts
- Historical observations
- Reports
- Dashboards
- Platform health
- Change detection

### Phase 13 — Billing + SaaS
- Subscription plans
- Entitlements
- Usage metering
- Billing integration
- Subscription lifecycle
- Plan enforcement
- Payment webhooks

### Phase 14 — Agency + White Label
- Agency tenants
- Client businesses
- Client isolation
- White-label configuration
- Agency reporting
- Agency workflows

### Phase 15 — Production Hardening
- Performance
- Security hardening
- Disaster recovery
- Backup/restore
- Scaling
- Cost controls
- Production observability
- Dependency/container scanning
- Final E2E
- Staging validation
- Production readiness

## 35. Infrastructure Strategy

Initial local environment:

```text
SQL Server
Redis
DigitalPulse API
DigitalPulse Worker
React Web
```

Docker Compose may be used locally.

Azure services can be introduced progressively:

```text
Azure SQL
Azure Blob Storage
Azure Service Bus
Azure Key Vault
Application Insights
Azure Container Apps / App Service / appropriate compute
```

Do not introduce production infrastructure prematurely.

## 36. Configuration

Support:

```text
Development
Test
Staging
Production
```

Use:

- User Secrets
- Environment variables
- Azure Key Vault
- CI/CD secret stores

Provider selection must be configurable without changing business logic.

## 37. Initial Development Prerequisites

Install:

- Cursor or VS Code
- .NET 10 SDK
- Node.js LTS
- Git
- SQL Server Developer Edition
- SQL Server Management Studio
- EF Core CLI
- Docker Desktop
- Azure CLI
- Optional PowerShell 7

Verify:

```powershell
git --version
dotnet --version
dotnet --info
dotnet ef --version
node --version
npm --version
docker --version
docker compose version
az --version
pwsh --version
```

Redis may initially run through Docker.

Do not configure all future providers before their required phase.

## 38. Data Ownership

```text
SQL Server
    = transactional source of truth

Blob Storage
    = media/object storage

Redis
    = cache

Search / Vector Provider
    = retrieval index

Graphify
    = business knowledge/context graph

CodeGraph
    = source-code dependency/context graph

AI Provider
    = reasoning/generation service

External Platforms
    = external system of record for their own platform data
```

Synchronization and verification must respect these ownership boundaries.

## 39. Reliability

Assume:

- API timeouts
- Rate limits
- Token expiration
- Provider outages
- Partial failures
- Duplicate delivery
- Webhook retries
- Network failures
- Eventual consistency

Therefore use:

- Retry policies
- Idempotency keys
- Durable queues
- Attempt tracking
- Verification
- Audit events
- Actionable errors

## 40. Cost Control

Minimize unnecessary AI and infrastructure cost through:

- Graphify context selection
- CodeGraph context selection
- Retrieval before generation
- Cached platform data
- Incremental scans
- Plan-based monitoring
- Token budgets
- Model routing
- Provider abstraction
- Usage metering

Never send the entire repository, database, or business history to every AI call.

## 41. Documentation

Maintain documentation for:

- Architecture
- Database
- API
- Integrations
- Security
- AI
- Deployment
- Operations
- Troubleshooting
- ADRs
- Phase completion

Material architecture decisions should be recorded as ADRs.

## 42. Acceptance Philosophy

A feature is not complete merely because a screen exists, an endpoint returns 200, an AI response is generated, a button works locally, or a mock provider succeeds.

Where applicable, completion requires:

```text
UI
→ API
→ Authorization
→ Domain/Application logic
→ Database
→ Background processing
→ External provider
→ Retry
→ Verification
→ Audit
→ Monitoring
→ Tests
```

## 43. Master Completion Rule

**Do not move to the next phase until the current phase is fully implemented, tested, reviewed, secured, documented, and marked PASS.**

No percentage-based completion is sufficient.

Use binary acceptance:

```text
PASS
FAIL
```

Every acceptance criterion must be individually verified.

## 44. Final Architecture

```text
                    ┌──────────────────────────┐
                    │     DigitalPulse Web     │
                    │ React + TypeScript       │
                    │ Fluent UI + Tailwind     │
                    └────────────┬─────────────┘
                                 │
                                 ▼
                    ┌──────────────────────────┐
                    │      ASP.NET Core API     │
                    │ .NET 10 / Vertical Slice │
                    └────────────┬─────────────┘
                                 │
             ┌───────────────────┼───────────────────┐
             ▼                   ▼                   ▼
      ┌─────────────┐    ┌──────────────┐    ┌──────────────┐
      │ Application │    │    Domain    │    │  Contracts   │
      └─────────────┘    └──────────────┘    └──────────────┘
             │
             ▼
      ┌───────────────────────────────────────────────────┐
      │                  Infrastructure                    │
      │ SQL / Redis / Blob / Queues / External Adapters   │
      └───────────────────────────────────────────────────┘
             │
      ┌──────┼────────┬────────────┬──────────────┐
      ▼      ▼        ▼            ▼              ▼
   SQL DB   Redis   Blob       Service Bus    External APIs
                                              │
                ┌─────────────────────────────┼──────────────┐
                ▼                             ▼              ▼
             Google                         Meta       LinkedIn
                │
                ├── YouTube
                ├── IndiaMART
                ├── Justdial
                ├── WhatsApp Cloud API
                └── Website / Search / Ads

                    ┌──────────────────────────┐
                    │      AI Orchestrator     │
                    │ Provider-neutral AI      │
                    └────────────┬─────────────┘
                                 │
                    ┌────────────┴─────────────┐
                    ▼                          ▼
                Graphify                   Retrieval
             Business Context          Search / Vector
                    │
                    ▼
              AI Validation
                    │
                    ▼
             Approval / Policy
                    │
                    ▼
                Actions
                    │
                    ▼
               Verification
                    │
                    ▼
                  Audit
```

## 45. Master Rule for Cursor

Cursor must treat this document and the repository `.cursor/rules` as the authoritative development instructions.

For every implementation request:

1. Determine the current phase.
2. Read relevant rules.
3. Inspect CodeGraph.
4. Inspect Graphify for business/AI context.
5. Identify dependencies.
6. Implement completely.
7. Test completely.
8. Perform security and tenant-isolation checks.
9. Perform CodeGraph impact analysis.
10. Update Graphify where applicable.
11. Update documentation.
12. Review acceptance criteria.
13. Mark the phase PASS only when every criterion passes.

**Never skip a requirement to save time or tokens.**

---

**End of DigitalPulse Architecture & Development Master Specification v2.0**
