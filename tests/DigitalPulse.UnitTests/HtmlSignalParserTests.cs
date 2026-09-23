using DigitalPulse.Application.Website;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class HtmlSignalParserTests
{
    [Fact]
    public void Parses_title_meta_schema_and_questions()
    {
        const string html = """
            <html>
            <head>
              <title>Harbour Coffee</title>
              <meta name="description" content="House roasted coffee in Mumbai for weekday mornings and weekend pour-over.">
              <meta property="og:title" content="Harbour Coffee">
              <link rel="canonical" href="https://harbour.example/">
              <script type="application/ld+json">{"@type":"LocalBusiness","name":"Harbour Coffee"}</script>
            </head>
            <body>
              <h1>Harbour Coffee</h1>
              <h2>What do you roast?</h2>
              <p>Single origin and house blend, roasted weekly in the harbour district.</p>
            </body>
            </html>
            """;

        var signals = HtmlSignalParser.Parse(html);
        Assert.Equal("Harbour Coffee", signals.Title);
        Assert.Equal("Harbour Coffee", signals.H1);
        Assert.True(signals.HasJsonLd);
        Assert.True(signals.HasOrganizationSchema);
        Assert.False(signals.HasFaqSchema);
        Assert.True(signals.HasOgTitle);
        Assert.Contains("What do you roast?", signals.QuestionHeadings);
        Assert.True(signals.WordCount > 8);
    }

    [Fact]
    public void Empty_html_is_thin_and_unstructured()
    {
        var signals = HtmlSignalParser.Parse("<html><body><p>Hi</p></body></html>");
        Assert.Null(signals.Title);
        Assert.False(signals.HasJsonLd);
        Assert.True(signals.WordCount < 200);
    }
}
