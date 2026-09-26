import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { HubArticleView } from "../components/HubArticleView";
import { PageState } from "../components/PageState";
import { api } from "../lib/api";

export function PublicHubPage() {
  const { businessId, slug } = useParams();
  const index = useQuery({
    queryKey: ["public-hub", businessId],
    queryFn: () => api.publicHubIndex(businessId!),
    enabled: Boolean(businessId && !slug)
  });
  const articleQuery = useQuery({
    queryKey: ["public-hub", businessId, slug],
    queryFn: () => api.publicHubArticle(businessId!, slug!),
    enabled: Boolean(businessId && slug)
  });

  if (!businessId) return <PageState mode="empty" title="Content Hub not found" />;

  if (!slug) {
    if (index.isLoading) return <PageState mode="loading" title="Opening Content Hub" />;
    if (index.isError || !index.data) return <PageState mode="error" title="Content Hub was not found" />;
    const hub = index.data;
    if (hub.articles.length === 0) {
      return <PageState mode="empty" title={`${hub.businessName} has no public articles yet`} detail="Published Public articles appear here. Login is not required." />;
    }

    return (
      <article className="hub-article">
        <p className="hero-kicker">{hub.businessName}</p>
        <h1 className="display display-page">Insights & Resources</h1>
        <p className="command-lead">Published articles for this business. No DigitalPulse login is required.</p>
        <div className="hub-library">
          {hub.articles.map((item) => (
            <section key={item.slug} className="panel hub-card">
              {item.featuredImageUrl ? <img className="hub-featured is-thumb" src={item.featuredImageUrl} alt="" /> : null}
              <p className="hero-kicker">{item.contentTypeCode.replaceAll("_", " ")}</p>
              <h2><Link className="text-link" to={`/hub/${hub.businessId}/${item.slug}`}>{item.title}</Link></h2>
              <p className="ink-muted">{new Date(item.publishedAtUtc).toUTCString()}</p>
              {item.excerpt ? <p>{item.excerpt}</p> : null}
            </section>
          ))}
        </div>
      </article>
    );
  }

  if (articleQuery.isLoading) return <PageState mode="loading" title="Opening article" />;
  if (articleQuery.isError || !articleQuery.data) return <PageState mode="error" title="Published article was not found" />;

  const article = articleQuery.data;
  return (
    <HubArticleView
      businessName={article.businessName}
      businessHref={`/hub/${businessId}`}
      contentType={article.contentTypeCode}
      title={article.title}
      excerpt={article.excerpt}
      body={article.body}
      publishedAtUtc={article.publishedAtUtc}
      featuredImageUrl={article.featuredImageUrl}
    />
  );
}
