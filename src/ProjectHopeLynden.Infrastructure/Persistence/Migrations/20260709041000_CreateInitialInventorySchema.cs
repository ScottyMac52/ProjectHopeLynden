using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[ExcludeFromCodeCoverage]
[Migration("20260709041000_CreateInitialInventorySchema")]
public partial class CreateInitialInventorySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", category => category.Id);
            });

        migrationBuilder.CreateTable(
            name: "Items",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Items", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "Locations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Locations", location => location.Id);
            });

        migrationBuilder.CreateTable(
            name: "InventoryEntries",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                BestByDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                IsCommodity = table.Column<bool>(type: "INTEGER", nullable: false),
                IsMenuItem = table.Column<bool>(type: "INTEGER", nullable: false),
                LastUpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryEntries", entry => entry.Id);
                table.CheckConstraint("CK_InventoryEntries_CurrentQuantity_NonNegative", "CurrentQuantity >= 0");
                table.ForeignKey(
                    name: "FK_InventoryEntries_Categories_CategoryId",
                    column: entry => entry.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InventoryEntries_Items_ItemId",
                    column: entry => entry.ItemId,
                    principalTable: "Items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InventoryEntries_Locations_LocationId",
                    column: entry => entry.LocationId,
                    principalTable: "Locations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InventoryCountHistory",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InventoryEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                CountedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                CountedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                PreviousQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                QuantityChange = table.Column<int>(type: "INTEGER", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryCountHistory", history => history.Id);
                table.CheckConstraint("CK_InventoryCountHistory_CountedQuantity_NonNegative", "CountedQuantity >= 0");
                table.ForeignKey(
                    name: "FK_InventoryCountHistory_InventoryEntries_InventoryEntryId",
                    column: history => history.InventoryEntryId,
                    principalTable: "InventoryEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryCountHistory_InventoryEntryId_CountedAtUtc",
            table: "InventoryCountHistory",
            columns: ["InventoryEntryId", "CountedAtUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryEntries_CategoryId",
            table: "InventoryEntries",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryEntries_ItemId_IsCommodity",
            table: "InventoryEntries",
            columns: ["ItemId", "IsCommodity"]);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryEntries_LocationId",
            table: "InventoryEntries",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "IX_Items_Name",
            table: "Items",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Locations_Name",
            table: "Locations",
            column: "Name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InventoryCountHistory");
        migrationBuilder.DropTable(name: "InventoryEntries");
        migrationBuilder.DropTable(name: "Categories");
        migrationBuilder.DropTable(name: "Items");
        migrationBuilder.DropTable(name: "Locations");
    }
}
