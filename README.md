# DigitalPulse

AI Digital Presence OS. This repository currently implements the **landing + authentication + multi-tenant onboarding** slice.

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

Landing → Register → Login → Create Tenant → Tenant → Business → Location → Select Plan → Dashboard

Development authentication is JWT behind `IAuthTokenIssuer` (`DevelopmentJwtTokenIssuer`). Entra External ID is not wired yet.

Plans are seeded in SQL (`STARTER`, `GROWTH`, `BUSINESS`, `AGENCY`). Payments are not implemented.
