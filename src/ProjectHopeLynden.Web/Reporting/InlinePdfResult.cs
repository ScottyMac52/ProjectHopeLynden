using Microsoft.AspNetCore.Mvc;
using ProjectHopeLynden.Application.Reporting;

namespace ProjectHopeLynden.Web.Reporting;

public sealed class InlinePdfResult(ReportPdfFile pdf) : FileContentResult(pdf.Content, "application/pdf")
{
    public string InlineFileName { get; } = pdf.FileName;

    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.HttpContext.Response.Headers.ContentDisposition =
            $"inline; filename=\"{InlineFileName.Replace("\"", string.Empty, StringComparison.Ordinal)}\"";
        return base.ExecuteResultAsync(context);
    }
}
