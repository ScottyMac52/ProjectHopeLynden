namespace ProjectHopeLynden.Infrastructure.Reporting;

public interface IReportPdfRenderer
{
    byte[] Render(ReportPdfDefinition definition);
}
