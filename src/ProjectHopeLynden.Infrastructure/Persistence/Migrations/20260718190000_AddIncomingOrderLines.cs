using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[Migration("20260718190000_AddIncomingOrderLines")]
public partial class AddIncomingOrderLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IncomingOrderLines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                InventoryEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<double>(type: "REAL", nullable: false),
                ExpectedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IncomingOrderLines", order => order.Id);
                table.CheckConstraint("CK_IncomingOrderLines_Quantity_Positive", "Quantity > 0");
                table.ForeignKey(
                    name: "FK_IncomingOrderLines_InventoryEntries_InventoryEntryId",
                    column: order => order.InventoryEntryId,
                    principalTable: "InventoryEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrderLines_InventoryEntryId_Status",
            table: "IncomingOrderLines",
            columns: new[] { "InventoryEntryId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_IncomingOrderLines_Status_ExpectedDate",
            table: "IncomingOrderLines",
            columns: new[] { "Status", "ExpectedDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IncomingOrderLines");
    }
}
