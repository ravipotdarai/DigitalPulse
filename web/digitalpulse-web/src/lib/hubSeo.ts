export type HubSeoCheck = { code: string; label: string; passed: boolean; note: string };

export type LiveHubSeo = {
  searchIntent: string;
  passed: number;
  total: number;
  seoScore: number;
  metaTitle: string;
  metaDescription: string;
  notes: string[];
  readabilityScore: number;
  aeoScore: number;
  slugScore: number;
  internalLinkScore: number;
  entityCoverageScore: number;
  checks: HubSeoCheck[];
  aeoChecks: HubSeoCheck[];
  entitiesMentioned: number;
  entitiesTotal: number;
};

const HEALTH_TOTAL = 7;
const AEO_TOTAL = 6;

export function liveHubSeo(input: {
  title: string;
  excerpt: string;
  body: string;
  focusKeyword?: string | null;
  canonicalUrl?: string | null;
  slug?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  entities?: string[];
}): LiveHubSeo {
  const heading = input.title.trim();
  const text = input.body.trim();
  const blurb = input.excerpt.trim();
  const keyword = input.focusKeyword?.trim() ?? "";
  const haystack = `${heading}\n${text}`;
  const names = (input.entities ?? []).map((name) => name.trim()).filter(Boolean);
  const unique = [...new Set(names.map((name) => name.toLowerCase()))].map((lower) => names.find((name) => name.toLowerCase() === lower) ?? lower);
  const mentioned = unique.filter((name) => haystack.toLowerCase().includes(name.toLowerCase())).length;
  const metaTitle = input.metaTitle?.trim() || (heading.length <= 60 ? heading : heading.slice(0, 60));
  const metaDescription = input.metaDescription?.trim() || (blurb.length >= 40 ? blurb.slice(0, 160) : text.slice(0, 160));
  const searchIntent = classifyIntent(heading, keyword);
  const intentSignaled = hasIntentSignal(heading, keyword);
  const words = text.split(/\s+/).filter(Boolean);
  const sentences = text.split(".").filter(Boolean).length;
  const avg = sentences === 0 ? words.length : Math.floor(words.length / Math.max(1, sentences));
  const readable = avg >= 8 && avg <= 24;
  const headingStructure = /^#{1,3}\s+\S/m.test(text);
  const internalLinks = text.includes("](/") || text.toLowerCase().includes('href="/');
  const titleReady = heading.length >= 12 && heading.length <= 70 && (keyword.length < 3 || heading.toLowerCase().includes(keyword.toLowerCase()));
  const metaReady = metaDescription.length >= 40 && metaDescription.length <= 160;

  const checks: HubSeoCheck[] = [
    check("search-intent", "Search intent", intentSignaled, intentSignaled
      ? `${searchIntent}. Classified from the title and focus keyword, not from a ranking model.`
      : "Add how, what, vs, buy, or a service so search intent is visible in the title."),
    check("title", "Title optimization", titleReady, titleReady
      ? "Title is 12–70 characters and includes the focus keyword when one is set."
      : "Title should be 12–70 characters and repeat the focus keyword."),
    check("meta-description", "Meta description", metaReady, metaReady ? "Snippet is 40–160 characters." : "Meta description should be 40–160 characters."),
    check("headings", "Heading structure", headingStructure, headingStructure ? "The body has markdown headings." : "Add headings so the article has structure."),
    check("internal-links", "Internal links", internalLinks, internalLinks ? "The body links to another DigitalPulse path." : "Add an internal link to another DigitalPulse path."),
    check("entity-coverage", "Entity coverage", unique.length > 0 && mentioned > 0, unique.length === 0
      ? "Entity coverage stays 0 until this business has approved facts, services, or projects, and the article names them."
      : mentioned === 0
        ? `Add a stored record name. ${unique.length} on record, 0 mentioned.`
        : `${mentioned}/${unique.length} stored records appear in the title or body.`),
    check("readability", "Readability", readable, readable ? "Average sentence length is in the readable band." : "Sentences look too long or too short for a readable article.")
  ];

  const aeoChecks = aeoFrom(text);
  const notes = [
    ...checks.filter((item) => !item.passed).map((item) => item.note),
    ...aeoChecks.filter((item) => !item.passed).map((item) => item.note)
  ];
  if (metaTitle.length < 12 || metaTitle.length > 70) notes.push("Meta title should be 12–70 characters.");
  if (!input.canonicalUrl?.startsWith("https://")) notes.push("Add an https canonical URL when this article has a public page.");
  const slug = input.slug?.trim() ?? "";
  const slugScore = slug.length >= 8 && slug.length <= 60 && slug.includes("-") ? 100 : slug.length >= 3 ? 50 : 0;
  if (slugScore < 100) notes.push("Slug score is the hyphenated public path, not a ranking promise.");

  const passed = checks.filter((item) => item.passed).length;
  const aeoPassed = aeoChecks.filter((item) => item.passed).length;
  return {
    searchIntent,
    passed,
    total: HEALTH_TOTAL,
    seoScore: Math.round((100 * passed) / HEALTH_TOTAL),
    metaTitle,
    metaDescription,
    notes,
    readabilityScore: readable ? 100 : 0,
    aeoScore: Math.round((100 * aeoPassed) / AEO_TOTAL),
    slugScore,
    internalLinkScore: internalLinks ? 100 : 0,
    entityCoverageScore: unique.length === 0 ? 0 : Math.round((100 * mentioned) / unique.length),
    checks,
    aeoChecks,
    entitiesMentioned: mentioned,
    entitiesTotal: unique.length
  };
}

function classifyIntent(title: string, keyword: string) {
  const hay = `${title} ${keyword}`.toLowerCase();
  if (["buy", "price", "hire", "book", "cost"].some((mark) => hay.includes(mark))) return "Transactional";
  if ([" vs ", "vs.", "compare", "best"].some((mark) => hay.includes(mark))) return "Commercial";
  if (hay.includes("near")) return "Navigational";
  return "Informational";
}

function hasIntentSignal(title: string, keyword: string) {
  const hay = `${title} ${keyword}`.toLowerCase();
  return ["how", "what", "why", "guide", "faq", " vs ", "compare", "best", "buy", "price", "hire", "book", "cost", "near"].some((mark) => hay.includes(mark));
}

function aeoFrom(text: string): HubSeoCheck[] {
  const has = (...marks: string[]) => marks.some((mark) => text.toLowerCase().includes(mark.toLowerCase()));
  const steps = text.includes("1.") && text.includes("2.");
  return [
    check("faq", "FAQ", has("FAQ", "Question:", "What should", "What is"), has("FAQ", "Question:", "What should", "What is") ? "FAQ or question copy is present." : "Add an FAQ or a question so answer engines can quote a stored answer."),
    check("howto", "How-to steps", steps || has("## How-to", "How to"), steps ? "Numbered steps are present." : "Add numbered how-to steps."),
    check("definition", "Definitions", has("## Definition", " is a ", "What is "), has("## Definition", " is a ", "What is ") ? "A definition block or sentence is present." : "Add a definition from a stored record."),
    check("comparison", "Comparison", has(" vs ", "compared to", "## Comparison"), has(" vs ", "compared to", "## Comparison") ? "A comparison is present." : "Add a comparison only when stored records support it."),
    check("key-facts", "Key facts", has("## Key facts", "Key considerations", "Key fact"), has("## Key facts", "Key considerations", "Key fact") ? "Key facts are listed." : "Add a key-facts list from approved records."),
    check("summary", "Summary", has("## Summary", "In summary"), has("## Summary", "In summary") ? "A summary heading is present." : "Add a short summary section.")
  ];
}

function check(code: string, label: string, passed: boolean, note: string): HubSeoCheck {
  return { code, label, passed, note };
}
