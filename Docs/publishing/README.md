# DigitalPulse publishing

A canonical `ContentItem` is the source. Platform packs, approvals, and `ContentDistribution` rows are the destinations. DigitalPulse does not invent a posted update.

```text
Create → Optimize → Approve → Schedule → Distribute → Official adapter → Verify
```

## Destinations

| Code | Automatic write | Honest hold |
|---|---|---|
| `HUB` | Public Content Hub on this product | Never. First-party publish. |
| `WEBSITE` | Only if a future official CMS adapter confirms `Published` | Current `WebsiteAdapter` is Assisted. No invented CMS post. |
| `GOOGLE` | Official GBP `localPosts` when `ExternalAccount` contains `locations/` | One distribution row per business location. No fake Published. |
| `LINKEDIN` / `FACEBOOK` / `INSTAGRAM` / `YOUTUBE` | Official adapter `PublishAsync` when a live grant exists and the adapter returns `Published` | Capability-driven Assisted/Hold otherwise. |
| `WHATSAPP` | Consented Cloud API templates only | Excluded from Publish Everywhere. |

## API

Authenticated, tenant-scoped under `/v1/businesses/{businessId}/content`:

- `POST /{contentId}/distribute` — one provider; Google fans out per owned location
- `POST /{contentId}/distribute/everywhere` — HUB + website + Google + social; requires an active plan
- `POST /{contentId}/distributions/{id}/retry|cancel|verify`
- `POST /calendar/release` — also retries held rows and verifies IDs that already exist

Idempotency keys are stored on `ContentDistribution`. Repeating the same key does not create a second row.

## Worker

`PublishingTicker` (registered with monitoring, not in in-memory tests) releases due calendar rows, retries Failed distributions up to 5 attempts, and marks Verified only when an official provider ID is already stored.

## UI

Content Hub → Distribution is Google-first: location checkboxes, GBP preview from the `GooglePost` variant, Publish Everywhere, retry/cancel/verify. A hold is a hold.

## Safety

- Tenant and business filters on every query
- Location IDs must belong to the current business
- Entitlement uses the active plan and this month's distribution count
- Audit rows go to `OperationsAudit` without tokens
- AI variants use Graphify + `IAiOrchestrator`; restricted output is discarded
