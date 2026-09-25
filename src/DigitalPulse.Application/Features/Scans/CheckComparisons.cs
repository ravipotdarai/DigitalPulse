using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Website;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;

namespace DigitalPulse.Application.Features.Scans;

public sealed record DraftEvidence(EvidenceKind Kind, string Label, string Value, string Source);

public sealed record DraftFinding(
    string Category,
    FindingSeverity Severity,
    string Title,
    string Description,
    string? ExpectedValue,
    string? ObservedValue,
    string Recommendation,
    string SuggestedAction,
    string VerificationMethod,
    FindingAutomationState AutomationState,
    IReadOnlyList<DraftEvidence> Evidence);

public static class CheckComparisons
{
    public static IReadOnlyList<DraftFinding> CompareIdentity(
        Business business,
        IReadOnlyCollection<BusinessLocation> locations,
        IReadOnlyCollection<ContactPoint> contacts,
        int categoryCount,
        int serviceCount,
        IReadOnlyCollection<BusinessFact> facts)
    {
        var findings = new List<DraftFinding>();
        var phones = contacts.Where(c => c.Kind == ContactPointKind.Phone).ToList();
        var emails = contacts.Where(c => c.Kind == ContactPointKind.Email).ToList();
        var approved = facts.Where(f => f.Status == FactStatus.Approved).ToList();
        var restricted = facts.Where(f => f.Status == FactStatus.Restricted).ToList();

        if (string.IsNullOrWhiteSpace(business.Website))
        {
            findings.Add(IdentityGap(
                FindingSeverity.High,
                "Official website is missing",
                "DigitalPulse Check cannot compare the public site until the identity record has a website.",
                "A public website URL on the business identity",
                "Not set",
                "Add the official website on the identity record.",
                "Identity.Website"));
        }

        if (phones.Count == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.High,
                "Phone contact is missing",
                "Canonical identity has no phone contact point to compare against platforms or the website.",
                "At least one Phone contact point",
                "None",
                "Add a phone contact on the identity record.",
                "Identity.ContactPoint.Phone"));
        }

        if (emails.Count == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.Medium,
                "Email contact is missing",
                "Canonical identity has no email contact point.",
                "At least one Email contact point",
                "None",
                "Add an email contact on the identity record.",
                "Identity.ContactPoint.Email"));
        }

        if (locations.Count == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.High,
                "Business location is missing",
                "NAP comparison needs at least one location on the identity record.",
                "At least one location",
                "None",
                "Add a location during identity setup.",
                "Identity.Location"));
        }

        if (categoryCount == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.Medium,
                "Business category is missing",
                "Directory and search listings cannot be compared without a canonical category.",
                "At least one category",
                "None",
                "Add a category on the identity record.",
                "Identity.Category"));
        }

        if (serviceCount == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.Medium,
                "No services recorded",
                "Service coverage cannot be compared until the identity lists what the business sells.",
                "At least one service",
                "None",
                "Add a service on the identity record.",
                "Identity.Service"));
        }

        if (approved.Count == 0)
        {
            findings.Add(IdentityGap(
                FindingSeverity.Medium,
                "No approved publishable facts",
                "Facts in Draft or Restricted status are not treated as public claims.",
                "At least one Approved fact",
                facts.Count == 0 ? "None" : string.Join(", ", facts.Select(f => $"{f.FactTypeCode}:{f.Status}")),
                "Approve one fact that is allowed to publish.",
                "Identity.Fact"));
        }

        if (restricted.Count > 0)
        {
            findings.Add(new DraftFinding(
                "Policy",
                FindingSeverity.Info,
                "Restricted facts will not publish",
                "Restricted facts stay on the identity record and are excluded from platform writes.",
                "Restricted facts remain unpublished",
                string.Join(", ", restricted.Select(f => f.FactTypeCode)),
                "Keep GSTIN and other restricted facts off public listings.",
                "No action required unless a fact was marked Restricted by mistake.",
                "Review fact status on the identity record.",
                FindingAutomationState.None,
                restricted.Select(f => new DraftEvidence(
                    EvidenceKind.Policy,
                    f.FactTypeCode,
                    f.Value,
                    "Identity.Fact.Restricted")).ToList()));
        }

        if (string.IsNullOrWhiteSpace(business.IndustryCode))
        {
            findings.Add(IdentityGap(
                FindingSeverity.Low,
                "Industry is not set",
                "Industry helps later search and content agents. It is not required to run a check.",
                "An industry code on the business",
                "Not set",
                "Choose an industry on the identity record.",
                "Identity.Industry"));
        }

        return findings;
    }

    public static IReadOnlyList<DraftFinding> CompareWebsite(Business business, WebsiteProbeResult probe, IReadOnlyCollection<ContactPoint> phones)
    {
        if (!probe.Attempted || probe.Disabled)
        {
            return [];
        }

        if (probe.Blocked)
        {
            return
            [
                new DraftFinding(
                    "Website",
                    FindingSeverity.High,
                    "Website URL is not allowed",
                    "The website on the identity record failed the crawl safety policy (scheme, host, or private address).",
                    "A public http(s) website",
                    probe.Error ?? "Blocked",
                    "Use a public website URL. DigitalPulse will not fetch private or reserved hosts.",
                    "Correct the website on the identity record.",
                    "Re-run DigitalPulse Check after the URL change.",
                    FindingAutomationState.Assisted,
                    [
                        new DraftEvidence(EvidenceKind.Identity, "Website", business.Website ?? "", "Identity.Website"),
                        new DraftEvidence(EvidenceKind.Policy, "SSRF policy", probe.Error ?? "Blocked", "SafeUrlPolicy")
                    ])
            ];
        }

        if (!probe.Reached)
        {
            return
            [
                new DraftFinding(
                    "Website",
                    FindingSeverity.High,
                    "Website is unreachable",
                    "DigitalPulse Check fetched the official website and did not receive a usable public response.",
                    "HTTP response from the official website",
                    probe.Error ?? "Unreachable",
                    "Confirm the site is publicly reachable, then re-run the check.",
                    "Verify DNS and hosting for the official website.",
                    "Re-run DigitalPulse Check after the site responds.",
                    FindingAutomationState.Assisted,
                    [
                        new DraftEvidence(EvidenceKind.Identity, "Website", business.Website ?? "", "Identity.Website"),
                        new DraftEvidence(EvidenceKind.Website, "Probe error", probe.Error ?? "Unreachable", "WebsiteProbe")
                    ])
            ];
        }

        var findings = new List<DraftFinding>();
        if (!probe.ContainsBusinessName)
        {
            findings.Add(new DraftFinding(
                "Website",
                FindingSeverity.Medium,
                "Business name not found on the website",
                "The fetched page did not contain the canonical business name. This is an observed HTML comparison, not a search ranking.",
                business.Name,
                probe.FinalUrl,
                "Confirm the homepage publishes the same legal or trading name as the identity record.",
                "Update the website or the identity name so they match.",
                "Re-run DigitalPulse Check and confirm the name appears in the fetched HTML.",
                FindingAutomationState.Assisted,
                [
                    new DraftEvidence(EvidenceKind.Identity, "Canonical name", business.Name, "Identity.Business.Name"),
                    new DraftEvidence(EvidenceKind.Website, "Fetched URL", probe.FinalUrl ?? "", "WebsiteProbe"),
                    new DraftEvidence(EvidenceKind.Website, "HTTP status", probe.StatusCode?.ToString() ?? "", "WebsiteProbe")
                ]));
        }

        var phone = phones.FirstOrDefault();
        if (phone is not null && !probe.ContainsPhone)
        {
            findings.Add(new DraftFinding(
                "Website",
                FindingSeverity.Low,
                "Phone number not found on the website",
                "The fetched page did not contain the digits of the canonical phone contact.",
                phone.Value,
                probe.FinalUrl,
                "Publish the same phone number on the website, or record the number the site actually uses.",
                "Align the website phone with the identity contact.",
                "Re-run DigitalPulse Check after the page or contact changes.",
                FindingAutomationState.Assisted,
                [
                    new DraftEvidence(EvidenceKind.Identity, "Phone", phone.Value, "Identity.ContactPoint.Phone"),
                    new DraftEvidence(EvidenceKind.Website, "Fetched URL", probe.FinalUrl ?? "", "WebsiteProbe")
                ]));
        }

        return findings;
    }

    public static IReadOnlyList<DraftFinding> CompareConnections(
        IReadOnlyCollection<PlatformConnection> connections,
        IPlatformAdapterCatalog catalog)
    {
        var findings = new List<DraftFinding>();
        var connected = connections.Where(c => c.Status == ConnectionStatus.Connected).ToList();

        if (connected.Count == 0)
        {
            findings.Add(new DraftFinding(
                "Platform",
                FindingSeverity.Medium,
                "No authorized platforms to compare",
                "DigitalPulse Check only reads platforms the tenant has authorized. None are connected for this business.",
                "At least one connected platform",
                "None connected",
                "Connect a platform in Connection Center. Live provider snapshots are not invented.",
                "Open Connection Center and connect a platform.",
                "Re-run DigitalPulse Check after a connection is healthy.",
                FindingAutomationState.Suggested,
                [
                    new DraftEvidence(EvidenceKind.Connection, "Connected platforms", "0", "PlatformConnection")
                ]));
            return findings;
        }

        foreach (var connection in connected)
        {
            var adapter = catalog.Get(connection.PlatformCode);
            var name = adapter.Describe().Name;
            if (string.Equals(connection.GrantKind, "Development", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new DraftFinding(
                    "Platform",
                    FindingSeverity.Medium,
                    $"{name} snapshot is unavailable",
                    "This connection uses a development grant. DigitalPulse Check will not invent live listing, review, or profile data.",
                    "A live provider grant with read capability",
                    connection.GrantKind,
                    "Keep the adapter contract. Configure a real provider later; do not treat development grants as observed listings.",
                    "Leave the connection as a development grant, or wait for a live authorization broker.",
                    "Diagnostics on the connection already report that the live provider API is on hold.",
                    FindingAutomationState.Blocked,
                    [
                        new DraftEvidence(EvidenceKind.Connection, "Platform", name, $"PlatformConnection.{connection.PlatformCode}"),
                        new DraftEvidence(EvidenceKind.Connection, "Grant kind", connection.GrantKind ?? "", "PlatformConnection.GrantKind"),
                        new DraftEvidence(EvidenceKind.Connection, "Health", connection.LastHealthStatus ?? connection.Status.ToString(), "PlatformConnection.Health")
                    ]));
                continue;
            }

            if (IsGoogleReader(connection.PlatformCode))
            {
                continue;
            }

            findings.Add(new DraftFinding(
                "Platform",
                FindingSeverity.Low,
                $"{name} has no snapshot reader yet",
                "The connection is authorized, but this phase does not call a live provider API to invent profile fields.",
                "An evidence-backed platform snapshot",
                "No snapshot stored",
                "Platform discovery waits for a real adapter read. Findings stay limited to identity and website evidence.",
                "Do not assume listing data exists.",
                "Re-run DigitalPulse Check after a snapshot reader is implemented for this adapter.",
                FindingAutomationState.Blocked,
                [
                    new DraftEvidence(EvidenceKind.Connection, "Platform", name, $"PlatformConnection.{connection.PlatformCode}"),
                    new DraftEvidence(EvidenceKind.Connection, "Grant kind", connection.GrantKind ?? "Unknown", "PlatformConnection.GrantKind")
                ]));
        }

        foreach (var connection in connections.Where(c => c.Status != ConnectionStatus.Connected))
        {
            var name = catalog.Get(connection.PlatformCode).Describe().Name;
            findings.Add(new DraftFinding(
                "Platform",
                FindingSeverity.Low,
                $"{name} is not healthy enough to compare",
                "Only connected platforms can be read. This connection is still connecting, needs reauth, or is in error.",
                ConnectionStatus.Connected.ToString(),
                connection.Status.ToString(),
                "Finish authorization or repair the connection before expecting platform findings.",
                "Open Connection Center and complete or reauthorize the platform.",
                "Re-run DigitalPulse Check after health is Connected.",
                FindingAutomationState.Assisted,
                [
                    new DraftEvidence(EvidenceKind.Connection, "Status", connection.Status.ToString(), $"PlatformConnection.{connection.PlatformCode}"),
                    new DraftEvidence(EvidenceKind.Connection, "Last error", connection.LastError ?? "None", "PlatformConnection.LastError")
                ]));
        }

        return findings;
    }

    public static IReadOnlyList<DraftFinding> CompareSiteAudit(
        IReadOnlyCollection<DigitalPulse.Domain.Website.SearchObservation> observations)
    {
        return observations
            .Where(o => o.Category is DigitalPulse.Domain.Website.SearchObservationCategory.Page
                or DigitalPulse.Domain.Website.SearchObservationCategory.Vision
                or DigitalPulse.Domain.Website.SearchObservationCategory.Contact
                or DigitalPulse.Domain.Website.SearchObservationCategory.Seo
                or DigitalPulse.Domain.Website.SearchObservationCategory.Visibility)
            .Select(o => new DraftFinding(
                o.Category.ToString(),
                o.Severity == DigitalPulse.Domain.Website.SearchObservationSeverity.High ? FindingSeverity.High
                    : o.Severity == DigitalPulse.Domain.Website.SearchObservationSeverity.Medium ? FindingSeverity.Medium
                    : FindingSeverity.Low,
                o.Title,
                o.Detail,
                o.ExpectedValue,
                o.ObservedValue,
                o.Recommendation,
                o.Recommendation,
                "Re-analyze the website and confirm the official page now matches the identity record.",
                FindingAutomationState.Assisted,
                [
                    new DraftEvidence(EvidenceKind.Website, o.Title, o.ObservedValue ?? o.Detail, $"SearchObservation.{o.Category}")
                ]))
            .ToList();
    }

    public static IReadOnlyList<DraftFinding> CompareSearch(
        IReadOnlyCollection<DigitalPulse.Domain.Website.SearchConsoleQuery> queries)
    {
        if (queries.Count == 0)
        {
            return [];
        }

        return queries.Take(5).Select(q => new DraftFinding(
            "SearchConsole",
            FindingSeverity.Low,
            Clip($"Search Console query: {q.Query}", 160),
            "This query came from official searchAnalytics/query. Clicks were not invented.",
            "An official query row",
            $"{q.Clicks:0} clicks / {q.Impressions:0} impressions",
            "Improve the matching page on the website. DigitalPulse cannot change a ranking.",
            "Open the page editor and strengthen the copy for this query.",
            "Re-analyze the website after the page change.",
            FindingAutomationState.Assisted,
            [
                new DraftEvidence(EvidenceKind.Scan, q.Query, $"{q.Clicks:0}/{q.Impressions:0}", "SearchConsoleQuery")
            ])).ToList();
    }

    public static IReadOnlyList<DraftFinding> CompareGoogleMetrics(
        string category,
        string platformName,
        DigitalPulse.Domain.Website.TestReport? report)
    {
        if (report is null)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(report.HoldReason))
        {
            if (category.Equals("Ads", StringComparison.OrdinalIgnoreCase))
            {
                var campaigns = GoogleAdsCampaigns.Parse(report.Body);
                if (campaigns.Count == 0)
                {
                    return
                    [
                        new DraftFinding(
                            category,
                            FindingSeverity.Low,
                            "Google Ads returned no campaigns",
                            "Official googleAds:search returned no campaign rows. Campaigns were not invented.",
                            "Official campaign rows",
                            "None",
                            "Create or enable campaigns in Google Ads. DigitalPulse does not mutate campaigns.",
                            "Open Google Ads and review the official customer.",
                            "Refresh Ads metrics after Google returns campaigns.",
                            FindingAutomationState.Assisted,
                            [new DraftEvidence(EvidenceKind.Connection, platformName, "No official campaigns", $"TestReport.{report.Kind}")])
                    ];
                }

                return campaigns.Take(10).Select(c => new DraftFinding(
                    category,
                    FindingSeverity.Low,
                    Clip($"Google Ads campaign: {c.Name}", 160),
                    "This campaign came from official googleAds:search. Spend and status were not invented.",
                    "An official campaign row",
                    $"{c.Id} · {c.Status} · {c.Impressions ?? 0:0} impr / {c.Clicks ?? 0:0} clicks",
                    "Change the campaign in the official Google Ads UI. DigitalPulse does not pause, bid, or add keywords.",
                    "Open ads.google.com for this customer.",
                    "Refresh Ads metrics after the official change.",
                    FindingAutomationState.Assisted,
                    [new DraftEvidence(EvidenceKind.Scan, c.Name, $"{c.Id}|{c.Status}", "GoogleAdsCampaign")])).ToList();
            }

            return
            [
                new DraftFinding(
                    category,
                    FindingSeverity.Low,
                    $"{platformName} snapshot stored",
                    report.ObservedFact,
                    "Official adapter metrics",
                    report.ObservedFact,
                    report.Recommendation,
                    report.Recommendation,
                    "Refresh the official metrics after you change the provider UI.",
                    FindingAutomationState.Assisted,
                    [new DraftEvidence(EvidenceKind.Connection, platformName, report.ObservedFact, $"TestReport.{report.Kind}")])
            ];
        }

        return
        [
            new DraftFinding(
                category,
                FindingSeverity.Medium,
                $"{platformName} is on hold",
                report.HoldReason,
                "A live official grant and required tokens",
                report.HoldReason,
                report.Recommendation,
                "Connect or configure the official grant. Values are not invented.",
                "Re-analyze after a live grant is stored.",
                report.HoldReason.Contains("Development", StringComparison.OrdinalIgnoreCase)
                    ? FindingAutomationState.Blocked
                    : FindingAutomationState.Assisted,
                [new DraftEvidence(EvidenceKind.Connection, platformName, report.HoldReason, $"TestReport.{report.Kind}")])
        ];
    }

    private static bool IsGoogleReader(string platformCode) =>
        platformCode.Equals("SEARCH_CONSOLE", StringComparison.OrdinalIgnoreCase)
        || platformCode.Equals("GOOGLE_ADS", StringComparison.OrdinalIgnoreCase)
        || platformCode.Equals("GOOGLE_ANALYTICS", StringComparison.OrdinalIgnoreCase)
        || platformCode.Equals("GOOGLE", StringComparison.OrdinalIgnoreCase);

    private static string Clip(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private static DraftFinding IdentityGap(
        FindingSeverity severity,
        string title,
        string description,
        string expected,
        string observed,
        string action,
        string source) =>
        new(
            "Identity",
            severity,
            title,
            description,
            expected,
            observed,
            action,
            action,
            "Re-run DigitalPulse Check after the identity record changes.",
            FindingAutomationState.Suggested,
            [new DraftEvidence(EvidenceKind.Identity, title, observed, source)]);
}
