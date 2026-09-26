# DigitalPulse — Content Hub & Content Intelligence

**Mode:** Cursor Agent  
**Scope:** New DigitalPulse feature — Content Hub / Blog / Content Intelligence

---

## 1. Objective

Add a complete multi-tenant **Content Hub** capability to DigitalPulse.

This is **not** a simple blog CRUD module.

The goal is to allow every Business managed by a Tenant/Agency to:

- create and manage articles
- publish business insights
- create case studies
- create guides
- create FAQs
- create news/updates
- discover content opportunities
- use AI to generate content
- optimize content for SEO/AEO
- create content variants for multiple platforms
- submit content for approval
- publish to supported platforms
- schedule content
- monitor content performance
- continuously discover what content should be created next

The feature must integrate with the existing DigitalPulse ecosystem:

**Business → Identity → Services → Projects → Media → Connections → Findings/Signals → AI → Content → Distribution → Monitoring → Analytics**

Do **not** create an isolated blog application.

---

## 2. Product Positioning

Inside DigitalPulse, call the feature:

# Content Hub

Not simply "Blog".

Suggested navigation:

```text
CONTENT

Overview
Content Hub
Ideas
Calendar
Drafts
Published
SEO Opportunities
Distribution
Analytics
```

The Content Hub should feel like an intelligent content operating system.

---

## 3. Core Content Model

A Business owns its content.

The relationship must be:

```text
Tenant
  |
  +-- Business
       |
       +-- ContentItem
```

For Agency tenants:

```text
Tenant
  |
  +-- Business A
  |     +-- Content
  |
  +-- Business B
  |     +-- Content
  |
  +-- Business C
        +-- Content
```

Never allow content from Business A to accidentally appear for Business B.

Tenant isolation and Business isolation are mandatory.

---

## 4. Content Types

Support relational `ContentType` values such as:

- ARTICLE
- CASE_STUDY
- GUIDE
- HOW_TO
- NEWS
- ANNOUNCEMENT
- FAQ
- CUSTOMER_STORY
- SERVICE_GUIDE
- COMPARISON
- LOCAL_GUIDE
- INDUSTRY_INSIGHT
- PROJECT_STORY

Do not hard-code these throughout the frontend.

Use the existing ContentType reference structure where possible.

### Example

Business:

**AV Professionals**

Content:

> How to Design a Professional Conference Room

ContentType:

`GUIDE`

Another:

> Corporate Conference Room Transformation

ContentType:

`CASE_STUDY`

Another:

> 7 Common Video Wall Installation Mistakes

ContentType:

`ARTICLE`

---

## 5. Database / Relational Database Requirements

**FIRST inspect the existing DigitalPulse database schema.**

The existing schema already contains concepts such as:

- ContentItem
- ContentVariant
- Project
- ProjectMedia
- ProjectService
- ProjectBrand
- Business
- BusinessFact
- ApprovalRequest
- ApprovalDecision
- PlatformConnection
- Action
- Verification
- Report

Reuse existing entities where appropriate.

Do **not** create duplicate concepts.

Extend the existing relational model.

Do **not** solve the content system by putting everything into one JSON column.

Transactional relationships must remain relational.

JSON may only be used for genuinely provider-specific/flexible payloads.

---

## 6. Proposed Relational Model

Evaluate and implement the following entities as required.

### ContentItem

```text
ContentItem

Id
TenantId
BusinessId
ContentTypeId
AuthorUserId
Title
Slug
Excerpt
Body
Status
Visibility
FeaturedMediaAssetId
CanonicalUrl
PublishedAt
ScheduledAt
CreatedAt
UpdatedAt
RowVersion
```

Suggested Status:

```text
DRAFT
IN_REVIEW
APPROVED
SCHEDULED
PUBLISHED
ARCHIVED
REJECTED
```

Suggested Visibility:

```text
PUBLIC
PRIVATE
```

Rules:

- BusinessId is mandatory.
- TenantId must match the Business tenant.
- Slug must be unique within the appropriate Business scope.
- PublishedAt must only exist for published content.

---

### ContentCategory

```text
ContentCategory

Id
TenantId
BusinessId
Name
Slug
Description
CreatedAt
UpdatedAt
```

Relationship:

```text
Business 1 → many ContentCategories
```

---

### ContentItemCategory

```text
ContentItemCategory

ContentItemId
ContentCategoryId
```

Use a composite primary key.

Relationship:

```text
ContentItem many ↔ many Category
```

---

### ContentTag

```text
ContentTag

Id
TenantId
BusinessId
Name
Slug
```

---

### ContentItemTag

```text
ContentItemTag

ContentItemId
ContentTagId
```

Use a composite primary key.

---

### ContentRevision

```text
ContentRevision

Id
TenantId
ContentItemId
VersionNumber
Title
Excerpt
Body
ChangeSummary
CreatedByUserId
CreatedAt
```

Purpose:

Maintain content history.

Example:

```text
Version 1
AI generated draft

Version 2
Marketing manager edited

Version 3
SEO optimization

Version 4
Approved version
```

Do not overwrite historical revisions.

---

### ContentMedia

If `MediaAsset` already exists, use it.

Create a bridge:

```text
ContentItemMedia

ContentItemId
MediaAssetId
DisplayOrder
MediaRole
```

MediaRole examples:

```text
FEATURED
BODY
GALLERY
THUMBNAIL
SOCIAL
```

---

### ContentAuthor

Do not duplicate AppUser if existing AppUser can be referenced.

If required:

```text
ContentAuthor

Id
TenantId
BusinessId
DisplayName
Bio
AvatarMediaAssetId
AuthorUserId
IsActive
```

---

### ContentApproval

Use existing `ApprovalRequest` / `ApprovalDecision` infrastructure where possible.

Do not create a completely separate approval architecture.

Workflow:

```text
ContentItem
    ↓
ApprovalRequest
    ↓
ApprovalDecision
    ↓
APPROVED
    ↓
Publish
```

---

### ContentVariant

Reuse/extend the existing `ContentVariant`.

ContentVariant represents a platform-specific derivative.

Example:

```text
Original:

How to Design a Professional Conference Room

Variants:

WEBSITE_ARTICLE
GOOGLE_POST
LINKEDIN_POST
FACEBOOK_POST
INSTAGRAM_CAPTION
INSTAGRAM_CAROUSEL
YOUTUBE_SCRIPT
WHATSAPP_MESSAGE
NEWSLETTER
```

Suggested fields where required:

```text
Id
TenantId
ContentItemId
ProviderId
ContentFormat
Title
Body
Status
ScheduledAt
PublishedAt
ExternalContentId
CreatedAt
UpdatedAt
```

Do not hard-code providers as columns.

Use Provider relationship.

---

### ContentDistribution

Create a relational distribution/execution record if the existing Action model does not already provide this capability.

```text
ContentDistribution

Id
TenantId
BusinessId
ContentItemId
ContentVariantId
ProviderId
PlatformConnectionId
Status
ScheduledAt
PublishedAt
ExternalContentId
FailureReason
CreatedAt
UpdatedAt
```

Possible statuses:

```text
DRAFT
APPROVAL_REQUIRED
APPROVED
SCHEDULED
PUBLISHING
PUBLISHED
FAILED
CANCELLED
```

---

## 7. Content SEO Model

Create relational SEO metadata.

```text
ContentSeoAnalysis

Id
TenantId
ContentItemId

FocusKeyword
SearchIntent
SeoScore
ReadabilityScore
AeoScore
MetaTitle
MetaDescription
CanonicalUrl
SlugScore
InternalLinkScore
EntityCoverageScore
LastAnalyzedAt
```

Do not store calculated SEO information permanently inside ContentItem unless justified.

SEO analysis must be refreshable.

---

## 8. Content Topic / Opportunity Model

This is an important DigitalPulse capability.

Create:

### ContentTopic

```text
ContentTopic

Id
TenantId
BusinessId
Topic
Description
SearchIntent
Status
CreatedAt
```

### ContentOpportunity

```text
ContentOpportunity

Id
TenantId
BusinessId
ContentTopicId
SourceType
RelevanceScore
OpportunityScore
CompetitionScore
CoverageScore
Priority
Reason
Status
CreatedAt
UpdatedAt
```

SourceType examples:

```text
AI
SEARCH
WEBSITE_GAP
PROJECT
SERVICE
FINDING
COMPETITOR
CUSTOMER_QUESTION
SEASONAL
MANUAL
```

Do not hard-code scores.

Use decimal/numeric fields with documented ranges.

---

## 9. Content Calendar

Create:

```text
ContentCalendarEntry

Id
TenantId
BusinessId
ContentItemId
ContentVariantId
ScheduledAt
Status
Channel
CreatedByUserId
```

The calendar must support:

- Draft
- Scheduled
- Published
- Cancelled

Do not create duplicate calendar records if an existing scheduling model can be reused.

---

## 10. Content Analytics

Create content performance storage.

Possible:

```text
ContentMetric

Id
TenantId
BusinessId
ContentItemId
ContentVariantId
ProviderId
MetricDate
Views
Clicks
Engagements
Shares
Reactions
Comments
Leads
Conversions
```

Use appropriate numeric types.

Do not create provider-specific columns such as:

```text
GoogleViews
LinkedInViews
FacebookViews
```

Instead use:

```text
ProviderId + metric records
```

Provider-specific raw responses may be stored separately as JSON only where necessary.

---

## 11. Relationships with Existing DigitalPulse Data

Content must connect with existing business knowledge.

Important relationships:

```text
Business
  ↓
Services
  ↓
Content Opportunities
```

```text
Business
  ↓
Projects
  ↓
Case Studies / Project Stories
```

```text
Business
  ↓
BusinessFacts
  ↓
AI Content Generation
```

```text
Business
  ↓
MediaAssets
  ↓
Content
```

```text
Business
  ↓
Findings
  ↓
Content Opportunities
```

```text
Business
  ↓
PlatformConnections
  ↓
Content Distribution
```

```text
Content
  ↓
Approval
  ↓
Action
  ↓
Verification
```

```text
Content
  ↓
Monitoring
  ↓
Analytics
```

---

## 12. Example — Business Content Generation

Example Business:

**AV Professionals**

Services:

- Audio Visual Solutions
- Conference Room Solutions
- Video Walls
- Digital Signage
- Corporate AV

Projects:

- Corporate Conference Room Installation
- Auditorium AV Deployment

DigitalPulse should be able to identify topics such as:

1. How to Design a Professional Conference Room
2. 7 Mistakes to Avoid When Installing a Video Wall
3. LED Video Wall vs Projector
4. Corporate AV Solutions for Hybrid Meetings
5. Conference Room Audio Problems and Solutions

These become:

`ContentOpportunity` records.

Example:

```text
Topic:
Conference Room AV Design

BusinessId:
AV Professionals

SourceType:
SERVICE

RelevanceScore:
94

OpportunityScore:
87

CoverageScore:
22

Status:
NEW
```

---

## 13. AI Content Generation

AI must generate content only from trusted Business Knowledge.

AI context should include:

- Business Identity
- Services
- Projects
- Brands
- Locations
- Verified BusinessFacts
- Approved Claims
- Restricted Claims
- Relevant Media
- Existing Content
- Relevant Findings
- Content Opportunity

Use Graphify before AI calls.

Graphify should retrieve only relevant context.

Do not send the entire database or entire repository to the AI.

### Example

Content Opportunity:

> How to Design a Professional Conference Room

Graphify retrieves:

```text
Business:
AV Professionals

Services:
Conference Room Solutions

Location:
Pune

Projects:
Corporate Conference Room Installation

Verified facts:
approved project facts

Existing articles:
related content
```

The AI generates the draft.

---

## 14. AI Safety Rules

AI must not invent business facts.

If information is not verified:

**Do not present it as fact.**

Examples of prohibited invention:

- "20 years of experience"
- "500 successful projects"
- "ISO certified"
- "serving 50 cities"

unless these are verified BusinessFacts.

If evidence conflicts:

**Flag for review.**

If confidence is low:

**Require approval.**

Restricted claims must never be published automatically.

---

## 15. Content Editor

Create a premium content editor.

It should support:

- Title
- Slug
- Excerpt
- Rich content
- Images
- Videos where supported
- Categories
- Tags
- Author
- SEO metadata
- Internal links
- Related content
- Featured image
- Preview
- Save draft
- Submit for approval
- Schedule
- Publish

Do not make the editor look like a generic admin CRUD form.

Create a modern editorial writing experience.

---

## 16. AI Content Assistant

Inside the editor provide:

- Generate outline
- Generate draft
- Improve section
- Rewrite
- Shorten
- Expand
- Change tone
- Create FAQ
- Generate meta title
- Generate meta description
- Generate social post
- Generate LinkedIn version
- Generate Google version
- Generate Instagram caption
- Create YouTube script

AI suggestions must not automatically overwrite user content.

Use:

```text
GENERATE
PREVIEW
ACCEPT
REJECT
```

workflow.

---

## 17. SEO Intelligence

While editing, show:

```text
SEO HEALTH

SEO SCORE                 87
━━━━━━━━━━━━━━━━━━━━

✓ Search intent
✓ Title optimization
✓ Meta description
✓ Heading structure
✓ Internal links
✓ Entity coverage
✓ Readability

Needs attention:

⚠ Add "conference room design"
⚠ Add FAQ section
⚠ Add internal link to Conference Room Services
```

The score is informational.

Do not claim guaranteed Google ranking.

---

## 18. AEO / AI Search Optimization

Support content structures useful for answer engines.

Examples:

- FAQ
- How-to steps
- Definitions
- Comparison tables
- Key facts
- Summary
- Entity information

Example:

```text
Question:
What should be considered when designing a conference room?

Answer:
...

Key considerations:
1. Room size
2. Display
3. Audio
4. Camera
5. Connectivity
6. Control system
```

Keep claims evidence-based.

---

## 19. Content Repurposing

One approved article should become multiple variants.

Example:

```text
SOURCE CONTENT

Complete Guide to Conference Room AV

        ↓

Website Article
Google Business Profile Post
LinkedIn Post
Facebook Post
Instagram Caption
Instagram Carousel
YouTube Script
Newsletter
WhatsApp Message
```

Relationship:

```text
ContentItem
    |
    +-- ContentVariant
    |       |
    |       +-- Google
    |       +-- LinkedIn
    |       +-- Instagram
    |       +-- Facebook
    |       +-- YouTube
    |
    +-- ContentDistribution
```

Never duplicate the original article into separate unrelated records.

---

## 20. Approval Workflow

High-risk publishing must require approval.

Example:

```text
AI generates article
        ↓
SEO analysis
        ↓
Content review
        ↓
Approval request
        ↓
Human approval
        ↓
Create distribution actions
        ↓
Publish
        ↓
Verify
        ↓
Record result
```

Integrate with existing:

- ApprovalRequest
- ApprovalDecision
- Action
- ActionAttempt
- Verification

Do not create duplicate execution infrastructure.

---

## 21. Content Distribution

Where provider APIs officially support publishing:

Use the existing Platform Adapter architecture.

Example:

```text
ContentVariant
        ↓
Provider Adapter
        ↓
Create/Publish
        ↓
External ID
        ↓
Verification
```

For unsupported providers:

Use:

```text
ASSISTED
```

or

```text
MANUAL
```

Do not invent APIs.

IndiaMART and Justdial must use the existing capability model.

---

## 22. Public Content Website

Support a public Content Hub for each Business.

Example:

```text
Business:

AV PROFESSIONALS

/blog
/blog/conference-room-av-guide
/blog/video-wall-installation
/blog/hybrid-meeting-av
```

The exact URL architecture must respect the existing website/domain architecture.

If DigitalPulse hosts the public content, support:

**Business Content Hub**

If the business has its own website:

support future publishing through the Website/CMS adapter.

Do not assume every website supports automatic publishing.

---

## 23. Public Content Page

Example:

```text
--------------------------------------------------
AV PROFESSIONALS

Insights & Resources
--------------------------------------------------

[Featured Article]

How to Design a Professional
Conference Room

5 min read

--------------------------------------------------

LATEST INSIGHTS

Article 1
Article 2
Article 3

--------------------------------------------------

CASE STUDIES

Corporate Conference Room
Transformation

--------------------------------------------------

SERVICES

Conference Room Solutions
Video Walls
Digital Signage

--------------------------------------------------

CTA

Need help with your AV project?

Contact AV Professionals
--------------------------------------------------
```

The public experience must be premium and SEO-friendly.

---

## 24. Content Analytics

Show:

- Published Articles
- Views
- Engagement
- Clicks
- Shares
- Leads
- Conversions
- Top Content
- Growing Topics
- Underperforming Content

Example:

```text
CONTENT PERFORMANCE

Conference Room Guide
Views: 4,820
Engagement: 8.2%
Leads: 17

Video Wall Guide
Views: 2,130
Engagement: 6.4%
Leads: 9
```

Use real data only.

Do not generate fake analytics.

---

## 25. Content Intelligence

DigitalPulse should continuously answer:

**"What should this business publish next?"**

Example:

```text
CONTENT OPPORTUNITIES

1.
Conference Room Design

Relevance       94
Opportunity     87
Coverage        22

Reason:
Business offers conference room solutions but has
limited supporting content.

[Generate Article]


2.
Video Wall Maintenance

Relevance       91
Opportunity     82
Coverage        15

[Generate Article]


3.
Hybrid Meeting Solutions

Relevance       88
Opportunity     79
Coverage        31

[Generate Article]
```

---

## 26. Findings → Content

DigitalPulse findings should be able to generate content opportunities.

Example:

```text
Finding:

Website has weak explanation of conference room services.

        ↓

DigitalPulse recommendation:

Create educational content explaining conference room solutions.

        ↓

Action:

[Create Content Opportunity]

        ↓

ContentOpportunity
        ↓
AI Outline
        ↓
Article
        ↓
Approval
        ↓
Publish
```

---

## 27. Project → Case Study

A Project should be able to generate a Case Study.

Example:

```text
Project:

Corporate Conference Room Installation
```

Available:

- Project description
- Project services
- Project brands
- Project media
- Project location

Action:

**Create Case Study**

AI creates:

- Title
- Challenge
- Solution
- Implementation
- Result

Only use verified project facts.

If result information is missing:

> Result information required

Do not invent results.

---

## 28. Media Integration

Use existing `MediaAsset`.

Allow:

- Featured image
- Project images
- Gallery
- Inline images
- Social images

Do not create a separate media storage system.

Use existing Azure Blob Storage architecture.

---

## 29. Agency Support

Agency tenants must be able to manage multiple Business content hubs.

Example:

```text
AGENCY

Client A
  12 published
  3 drafts
  2 approvals

Client B
  8 published
  1 approval

Client C
  19 published
  5 opportunities
```

The agency must never cross business data boundaries.

---

## 30. White Label

Respect the existing `WHITE_LABEL` entitlement.

Agency plan may optionally remove:

```text
Powered by DigitalPulse
```

from public content hubs.

Do not hard-code this behavior.

Use the existing entitlement system.

---

## 31. Search / RAG

Content must be indexed for search.

Respect the existing SearchProvider abstraction.

Possible providers:

- Azure AI Search
- OpenSearch
- Elasticsearch
- PostgreSQL Vector
- In-Memory

Do not hard-code one search engine into the domain model.

Content indexing must be tenant/business scoped.

---

## 32. Graphify

Use Graphify to represent relationships such as:

```text
Business
  → Service
  → Project
  → Brand
  → BusinessFact
  → Finding
  → ContentOpportunity
  → ContentItem
  → ContentVariant
  → Distribution
  → Result
```

Before AI generation:

retrieve relevant graph context.

After AI generation:

update graph with:

- generated content relationship
- topic
- source entities
- content type
- dependencies
- approval status
- distribution status
- verification result

Graphify is context/orchestration support.

SQL remains the transactional source of truth.

---

## 33. CodeGraph

Before implementation:

Use CodeGraph to locate:

- ContentItem
- ContentVariant
- Project
- MediaAsset
- ApprovalRequest
- Action
- Verification
- Business
- BusinessFact
- Provider
- PlatformConnection

Do not duplicate existing implementations.

Use CodeGraph to identify affected frontend components, API endpoints, handlers, EF configurations, migrations, tests and dependencies.

---

## 34. Frontend Navigation

Add:

```text
CONTENT

Overview
Content Hub
Ideas
Calendar
Drafts
Published
SEO Opportunities
Distribution
Analytics
```

Respect the existing DigitalPulse design system.

Do not create a generic CRUD interface.

The Content Hub should use:

- editorial layouts
- large typography
- content previews
- AI insight panels
- SEO visualization
- content pipeline
- calendar visualization
- performance charts
- approval status
- distribution status

---

## 35. Content Pipeline UI

Provide a visual pipeline:

```text
IDEA
 ↓
OUTLINE
 ↓
DRAFT
 ↓
REVIEW
 ↓
APPROVAL
 ↓
SCHEDULED
 ↓
PUBLISHED
 ↓
MONITORED
```

Example:

```text
Conference Room Guide

● Idea
  ↓
● AI Draft
  ↓
● SEO Review
  ↓
● Approval
  ↓
○ Scheduled
  ↓
○ Published
```

Animate status transitions subtly.

---

## 36. Security

Mandatory:

- TenantId filtering
- BusinessId authorization
- server-side authorization
- no cross-tenant queries
- no cross-business content access
- audit content changes
- audit publishing
- audit approvals
- protect public publishing endpoints
- validate uploaded media
- sanitize HTML
- prevent stored XSS
- validate URLs
- prevent SSRF
- do not expose provider credentials
- do not expose OAuth tokens

AI-generated HTML must be sanitized before rendering/publishing.

---

## 37. Database Constraints

Use:

- foreign keys
- unique constraints
- check constraints
- indexes
- rowversion/concurrency
- UTC timestamps

Important indexes should include appropriate combinations such as:

```text
TenantId + BusinessId
BusinessId + Status
BusinessId + Slug
BusinessId + PublishedAt
ContentItemId + ProviderId
BusinessId + ScheduledAt
BusinessId + OpportunityStatus
```

Do not blindly create every possible index.

Analyze query patterns first.

---

## 38. EF Core

Create proper EF Core configurations.

Create migrations.

Do not modify the database manually without migrations.

Seed reference data deterministically.

Content types should be reference data.

Do not seed tenant-specific business content.

---

## 39. API / Vertical Slice

Follow the existing Clean Architecture + Vertical Slice Architecture.

Suggested feature slices:

```text
Features/Content/List
Features/Content/Get
Features/Content/Create
Features/Content/Update
Features/Content/Delete
Features/Content/Publish
Features/Content/Schedule
Features/Content/Approve
Features/Content/Generate
Features/Content/AnalyzeSeo
Features/Content/CreateVariants
Features/Content/Opportunities
Features/Content/Calendar
Features/Content/Analytics
```

Do not create a giant `ContentService` containing every operation.

---

## 40. Testing

Create tests for:

- Content creation
- Content update
- Content deletion
- Slug uniqueness
- Tenant isolation
- Business isolation
- Category/tag relationships
- Revision creation
- Approval workflow
- AI generation
- AI safety rules
- SEO analysis
- Content variants
- Distribution
- Scheduling
- Publishing
- Verification
- Analytics
- Agency access
- White-label entitlement
- Search indexing

Critical test:

> Business A must NEVER retrieve Business B content.

Critical test:

> Tenant A must NEVER retrieve Tenant B content.

---

## 41. Example End-to-End Flow

Example:

Business:

**AV Professionals**

DigitalPulse identifies:

```text
Finding:
Website has insufficient educational content.
```

↓

Content Opportunity:

```text
How to Design a Professional Conference Room
```

↓

User clicks:

```text
GENERATE
```

↓

Graphify retrieves:

```text
Business identity
Conference room service
Relevant project
Approved facts
Existing content
```

↓

AI generates:

- Title
- Outline
- Article
- FAQ
- Meta description

↓

SEO analysis:

Score calculated from actual checks.

↓

User reviews content.

↓

Submit for approval.

↓

`ApprovalRequest`

↓

Approved.

↓

Generate variants:

- Google
- LinkedIn
- Facebook
- Instagram

↓

Schedule.

↓

Publish where supported.

↓

`Action + ActionAttempt`

↓

Verification.

↓

`ContentDistribution = PUBLISHED`

↓

Monitoring.

↓

Analytics.

↓

DigitalPulse learns:

> Topic performed well.

↓

New content opportunities are generated.

---

## 42. Do Not Implement

Do NOT:

- create a generic WordPress clone
- create an isolated blog database
- duplicate MediaAsset
- duplicate ApprovalRequest
- duplicate Action
- duplicate Provider
- hard-code providers
- hard-code plans
- invent publishing APIs
- invent analytics
- generate fake business facts
- bypass approval
- store everything as JSON
- bypass tenant isolation
- create giant service classes
- create placeholder APIs
- create fake integrations
- create TODO implementations
- disable tests
- skip migrations

---

## 43. Implementation Sequence

**Overall: 69% (11/16 phases).**

Implement in this order:

### Phase 1 — completed (100%)
Database model + EF configurations + migrations

### Phase 2 — completed (100%)
Content CRUD API

### Phase 3 — completed (100%)
Categories / Tags / Revisions / Media

### Phase 4 — completed (100%)
Content editor

### Phase 5 — completed (100%)
AI generation

### Phase 6 — completed (100%)
SEO/AEO analysis

### Phase 7 — completed (100%)
Content opportunities

### Phase 8 — completed (100%)
Project → Case Study

### Phase 9 — completed (100%)
Content variants / repurposing

### Phase 10 — completed (100%)
Approval integration

### Phase 11 — completed (100%)
Scheduling / Distribution

### Phase 12
Public Content Hub

### Phase 13
Analytics

### Phase 14
Agency / White-label support

### Phase 15
Search / Graphify integration

### Phase 16
Security / performance / accessibility

---

## 44. Visual Quality

The UI must match the DigitalPulse premium design system.

Do NOT build:

- generic blog cards
- generic admin tables
- generic WordPress UI
- generic AI text box

Use:

- editorial layouts
- large article previews
- content pipeline
- AI intelligence panels
- SEO health visualization
- topic opportunity visualization
- calendar
- timeline
- distribution flow
- content analytics
- rich media

The experience should feel like:

# CONTENT INTELLIGENCE

not:

# BLOG MANAGEMENT

---

## 45. Acceptance Criteria

The feature is NOT complete until:

- [x] relational database model implemented
- [x] EF migrations created
- [x] tenant isolation verified
- [x] business isolation verified
- [x] content CRUD works
- [x] content types work
- [x] categories/tags work
- [x] revisions work
- [x] media integration works
- [x] editor works
- [x] AI generation works
- [x] verified-fact rules enforced
- [x] SEO analysis works
- [x] content opportunities work
- [x] project-to-case-study works
- [x] content variants work
- [x] approval integration works
- [x] scheduling works
- [x] supported distribution works
- [x] unsupported providers use Assisted/Manual mode
- [ ] public content hub works
- [ ] analytics works
- [ ] agency clients work
- [ ] white-label entitlement works
- [ ] search indexing works
- [ ] Graphify integration works
- [ ] security tests pass
- [ ] API tests pass
- [ ] frontend tests pass
- [ ] no TypeScript errors
- [ ] no .NET build errors
- [ ] no migration errors
- [ ] no console errors
- [ ] no TODO placeholders
- [ ] no fake data in production paths

---

## 46. Final Instruction

Before coding:

1. Inspect the existing DigitalPulse architecture.
2. Inspect the current database schema.
3. Inspect existing ContentItem and ContentVariant.
4. Inspect Project, MediaAsset, BusinessFact.
5. Inspect ApprovalRequest, Action and Verification.
6. Inspect Provider and PlatformConnection.
7. Use CodeGraph to understand dependencies.
8. Use Graphify to understand the Business → Project → Service → Finding relationships.
9. Identify what can be reused.
10. Identify only the required database changes.

Then produce:

# DIGITALPULSE CONTENT HUB IMPLEMENTATION PLAN

Containing:

1. Existing entities reused
2. New entities
3. Modified entities
4. Relationships
5. Foreign keys
6. Unique constraints
7. Indexes
8. EF migrations
9. API slices
10. UI pages
11. AI workflows
12. Graphify changes
13. Integration changes
14. Testing strategy

After the plan is reviewed internally, implement the feature.

Do not stop at UI mockups.

Implement the complete feature end-to-end.
