using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[Migration("20260711202000_SupportFractionalInventoryQuantities")]
public partial class SupportFractionalInventoryQuantities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<double>(
            name: "CurrentQuantity",
            table: "InventoryEntries",
            type: "REAL",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<double>(
            name: "CountedQuantity",
            table: "InventoryCountHistory",
            type: "REAL",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER");

        migrationBuilder.AlterColumn<double>(
            name: "PreviousQuantity",
            table: "InventoryCountHistory",
            type: "REAL",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AlterColumn<double>(
            name: "QuantityChange",
            table: "InventoryCountHistory",
            type: "REAL",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "CurrentQuantity",
            table: "InventoryEntries",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(double),
            oldType: "REAL");

        migrationBuilder.AlterColumn<int>(
            name: "CountedQuantity",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(double),
            oldType: "REAL");

        migrationBuilder.AlterColumn<int>(
            name: "PreviousQuantity",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(double),
            oldType: "REAL",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "QuantityChange",
            table: "InventoryCountHistory",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(double),
            oldType: "REAL",
            oldNullable: true);
    }
}
