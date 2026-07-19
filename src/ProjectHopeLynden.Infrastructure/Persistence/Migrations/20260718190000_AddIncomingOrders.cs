using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[Migration("20260718190000_AddIncomingOrders")]
public partial class AddIncomingOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IncomingOrders",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Vendor = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                InvoiceAmount = table.Column<double>(type: "REAL", nullable: true),
                DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                SentToPayer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ChargeTo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ExpectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Weight = table.Column<double>(type: "REAL", nullable: true),
                ProductSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncomingOrders", x => x.Id);
                table.CheckConstraint("CK_IncomingOrders_InvoiceAmount_NonNegative", "InvoiceAmount IS NULL OR InvoiceAmount >= 0");
                table.CheckConstraint("CK_IncomingOrders_Weight_NonNegative", "Weight IS NULL OR Weight >= 0");
            });

        migrationBuilder.CreateTable(
            name: "IncomingOrderLines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                IncomingOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                InventoryEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                ExpectedQuantity = table.Column<double>(type: "REAL", nullable: false),
                ReceivedQuantity = table.Column<double>(type: "REAL", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncomingOrderLines", x => x.Id);
                table.CheckConstraint("CK_IncomingOrderLines_ExpectedQuantity_Positive", "ExpectedQuantity > 0");
                table.CheckConstraint("CK_IncomingOrderLines_ReceivedQuantity_Positive", "ReceivedQuantity IS NULL OR ReceivedQuantity > 0");
                table.ForeignKey(
                    name: "FK_IncomingOrderLines_IncomingOrders_IncomingOrderId",
                    column: x => x.IncomingOrderId,
                    principalTable: "IncomingOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IncomingOrderLines_InventoryEntries_InventoryEntryId",
                    column: x => x.InventoryEntryId,
                    principalTable: "InventoryEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrderLines_IncomingOrderId",
            table: "IncomingOrderLines",
            column: "IncomingOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrderLines_IncomingOrderId_InventoryEntryId",
            table: "IncomingOrderLines",
            columns: new[] { "IncomingOrderId", "InventoryEntryId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrderLines_InventoryEntryId",
            table: "IncomingOrderLines",
            column: "InventoryEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrders_Status_ExpectedDate",
            table: "IncomingOrders",
            columns: new[] { "Status", "ExpectedDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IncomingOrderLines");
        migrationBuilder.DropTable(name: "IncomingOrders");
    }
}
