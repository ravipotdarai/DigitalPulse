import { useEffect } from "react";
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

  useEffect(() => {
    if (!slug && index.data) document.title = index.data.metaTitle || `${index.data.businessName} insights`;
    if (slug && articleQuery.data) document.title = articleQuery.data.metaTitle || articleQuery.data.title;
  }, [slug, index.data, articleQuery.data]);

  if (!businessId) return <PageState mode="empty" title="Content Hub not found" />;

  if (!slug) {
    if (index.isLoading) return <PageState mode="loading" title="Opening Content Hub" />;
    if (index.isError || !index.data) return <PageState mode="error" title="Content Hub was not found" />;
    const hub = index.data;
    const latest = hub.articles.filter((item) => item.slug !== hub.featured?.slug);
    if (hub.articles.length === 0) {
      return <PageState mode="empty" title={`${hub.businessName} has no public articles yet`} detail="Published Public articles appear here. Login is not required." />;
    }

    return (
      <article className="hub-article" style={hub.whiteLabel && hub.primaryColor ? { ["--dp-accent" as string]: hub.primaryColor } : undefined}>
        <a className="skip-link" href="#latest">Skip to latest insights</a>
        {hub.whiteLabel && hub.logoUrl ? <img className="hub-featured is-thumb" src={hub.logoUrl} alt={hub.brandName || hub.businessName} /> : null}
        <p className="hero-kicker">{hub.whiteLabel && hub.brandName ? hub.brandName : hub.businessName}</p>
        <h1 className="display display-page">Insights & Resources</h1>
        <p className="command-lead">{hub.metaDescription || "Published articles for this business. No DigitalPulse login is required."}</p>
        {hub.featured ? (
          <section className="panel hub-card" aria-labelledby="featured-title">
            {hub.featured.featuredImageUrl ? <img className="hub-featured" src={hub.featured.featuredImageUrl} alt={hub.featured.title} /> : null}
            <p className="hero-kicker">Featured</p>
            <h2 id="featured-title"><Link className="text-link" to={`/hub/${hub.businessId}/${hub.featured.slug}`}>{hub.featured.title}</Link></h2>
            {hub.featured.excerpt ? <p>{hub.featured.excerpt}</p> : null}
          </section>
        ) : null}
        <section id="latest" aria-labelledby="latest-title">
          <h2 id="latest-title">Latest insights</h2>
          <div className="hub-library">
            {latest.map((item) => (
              <section key={item.slug} className="panel hub-card">
                {item.featuredImageUrl ? <img className="hub-featured is-thumb" src={item.featuredImageUrl} alt={item.title} /> : null}
                <p className="hero-kicker">{item.contentTypeCode.replaceAll("_", " ")}</p>
                <h3><Link className="text-link" to={`/hub/${hub.businessId}/${item.slug}`}>{item.title}</Link></h3>
                <p className="ink-muted">{new Date(item.publishedAtUtc).toUTCString()}</p>
                {item.excerpt ? <p>{item.excerpt}</p> : null}
              </section>
            ))}
          </div>
        </section>
        {(hub.caseStudies?.length ?? 0) > 0 ? (
          <section aria-labelledby="cases-title">
            <h2 id="cases-title">Case studies</h2>
            <ul className="stack-list">
              {hub.caseStudies!.map((item) => (
                <li key={item.slug}><Link className="text-link" to={`/hub/${hub.businessId}/${item.slug}`}>{item.title}</Link></li>
              ))}
            </ul>
          </section>
        ) : null}
        {(hub.services?.length ?? 0) > 0 ? (
          <section aria-labelledby="services-title">
            <h2 id="services-title">Services</h2>
            <ul className="stack-list">{hub.services!.map((name) => <li key={name}>{name}</li>)}</ul>
          </section>
        ) : null}
        {hub.cta ? <p className="command-lead">{hub.cta}</p> : null}
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
