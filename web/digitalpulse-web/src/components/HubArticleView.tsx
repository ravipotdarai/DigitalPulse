import { Link } from "react-router-dom";

function blocks(body: string) {
  return body
    .split(/\n{2,}/)
    .map((block) => block.trim())
    .filter(Boolean);
}

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
      {featuredImageUrl ? <img className="hub-featured" src={featuredImageUrl} alt="" /> : null}
      {excerpt ? <p className="command-lead">{excerpt}</p> : null}
      <div className="hub-body">
        {blocks(body).map((block, index) =>
          block.startsWith("# ")
            ? <h2 key={index}>{block.slice(2)}</h2>
            : <p key={index}>{block}</p>
        )}
      </div>
    </article>
  );
}
