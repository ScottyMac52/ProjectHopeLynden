using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[Migration("20260716054500_PreserveInventoryHistoryClassification")]
public partial class PreserveInventoryHistoryClassification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ItemIdAtCount",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "ItemNameAtCount",
            table: "InventoryCountHistory",
            type: "TEXT",
            maxLength: 150,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "CategoryIdAtCount",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "CategoryNameAtCount",
            table: "InventoryCountHistory",
            type: "TEXT",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "LocationIdAtCount",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "LocationNameAtCount",
            table: "InventoryCountHistory",
            type: "TEXT",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "IsCommodityAtCount",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE InventoryCountHistory
            SET ItemIdAtCount = (
                    SELECT InventoryEntries.ItemId
                    FROM InventoryEntries
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                ItemNameAtCount = (
                    SELECT Items.Name
                    FROM InventoryEntries
                    INNER JOIN Items ON Items.Id = InventoryEntries.ItemId
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                CategoryIdAtCount = (
                    SELECT InventoryEntries.CategoryId
                    FROM InventoryEntries
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                CategoryNameAtCount = (
                    SELECT Categories.Name
                    FROM InventoryEntries
                    INNER JOIN Categories ON Categories.Id = InventoryEntries.CategoryId
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                LocationIdAtCount = (
                    SELECT InventoryEntries.LocationId
                    FROM InventoryEntries
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                LocationNameAtCount = (
                    SELECT Locations.Name
                    FROM InventoryEntries
                    INNER JOIN Locations ON Locations.Id = InventoryEntries.LocationId
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                ),
                IsCommodityAtCount = (
                    SELECT InventoryEntries.IsCommodity
                    FROM InventoryEntries
                    WHERE InventoryEntries.Id = InventoryCountHistory.InventoryEntryId
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ItemIdAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "ItemNameAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "CategoryIdAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "CategoryNameAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "LocationIdAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "LocationNameAtCount", table: "InventoryCountHistory");
        migrationBuilder.DropColumn(name: "IsCommodityAtCount", table: "InventoryCountHistory");
    }
}
