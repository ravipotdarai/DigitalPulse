using System.Text;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Website;

namespace DigitalPulse.Infrastructure.Reports;

public sealed class TestReportPdf : ITestReportPdf
{
    private const string Tagline = "AI Digital Presence OS";
    private const float PageWidth = 612;
    private const float PageHeight = 792;
    private const float Left = 48;

    public byte[] Render(TestReport report, TestReportPdfHeader header)
    {
        var pages = new List<string>();
        var y = 0f;
        var content = new StringBuilder();

        void NewPage()
        {
            if (content.Length > 0)
            {
                pages.Add(content.ToString());
                content.Clear();
            }

            y = 742;
            DrawHeader(content, report, header);
            y = 628;
        }

        NewPage();
        Section(content, ref y, pages, NewPage, "Observed fact", report.ObservedFact);
        Section(content, ref y, pages, NewPage, "Recommendation", report.Recommendation);
        if (!string.IsNullOrWhiteSpace(report.HoldReason))
        {
            Section(content, ref y, pages, NewPage, "Hold", report.HoldReason);
        }

        if (!string.IsNullOrWhiteSpace(report.Body))
        {
            Section(content, ref y, pages, NewPage, "Detail", report.Body.Replace("\r", string.Empty));
        }

        Section(content, ref y, pages, NewPage, "Integrity", "This PDF is assembled from stored observations only. Clicks, sessions, campaigns, and pages were not invented.");
        pages.Add(content.ToString());

        return Assemble(pages);
    }

    private static void DrawHeader(StringBuilder content, TestReport report, TestReportPdfHeader header)
    {
        content.Append("0.07 0.09 0.12 rg 0 720 612 72 re f ");
        content.Append("0.82 0.66 0.28 rg 48 738 18 18 re f ");
        content.Append("1 1 1 rg BT /F2 18 Tf 74 744 Td (DigitalPulse) Tj ET ");
        content.Append("0.82 0.84 0.86 rg BT /F1 9 Tf 74 728 Td (")
            .Append(Escape(Tagline))
            .Append(") Tj ET ");
        content.Append("0.16 0.18 0.22 rg 48 700 516 0.6 re f ");
        content.Append("0.07 0.09 0.12 rg BT /F2 14 Tf 48 676 Td (")
            .Append(Escape(report.Title))
            .Append(") Tj ET ");
        content.Append("0.35 0.38 0.42 rg BT /F1 9 Tf 48 658 Td (")
            .Append(Escape($"{report.Kind}  ·  {header.PrintedAtUtc.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)}  ·  Prepared for {header.PreparedFor}"))
            .Append(") Tj ET ");
    }

    private static void Section(StringBuilder content, ref float y, List<string> pages, Action newPage, string heading, string body)
    {
        if (y < 120)
        {
            newPage();
        }

        content.Append("0.07 0.09 0.12 rg BT /F2 11 Tf ").Append(Pdf.F(Left)).Append(' ').Append(Pdf.F(y)).Append(" Td (")
            .Append(Escape(heading))
            .Append(") Tj ET ");
        y -= 16;
        foreach (var line in Wrap(body, 88))
        {
            if (y < 72)
            {
                newPage();
            }

            content.Append("0.20 0.22 0.26 rg BT /F1 10 Tf ").Append(Pdf.F(Left)).Append(' ').Append(Pdf.F(y)).Append(" Td (")
                .Append(Escape(line))
                .Append(") Tj ET ");
            y -= 14;
        }

        y -= 10;
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                yield return string.Empty;
                continue;
            }

            var words = paragraph.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length + word.Length + 1 > width)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                {
                    line.Append(' ');
                }

                line.Append(word);
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }
    }

    private static byte[] Assemble(IReadOnlyList<string> pageStreams)
    {
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            string.Empty,
            "3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj\n"
        };

        var pageRefs = new List<string>();
        var nextId = 5;
        var contentIds = new List<int>();
        foreach (var stream in pageStreams)
        {
            var bytes = Encoding.ASCII.GetBytes(stream);
            objects.Add($"{nextId} 0 obj << /Length {bytes.Length} >> stream\n{stream}\nendstream endobj\n");
            contentIds.Add(nextId);
            nextId++;
        }

        foreach (var contentId in contentIds)
        {
            objects.Add($"{nextId} 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Contents {contentId} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >> endobj\n");
            pageRefs.Add($"{nextId} 0 R");
            nextId++;
        }

        objects[1] = $"2 0 obj << /Type /Pages /Kids [{string.Join(' ', pageRefs)}] /Count {pageRefs.Count} >> endobj\n";

        var pdf = new StringBuilder();
        pdf.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(obj);
        }

        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            pdf.Append($"{offsets[i]:D10} 00000 n \n");
        }

        pdf.Append($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\n", " ").Replace("\r", " ");

    private static class Pdf
    {
        public static string F(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
