export function liveHubSeo(input: {
  title: string;
  excerpt: string;
  body: string;
  focusKeyword?: string | null;
  canonicalUrl?: string | null;
  slug?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
}) {
  const notes: string[] = [];
  let passed = 0;
  const total = 8;
  const heading = input.title.trim();
  const text = input.body.trim();
  const blurb = input.excerpt.trim();
  const keyword = input.focusKeyword?.trim() ?? "";
  if (heading.length >= 12 && heading.length <= 70) passed++;
  else notes.push("Title should be 12–70 characters for a search snippet.");
  if (blurb.length >= 40 && blurb.length <= 160) passed++;
  else notes.push("Excerpt should be 40–160 characters so search can show a snippet.");
  if (text.length >= 400) passed++;
  else notes.push("Body is thinner than 400 characters. Search has little to index.");
  if (text.includes("#") || text.includes("\n")) passed++;
  else notes.push("Add headings or short paragraphs so the article has structure.");
  if (keyword.length >= 3 && heading.toLowerCase().includes(keyword.toLowerCase())) passed++;
  else notes.push("Repeat the focus keyword in the title, or set a focus keyword.");
  if (keyword.length >= 3 && text.toLowerCase().includes(keyword.toLowerCase())) passed++;
  else notes.push("Use the focus keyword once in the body.");
  if (input.canonicalUrl?.startsWith("https://")) passed++;
  else notes.push("Add an https canonical URL when this article has a public page.");
  const words = text.split(/\s+/).filter(Boolean);
  const sentences = text.split(".").filter(Boolean).length;
  const avg = sentences === 0 ? words.length : Math.floor(words.length / Math.max(1, sentences));
  if (avg >= 8 && avg <= 24) passed++;
  else notes.push("Sentences look too long or too short for a readable article.");
  const metaTitle = input.metaTitle?.trim() || (heading.length <= 60 ? heading : heading.slice(0, 60));
  const metaDescription = input.metaDescription?.trim() || (blurb.length >= 40 ? blurb.slice(0, 160) : text.slice(0, 160));
  if (metaTitle.length < 12 || metaTitle.length > 70) notes.push("Meta title should be 12–70 characters.");
  if (metaDescription.length < 40 || metaDescription.length > 160) notes.push("Meta description should be 40–160 characters.");
  if (!text.includes("](/")) notes.push("Internal-link score is 0 until the body links to another DigitalPulse path.");
  notes.push("Entity coverage stays 0 until Graphify entities are retrieved for this article.");
  return {
    passed,
    total,
    seoScore: Math.round((100 * passed) / total),
    metaTitle,
    metaDescription,
    notes
  };
}
