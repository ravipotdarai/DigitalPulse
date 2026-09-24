# DigitalPulse

AI Digital Presence OS. This repository currently implements **landing + authentication + multi-tenant onboarding + Phases 2–15**, plus a **theme architecture** (Editorial implemented; Executive / Future AI / Minimal fallback tokens).

## Run locally

SQL Server must be reachable as in `.env` (`localhost,1433`, database `DigitalPulse`). Copy `.env.example` to `.env` and leave provider keys empty for a full local run (development grants). Fill a key only when you want that official path on this machine.

```powershell
$env:PATH = "C:\Program Files\dotnet;" + $env:PATH
dotnet restore
dotnet build
dotnet test
dotnet ef migrations add InitialOnboarding --project src/DigitalPulse.Infrastructure --startup-project src/DigitalPulse.Api
dotnet run --project src/DigitalPulse.Api
dotnet run --project src/DigitalPulse.Worker
```

Official Google / Meta / LinkedIn OAuth redirect URI must be exactly `http://localhost:5088/v1/connections/callback`. The API loads `.env` on startup. Without client ids, Connect still issues a **development grant**. With client ids, the browser is sent to the official authorize URL.

Frontend:

```powershell
cd web/digitalpulse-web
npm install
npm run dev
```

Open http://localhost:5173 (API http://localhost:5088).

## Flow

Landing → Register → Login → Create Tenant → Tenant → Business → Location → Select Plan → Dashboard → Business identity → Connection Center → DigitalPulse Check → Website intelligence → Social → WhatsApp → Directories → Projects → AI Orchestrator → Action center → Monitoring → Billing → Agency → Operations

Connection Center lists official adapters (Google, Meta, LinkedIn, YouTube, IndiaMART, Justdial, WhatsApp Cloud API, Website, Search Console, Google Ads). `OfficialOAuthBroker` uses official authorize/token when `Connections:*` client ids are set; otherwise it issues a **development grant**. IndiaMART can attach a CRM key for official lead reads; profile writes stay assisted. Justdial stays assisted — it has no public official write API. WhatsApp is Cloud API only.

DigitalPulse Check compares canonical identity to a safe website fetch (SSRF-blocked) and to authorized connections. Development grants produce “snapshot unavailable” findings — they do not invent live listing data. Scan volume follows `SubscriptionPlan.ScansPerMonth`.

Website intelligence stores an on-page SEO/AEO snapshot from the official homepage. `ISearchProvider` is InMemory and tenant-scoped. Vector search is **LocalHash** on this machine (always on). Live Search Console metrics run only when a Search Console connection has a stored OAuth token and the business website is set; development grants do not invent impressions.

Social workspace drafts Google, Facebook, Instagram, LinkedIn, and YouTube copy. Approve → publish calls the official write when the connection has a live OAuth token (Facebook feed, LinkedIn UGC). Development grants stay on hold. Instagram/YouTube text publish stays unsupported. WhatsApp is not a social post. Metrics are Observed only from the official API; likes and views are not invented.

IndiaMART and Justdial are assisted playbooks prepared from canonical identity. IndiaMART official CRM listing reads run when `Connections:IndiaMART:CrmKey` is set. Justdial official reads/writes are not invented. Verification is operator-confirmed.

Projects store engagement records, permission scope, media catalog entries, and a content factory. Variants (including WhatsApp template/session drafts) are assembled from stored project fields. Approval honors Full / Partial / None. Live publishes and AI rewrites are not invented.

The AI orchestrator retrieves Graphify + knowledge, builds a prompt, calls `IAiProvider`, then validates. No evidence → no claim. Conflict → review. Restricted facts never publish. Without `Ai:OpenAi:ApiKey` the development provider holds with an evidence-only brief.

The action engine queues work through Draft → PendingApproval → Approved → Queued → Executing → Executed → Verified. Failures retry, then escalate. Full Auto only auto-executes low-risk internal work. Publish, WhatsApp, and directory **writes** stay assisted unless the connection has a live credential. `ActionDispatchTicker` (API + Worker) executes queued/retry work every 20 seconds. Autopilot never invents a provider result. Monthly volume follows `SubscriptionPlan.ActionsPerMonth`.

WhatsApp Business Messaging uses official Cloud API only. Starter is disabled. Growth / Business / Agency get 2,000 / 10,000 / 50,000 messages per month. A stored mobile number is not sendable until an explicit opt-in. Business-initiated sends need an approved template. Session replies need an open 24-hour window. Opt-out stops campaign and template sends immediately. Without `WhatsApp:CloudApi:AccessToken` the development provider holds. Unofficial WhatsApp clients are out of scope.

Monitoring records stored platform health, a safe website probe, identity fingerprints, WhatsApp holds, action failures, and operator-listed competitors. Search, live social metrics, reviews, competitor listings, and provider API failures stay held. Reports keep Observed Fact, Recommendation, AI Interpretation, and Customer Decision separate — AI commentary is not invented. Plan intervals are 168h / 24h / 6h / 1h. The hosted ticker uses an unfiltered `AppDbContext` so a null HTTP tenant cannot hide every row.

Development authentication is JWT behind `IAuthTokenIssuer` (`DevelopmentJwtTokenIssuer`). Entra External ID stays unwired for local testing.

Plans and entitlements are seeded in SQL (`STARTER`, `GROWTH`, `BUSINESS`, `AGENCY`). Usage is metered from stored scans, actions, AI runs, and WhatsApp sends. With `Billing:Razorpay` keys, checkout creates an official Razorpay **order** (`payment_capture=0`). The invoice stays unpaid until a signed webhook confirms capture. DigitalPulse does not invent a paid invoice.

Agency clients are extra businesses on one Agency tenant, not child tenants. The Agency plan caps clients at 50 and entitles stored white-label branding. Custom-domain hosting stays held. Agency reports keep Observed Fact, Recommendation, AI Interpretation, and Customer Decision separate and never mix one client's stored work into another.

Operations hardening stores tenant-scoped logical backups, same-tenant restore dry-runs, dependency/container inventories, and a production-readiness review. Security headers, correlation ids, and a 120/min rate limit run on the API. Azure Key Vault, Application Insights, Redis, Azure Backup, geo-failover, and CVE feeds stay held unless configured. Live Azure spend is not inventoried — catalog entitlements are the cost budget. `Dockerfile` and `docker-compose.yml` are local-only. CI runs `dotnet test`.

Visual themes live in `web/digitalpulse-web/src/theme`. Components consume CSS tokens from `ThemeProvider`. Theme preference is stored in `localStorage` (`dp.theme` / `dp.theme.user.{userId}`) and is not part of tenant or plan logic. A developer theme switcher is available in `npm run dev`.
