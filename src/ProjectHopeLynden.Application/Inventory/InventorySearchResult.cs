namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventorySearchResult(
    string SearchTerm,
    IReadOnlyList<InventorySearchRow> Rows)
{
    public bool HasResults => Rows.Count > 0;
}
