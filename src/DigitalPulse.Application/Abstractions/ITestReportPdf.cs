using DigitalPulse.Domain.Website;

namespace DigitalPulse.Application.Abstractions;

public sealed record TestReportPdfHeader(string PreparedFor, DateTimeOffset PrintedAtUtc);

public interface ITestReportPdf
{
    byte[] Render(TestReport report, TestReportPdfHeader header);
}
