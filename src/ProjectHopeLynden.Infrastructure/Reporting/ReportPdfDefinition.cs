namespace ProjectHopeLynden.Infrastructure.Reporting;

public sealed record ReportPdfDefinition(
    string Title,
    DateTime GeneratedAtUtc,
    bool Landscape,
    IReadOnlyList<ReportPdfDetail> Details,
    IReadOnlyList<ReportPdfSection> Sections);

public sealed record ReportPdfDetail(string Label, string Value);

public sealed record ReportPdfSection(
    string Heading,
    string? EmptyMessage,
    ReportPdfTable? Table);

public sealed record ReportPdfTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<double> ColumnWidths,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<string>? FooterRow = null);
