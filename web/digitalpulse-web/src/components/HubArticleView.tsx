import { Link } from "react-router-dom";
import { isSafeHref, parseHubMarkup, splitInlines, videoEmbed } from "../lib/hubMarkup";

export function HubArticleView({
  businessName,
  businessHref,
  contentType,
  title,
  excerpt,
  body,
  publishedAtUtc,
  featuredImageUrl,
  compact
}: {
  businessName: string;
  businessHref?: string;
  contentType: string;
  title: string;
  excerpt?: string | null;
  body: string;
  publishedAtUtc?: string | null;
  featuredImageUrl?: string | null;
  compact?: boolean;
}) {
  return (
    <article className={compact ? "hub-article is-preview" : "hub-article"}>
      <p className="hero-kicker">
        {businessHref ? <Link className="text-link" to={businessHref}>{businessName}</Link> : businessName}
        {" · "}
        {contentType.replaceAll("_", " ")}
      </p>
      <h1 className="display display-page">{title || "Untitled draft"}</h1>
      {publishedAtUtc ? <p className="ink-muted">{new Date(publishedAtUtc).toUTCString()}</p> : <p className="ink-muted">Draft preview — not published.</p>}
      {featuredImageUrl && isSafeHref(featuredImageUrl) ? <img className="hub-featured" src={featuredImageUrl} alt="" /> : null}
      {excerpt ? <p className="command-lead">{excerpt}</p> : null}
      <div className="hub-body">
        {parseHubMarkup(body).map((block) => {
          if (block.type === "heading") return <h2 key={block.id}>{block.text}</h2>;
          if (block.type === "quote") return <blockquote key={block.id}><Inlines text={block.text} /></blockquote>;
          if (block.type === "list") {
            return <ul key={block.id}>{block.items.filter((item) => item.trim()).map((item, index) => <li key={index}><Inlines text={item} /></li>)}</ul>;
          }
          if (block.type === "image") {
            return isSafeHref(block.url)
              ? <figure key={block.id}><img className="hub-featured" src={block.url} alt={block.text} />{block.text ? <figcaption>{block.text}</figcaption> : null}</figure>
              : <p key={block.id}>{block.text || "Image URL was not a public https host."}</p>;
          }
          if (block.type === "video") {
            const embed = videoEmbed(block.url);
            return embed
              ? (
                <figure key={block.id} className="hub-video">
                  <iframe title={block.text || embed.provider} src={embed.src} allow="accelerometer; autoplay; clipboard-write; encrypted-media; picture-in-picture" allowFullScreen />
                  <figcaption>{block.text || `${embed.provider} embed from the stored URL.`}</figcaption>
                </figure>
              )
              : isSafeHref(block.url)
                ? <p key={block.id} className="hub-video"><a className="text-link" href={block.url} target="_blank" rel="noreferrer">{block.text || "Watch video"}</a><span className="ink-muted"> — not a YouTube or Vimeo URL, so it stays a link.</span></p>
                : <p key={block.id}>{block.text || "Video URL was not a public https host."}</p>;
          }
          return <p key={block.id}><Inlines text={block.text} /></p>;
        })}
      </div>
    </article>
  );
}

function Inlines({ text }: { text: string }) {
  return (
    <>
      {splitInlines(text).map((part, index) =>
        part.href
          ? part.href.startsWith("/")
            ? <Link key={index} className="text-link" to={part.href}>{mark(part)}</Link>
            : <a key={index} className="text-link" href={part.href} target="_blank" rel="noreferrer">{mark(part)}</a>
          : <span key={index}>{mark(part)}</span>
      )}
    </>
  );
}

function mark(part: { text: string; bold?: boolean; italic?: boolean }) {
  const italic = part.italic ? <em>{part.text}</em> : part.text;
  return part.bold ? <strong>{italic}</strong> : italic;
}
