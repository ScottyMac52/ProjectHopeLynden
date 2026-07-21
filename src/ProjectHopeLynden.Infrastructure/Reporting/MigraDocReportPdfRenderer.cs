using System.Threading;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace ProjectHopeLynden.Infrastructure.Reporting;

public sealed class MigraDocReportPdfRenderer : IReportPdfRenderer
{
    private static int fontSettingsConfigured;

    public MigraDocReportPdfRenderer()
    {
        if (OperatingSystem.IsWindows() && Interlocked.Exchange(ref fontSettingsConfigured, 1) == 0)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }
    }

    public byte[] Render(ReportPdfDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var document = CreateDocument(definition);
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static Document CreateDocument(ReportPdfDefinition definition)
    {
        var document = new Document();
        document.Info.Title = definition.Title;
        document.Info.Subject = "Project Hope Food Bank of Lynden report";
        document.Info.Author = "Project Hope Food Bank of Lynden";

        ConfigureStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.Orientation = definition.Landscape ? Orientation.Landscape : Orientation.Portrait;
        section.PageSetup.TopMargin = Unit.FromInch(0.65);
        section.PageSetup.BottomMargin = Unit.FromInch(0.6);
        section.PageSetup.LeftMargin = Unit.FromInch(0.6);
        section.PageSetup.RightMargin = Unit.FromInch(0.6);

        AddHeaderAndFooter(section, definition);

        var title = section.AddParagraph(definition.Title);
        title.Style = "ReportTitle";
        var generated = section.AddParagraph($"Generated {definition.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
        generated.Style = "ReportGenerated";

        if (definition.Details.Count > 0)
        {
            AddDetails(section, definition.Details);
        }

        foreach (var reportSection in definition.Sections)
        {
            var heading = section.AddParagraph(reportSection.Heading);
            heading.Style = "ReportHeading";

            if (reportSection.Table is null || reportSection.Table.Rows.Count == 0)
            {
                var empty = section.AddParagraph(reportSection.EmptyMessage ?? "No rows are available for this report.");
                empty.Style = "ReportEmpty";
                continue;
            }

            AddTable(section, reportSection.Table);
        }

        return document;
    }

    private static void ConfigureStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 8;
        normal.Font.Color = Color.FromRgb(36, 49, 58);

        var title = document.Styles.AddStyle("ReportTitle", StyleNames.Normal);
        title.Font.Name = "Arial";
        title.Font.Size = 20;
        title.Font.Bold = true;
        title.Font.Color = Color.FromRgb(15, 52, 76);
        title.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);

        var generated = document.Styles.AddStyle("ReportGenerated", StyleNames.Normal);
        generated.Font.Name = "Arial";
        generated.Font.Size = 8;
        generated.Font.Color = Color.FromRgb(98, 112, 122);
        generated.ParagraphFormat.SpaceAfter = Unit.FromPoint(9);

        var heading = document.Styles.AddStyle("ReportHeading", StyleNames.Normal);
        heading.Font.Name = "Arial";
        heading.Font.Size = 12;
        heading.Font.Bold = true;
        heading.Font.Color = Color.FromRgb(23, 79, 115);
        heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        heading.ParagraphFormat.KeepWithNext = true;

        var empty = document.Styles.AddStyle("ReportEmpty", StyleNames.Normal);
        empty.Font.Name = "Arial";
        empty.Font.Size = 9;
        empty.Font.Italic = true;
        empty.Font.Color = Color.FromRgb(98, 112, 122);
        empty.ParagraphFormat.SpaceAfter = Unit.FromPoint(8);
    }

    private static void AddHeaderAndFooter(Section section, ReportPdfDefinition definition)
    {
        var header = section.Headers.Primary.AddParagraph("PROJECT HOPE FOOD BANK OF LYNDEN");
        header.Format.Font.Name = "Arial";
        header.Format.Font.Size = 7;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Color.FromRgb(23, 79, 115);
        header.Format.SpaceAfter = Unit.FromPoint(5);

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Name = "Arial";
        footer.Format.Font.Size = 7;
        footer.Format.Font.Color = Color.FromRgb(98, 112, 122);
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText($"{definition.Title}  |  Page ");
        footer.AddPageField();
    }

    private static void AddDetails(Section section, IReadOnlyList<ReportPdfDetail> details)
    {
        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.4);
        table.Borders.Color = Color.FromRgb(223, 210, 189);
        table.AddColumn(Unit.FromInch(1.25));
        table.AddColumn(Unit.FromInch(3.75));

        foreach (var detail in details)
        {
            var row = table.AddRow();
            row.Cells[0].Shading.Color = Color.FromRgb(234, 243, 248);
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].AddParagraph(detail.Label);
            row.Cells[1].AddParagraph(detail.Value);
        }

        table.Format.SpaceAfter = Unit.FromPoint(3);
    }

    private static void AddTable(Section section, ReportPdfTable reportTable)
    {
        if (reportTable.Headers.Count != reportTable.ColumnWidths.Count)
        {
            throw new ArgumentException("PDF table headers and column widths must have the same count.");
        }

        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.4);
        table.Borders.Color = Color.FromRgb(210, 210, 210);
        table.Format.Font.Size = 7;

        foreach (var width in reportTable.ColumnWidths)
        {
            table.AddColumn(Unit.FromInch(width));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Colors.White;
        header.Shading.Color = Color.FromRgb(23, 79, 115);
        AddCells(header, reportTable.Headers);

        for (var index = 0; index < reportTable.Rows.Count; index++)
        {
            var row = table.AddRow();
            if (index % 2 == 1)
            {
                row.Shading.Color = Color.FromRgb(255, 250, 241);
            }
            AddCells(row, reportTable.Rows[index]);
        }

        if (reportTable.FooterRow is not null)
        {
            var footer = table.AddRow();
            footer.Format.Font.Bold = true;
            footer.Shading.Color = Color.FromRgb(241, 223, 182);
            AddCells(footer, reportTable.FooterRow);
        }
    }

    private static void AddCells(Row row, IReadOnlyList<string> values)
    {
        for (var index = 0; index < row.Cells.Count; index++)
        {
            row.Cells[index].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[index].AddParagraph(index < values.Count ? values[index] : string.Empty);
        }
    }
}
