using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Website;

namespace DigitalPulse.Application.Website;

public static class WebsitePageAudits
{
    public static IReadOnlyList<DraftSearchObservation> FromPages(
        Business business,
        IReadOnlyCollection<WebsiteSnapshot> pages,
        IReadOnlyCollection<ContactPoint> contacts,
        IReadOnlyCollection<BusinessLocation> locations,
        IReadOnlyCollection<BusinessFact> facts)
    {
        var items = new List<DraftSearchObservation>();
        if (pages.Count == 0)
        {
            return items;
        }

        var about = pages.Where(p => p.PageRole == WebsitePageRole.About).ToList();
        var contact = pages.Where(p => p.PageRole == WebsitePageRole.Contact).ToList();
        var home = pages.Where(p => p.PageRole == WebsitePageRole.Home).ToList();
        var visionFact = facts.FirstOrDefault(f =>
            f.Status == FactStatus.Approved &&
            (f.FactTypeCode.Equals("VISION", StringComparison.OrdinalIgnoreCase)
             || f.FactTypeCode.Equals("MISSION", StringComparison.OrdinalIgnoreCase)));
        var hours = facts.FirstOrDefault(f =>
            f.Status == FactStatus.Approved && f.FactTypeCode.Equals("HOURS", StringComparison.OrdinalIgnoreCase));

        if (about.Count == 0)
        {
            items.Add(new(
                SearchObservationCategory.Page,
                SearchObservationSeverity.Medium,
                "About page was not found",
                "The same-host crawl did not find an About, About us, or vision URL. Pages were not invented.",
                "An About page on the official website",
                "Not found in the crawl",
                "Add an About page, or rename the URL so DigitalPulse can classify it."));
        }
        else
        {
            if (!about.Any(p => p.ContainsBusinessName))
            {
                items.Add(new(
                    SearchObservationCategory.Page,
                    SearchObservationSeverity.Medium,
                    "About page is missing the business name",
                    "The classified About page does not contain the canonical identity name.",
                    business.Name,
                    about[0].Url,
                    "Publish the same trading name on the About page."));
            }

            if (business.FoundedYear is int year && !about.Any(p =>
                    (p.Title ?? string.Empty).Contains(year.ToString(), StringComparison.Ordinal)
                    || (p.H1 ?? string.Empty).Contains(year.ToString(), StringComparison.Ordinal)))
            {
                items.Add(new(
                    SearchObservationCategory.Page,
                    SearchObservationSeverity.Low,
                    "Founded year is missing on About",
                    "Identity records a founded year that was not observed on the About page title or heading.",
                    year.ToString(),
                    about[0].Url,
                    "Add the founded year to the About page if it should be public."));
            }
        }

        if (visionFact is null)
        {
            items.Add(new(
                SearchObservationCategory.Vision,
                SearchObservationSeverity.Low,
                "Vision is not recorded on the identity",
                "DigitalPulse will not invent a vision statement. Add an approved VISION or MISSION fact first.",
                "An approved VISION or MISSION fact",
                "Missing on identity",
                "Record the vision on Identity, then publish that exact copy on About."));
        }
        else if (!about.Concat(home).Any(p => p.ContainsVision))
        {
            items.Add(new(
                SearchObservationCategory.Vision,
                SearchObservationSeverity.Medium,
                "Vision is missing on About",
                "The approved identity vision was not observed on Home or About.",
                visionFact.Value,
                about.FirstOrDefault()?.Url ?? home.FirstOrDefault()?.Url,
                "Paste the approved vision on the About page."));
        }

        if (contact.Count == 0)
        {
            items.Add(new(
                SearchObservationCategory.Contact,
                SearchObservationSeverity.Medium,
                "Contact page was not found",
                "The same-host crawl did not find a Contact or Contact us URL. Pages were not invented.",
                "A Contact page on the official website",
                "Not found in the crawl",
                "Add a Contact page, or rename the URL so DigitalPulse can classify it."));
        }
        else
        {
            var page = contact[0];
            foreach (var phone in contacts.Where(c => c.Kind == ContactPointKind.Phone))
            {
                if (!contact.Any(p => p.ContainsPhone))
                {
                    items.Add(new(
                        SearchObservationCategory.Contact,
                        SearchObservationSeverity.Medium,
                        "Phone is missing on Contact",
                        "The identity phone was not observed on a classified Contact page.",
                        phone.Value,
                        page.Url,
                        "Publish the same phone number on Contact us."));
                    break;
                }
            }

            foreach (var email in contacts.Where(c => c.Kind == ContactPointKind.Email))
            {
                if (!contact.Any(p => p.ContainsEmail))
                {
                    items.Add(new(
                        SearchObservationCategory.Contact,
                        SearchObservationSeverity.Medium,
                        "Email is missing on Contact",
                        "The identity email was not observed on a classified Contact page.",
                        email.Value,
                        page.Url,
                        "Publish the same email on Contact us."));
                    break;
                }
            }

            if (locations.Count > 0 && !contact.Any(p => p.ContainsAddress))
            {
                var location = locations.First();
                items.Add(new(
                    SearchObservationCategory.Contact,
                    SearchObservationSeverity.Medium,
                    "Address is missing on Contact",
                    "A recorded location was not observed on the Contact page.",
                    string.Join(", ", new[] { location.AddressLine, location.City }.Where(v => !string.IsNullOrWhiteSpace(v))),
                    page.Url,
                    "Publish the same address on Contact us."));
            }

            if (hours is not null && !contact.Any(p =>
                    (p.Title ?? string.Empty).Contains(hours.Value, StringComparison.OrdinalIgnoreCase)
                    || (p.H1 ?? string.Empty).Contains(hours.Value, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(new(
                    SearchObservationCategory.Contact,
                    SearchObservationSeverity.Low,
                    "Hours are missing on Contact",
                    "Approved hours exist on the identity and were not observed on Contact.",
                    hours.Value,
                    page.Url,
                    "Publish the approved hours on Contact us."));
            }

            if (!contact.Any(p => p.HasContactForm))
            {
                items.Add(new(
                    SearchObservationCategory.Contact,
                    SearchObservationSeverity.Low,
                    "Contact form or mailto is missing",
                    "The Contact page has no form, mailto, or tel link in the fetched HTML.",
                    "A form, mailto, or tel link",
                    page.Url,
                    "Add a contact form or a mailto/tel link."));
            }
        }

        return items;
    }
}
