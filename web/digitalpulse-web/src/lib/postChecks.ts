const BANNED = /\b(porn|pornography|xxx|onlyfans|nude|nudes|naked|erotica|erotic|fetish|bdsm|hentai|nsfw|sex\s*tape|sexual|escort|hooker|prostitute|prostitutes|camgirl|stripper|incest|bestiality|child\s*porn)\b/i;

export type LiveReview = {
  safetyStatus: "Pass" | "Banned";
  safetyDetail: string;
  seoStatus: "Ready" | "Needs work";
  seoNotes: string[];
  analyticsStatus: "Supported" | "Not connected";
  analyticsDetail: string;
};

export function reviewDraft(
  title: string,
  description: string,
  location: string,
  hasImage: boolean,
  analyticsLive: boolean
): LiveReview {
  const banned = BANNED.test(`${title}\n${description}\n${location}`);
  const notes: string[] = [];
  const heading = title.trim();
  if (heading.length < 8) notes.push("Title is too short for search.");
  if (heading.length > 70) notes.push("Title is longer than 70 characters.");
  if (description.trim().length < 40) notes.push("Description is thin for a search snippet.");
  if (!location.trim()) notes.push("Add a location for local SEO.");
  if (!hasImage) notes.push("Add an image. Posts with media are easier to find and share.");
  const tokens = heading.split(/\s+/).filter((word) => word.length > 3);
  if (tokens.length > 0 && description.trim() && tokens.every((word) => !description.toLowerCase().includes(word.toLowerCase()))) {
    notes.push("Repeat a title word in the description so search can match the post.");
  }

  return {
    safetyStatus: banned ? "Banned" : "Pass",
    safetyDetail: banned
      ? "Sexual or pornographic content is banned on DigitalPulse."
      : "No sexual or pornographic language detected.",
    seoStatus: notes.length === 0 ? "Ready" : "Needs work",
    seoNotes: notes,
    analyticsStatus: analyticsLive ? "Supported" : "Not connected",
    analyticsDetail: analyticsLive
      ? "Google Analytics is connected. DigitalPulse will not invent views or events for this post."
      : "Google Analytics is not authorized. Measurement is not supported until you connect GA4."
  };
}
