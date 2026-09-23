# DigitalPulse

AI Digital Presence OS. This repository currently implements **landing + authentication + multi-tenant onboarding + Phase 2 business digital identity + Phase 3 connection center + Phase 4 DigitalPulse Check + Phase 5 website + search**.

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

Landing → Register → Login → Create Tenant → Tenant → Business → Location → Select Plan → Dashboard → Business identity → Connection Center → DigitalPulse Check → Website intelligence

Connection Center lists official adapters (Google, Meta, LinkedIn, YouTube, IndiaMART, Justdial, WhatsApp Cloud API, Website, Search Console, Google Ads). Connect issues a **development grant** behind `IPlatformAuthorizationBroker`. Live provider OAuth is not invented. IndiaMART/Justdial are assisted-only. WhatsApp is Cloud API only.

DigitalPulse Check compares canonical identity to a safe website fetch (SSRF-blocked) and to authorized connections. Development grants produce “snapshot unavailable” findings — they do not invent live listing data. Scan volume follows `SubscriptionPlan.ScansPerMonth`.

Website intelligence stores an on-page SEO/AEO snapshot from the official homepage. `ISearchProvider` is InMemory and tenant-scoped. Vector search and live Search Console metrics are not configured and are not invented.

Development authentication is JWT behind `IAuthTokenIssuer` (`DevelopmentJwtTokenIssuer`). Entra External ID is not wired yet.

Plans are seeded in SQL (`STARTER`, `GROWTH`, `BUSINESS`, `AGENCY`). Payments are not implemented.
