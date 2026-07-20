using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectHopeLynden.Infrastructure.Persistence;

#nullable disable

namespace ProjectHopeLynden.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProjectHopeDbContext))]
[Migration("20260720200000_AddNormalizedLocationName")]
public partial class AddNormalizedLocationName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "Locations",
            type: "TEXT",
            nullable: true,
            computedColumnSql: "upper(trim(Name))",
            stored: false);

        migrationBuilder.CreateIndex(
            name: "IX_Locations_NormalizedName",
            table: "Locations",
            column: "NormalizedName",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Locations_NormalizedName",
            table: "Locations");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "Locations");
    }
}
