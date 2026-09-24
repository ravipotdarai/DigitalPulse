# DigitalPulse

AI Digital Presence OS. This repository currently implements **landing + authentication + multi-tenant onboarding + Phases 2–11**, plus a **theme architecture** (Editorial implemented; Executive / Future AI / Minimal fallback tokens).

## Run locally

SQL Server must be reachable as in `.env` (`localhost,1433`, database `DigitalPulse`).

```powershell
$env:PATH = "C:\Program Files\dotnet;" + $env:PATH
dotnet restore
dotnet build
dotnet test
dotnet ef migrations add InitialOnboarding --project src/DigitalPulse.Infrastructure --startup-project src/DigitalPulse.Api
dotnet run --project src/DigitalPulse.Api
```

Frontend:

```powershell
cd web/digitalpulse-web
npm install
npm run dev
```

Open http://localhost:5173 (API http://localhost:5088).

## Flow

Landing → Register → Login → Create Tenant → Tenant → Business → Location → Select Plan → Dashboard → Business identity → Connection Center → DigitalPulse Check → Website intelligence → Social → WhatsApp → Directories → Projects → AI Orchestrator → Action center

Connection Center lists official adapters (Google, Meta, LinkedIn, YouTube, IndiaMART, Justdial, WhatsApp Cloud API, Website, Search Console, Google Ads). Connect issues a **development grant** behind `IPlatformAuthorizationBroker`. Live provider OAuth is not invented. IndiaMART/Justdial are assisted-only. WhatsApp is Cloud API only.

DigitalPulse Check compares canonical identity to a safe website fetch (SSRF-blocked) and to authorized connections. Development grants produce “snapshot unavailable” findings — they do not invent live listing data. Scan volume follows `SubscriptionPlan.ScansPerMonth`.

Website intelligence stores an on-page SEO/AEO snapshot from the official homepage. `ISearchProvider` is InMemory and tenant-scoped. Vector search and live Search Console metrics are not configured and are not invented.

Social workspace drafts Google, Facebook, Instagram, LinkedIn, and YouTube copy. Approve → publish stays on hold unless a live provider write exists. WhatsApp is not a social post. Metrics are hold/unavailable — likes and views are not invented.

IndiaMART and Justdial are assisted playbooks prepared from canonical identity. Official reads/writes are not invented; verification is operator-confirmed.

Projects store engagement records, permission scope, media catalog entries, and a content factory. Variants (including WhatsApp template/session drafts) are assembled from stored project fields. Approval honors Full / Partial / None. Live publishes and AI rewrites are not invented.

The AI orchestrator retrieves Graphify + knowledge, builds a prompt, calls `IAiProvider`, then validates. No evidence → no claim. Conflict → review. Restricted facts never publish. Without `Ai:OpenAi:ApiKey` the development provider holds with an evidence-only brief.

The action engine queues work through Draft → PendingApproval → Approved → Queued → Executing → Executed → Verified. Failures retry, then escalate. Full Auto only auto-executes low-risk internal work. Publish and directory writes stay assisted unless an official live adapter exists. Autopilot never invents a provider result. Monthly volume follows `SubscriptionPlan.ActionsPerMonth`.

WhatsApp Business Messaging uses official Cloud API only. Starter is disabled. Growth / Business / Agency get 2,000 / 10,000 / 50,000 messages per month. A stored mobile number is not sendable until an explicit opt-in. Business-initiated sends need an approved template. Session replies need an open 24-hour window. Opt-out stops campaign and template sends immediately. Without `WhatsApp:CloudApi:AccessToken` the development provider holds. Unofficial WhatsApp clients are out of scope.

Development authentication is JWT behind `IAuthTokenIssuer` (`DevelopmentJwtTokenIssuer`). Entra External ID is not wired yet.

Plans are seeded in SQL (`STARTER`, `GROWTH`, `BUSINESS`, `AGENCY`). Payments are not implemented.

Visual themes live in `web/digitalpulse-web/src/theme`. Components consume CSS tokens from `ThemeProvider`. Theme preference is stored in `localStorage` (`dp.theme` / `dp.theme.user.{userId}`) and is not part of tenant or plan logic. A developer theme switcher is available in `npm run dev`.
