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
        migrationBuilder.Sql(
            """
            CREATE TABLE "__InventoryEntries_new" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_InventoryEntries" PRIMARY KEY AUTOINCREMENT,
                "ItemId" INTEGER NOT NULL,
                "CategoryId" INTEGER NOT NULL,
                "LocationId" INTEGER NOT NULL,
                "CurrentQuantity" REAL NOT NULL,
                "BestByDate" TEXT NULL,
                "IsCommodity" INTEGER NOT NULL,
                "IsMenuItem" INTEGER NOT NULL,
                "LastUpdatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "CK_InventoryEntries_CurrentQuantity_NonNegative" CHECK ("CurrentQuantity" >= 0),
                CONSTRAINT "FK_InventoryEntries_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryEntries_Items_ItemId" FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryEntries_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT
            );

            INSERT INTO "__InventoryEntries_new" (
                "Id", "ItemId", "CategoryId", "LocationId", "CurrentQuantity", "BestByDate",
                "IsCommodity", "IsMenuItem", "LastUpdatedAtUtc")
            SELECT
                "Id", "ItemId", "CategoryId", "LocationId", CAST("CurrentQuantity" AS REAL), "BestByDate",
                "IsCommodity", "IsMenuItem", "LastUpdatedAtUtc"
            FROM "InventoryEntries";

            CREATE TABLE "__InventoryCountHistory_new" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_InventoryCountHistory" PRIMARY KEY AUTOINCREMENT,
                "InventoryEntryId" INTEGER NOT NULL,
                "CountedQuantity" REAL NOT NULL,
                "CountedAtUtc" TEXT NOT NULL,
                "PreviousQuantity" REAL NULL,
                "QuantityChange" REAL NULL,
                CONSTRAINT "CK_InventoryCountHistory_CountedQuantity_NonNegative" CHECK ("CountedQuantity" >= 0),
                CONSTRAINT "FK_InventoryCountHistory_InventoryEntries_InventoryEntryId"
                    FOREIGN KEY ("InventoryEntryId") REFERENCES "__InventoryEntries_new" ("Id") ON DELETE CASCADE
            );

            INSERT INTO "__InventoryCountHistory_new" (
                "Id", "InventoryEntryId", "CountedQuantity", "CountedAtUtc", "PreviousQuantity", "QuantityChange")
            SELECT
                "Id", "InventoryEntryId", CAST("CountedQuantity" AS REAL), "CountedAtUtc",
                CAST("PreviousQuantity" AS REAL), CAST("QuantityChange" AS REAL)
            FROM "InventoryCountHistory";

            DROP TABLE "InventoryCountHistory";
            DROP TABLE "InventoryEntries";

            ALTER TABLE "__InventoryEntries_new" RENAME TO "InventoryEntries";
            ALTER TABLE "__InventoryCountHistory_new" RENAME TO "InventoryCountHistory";

            CREATE INDEX "IX_InventoryEntries_CategoryId" ON "InventoryEntries" ("CategoryId");
            CREATE INDEX "IX_InventoryEntries_ItemId_IsCommodity" ON "InventoryEntries" ("ItemId", "IsCommodity");
            CREATE INDEX "IX_InventoryEntries_LocationId" ON "InventoryEntries" ("LocationId");
            CREATE INDEX "IX_InventoryCountHistory_InventoryEntryId_CountedAtUtc"
                ON "InventoryCountHistory" ("InventoryEntryId", "CountedAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "__InventoryEntries_old" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_InventoryEntries" PRIMARY KEY AUTOINCREMENT,
                "ItemId" INTEGER NOT NULL,
                "CategoryId" INTEGER NOT NULL,
                "LocationId" INTEGER NOT NULL,
                "CurrentQuantity" INTEGER NOT NULL,
                "BestByDate" TEXT NULL,
                "IsCommodity" INTEGER NOT NULL,
                "IsMenuItem" INTEGER NOT NULL,
                "LastUpdatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "CK_InventoryEntries_CurrentQuantity_NonNegative" CHECK ("CurrentQuantity" >= 0),
                CONSTRAINT "FK_InventoryEntries_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryEntries_Items_ItemId" FOREIGN KEY ("ItemId") REFERENCES "Items" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryEntries_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT
            );

            INSERT INTO "__InventoryEntries_old" (
                "Id", "ItemId", "CategoryId", "LocationId", "CurrentQuantity", "BestByDate",
                "IsCommodity", "IsMenuItem", "LastUpdatedAtUtc")
            SELECT
                "Id", "ItemId", "CategoryId", "LocationId", CAST("CurrentQuantity" AS INTEGER), "BestByDate",
                "IsCommodity", "IsMenuItem", "LastUpdatedAtUtc"
            FROM "InventoryEntries";

            CREATE TABLE "__InventoryCountHistory_old" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_InventoryCountHistory" PRIMARY KEY AUTOINCREMENT,
                "InventoryEntryId" INTEGER NOT NULL,
                "CountedQuantity" INTEGER NOT NULL,
                "CountedAtUtc" TEXT NOT NULL,
                "PreviousQuantity" INTEGER NULL,
                "QuantityChange" INTEGER NULL,
                CONSTRAINT "CK_InventoryCountHistory_CountedQuantity_NonNegative" CHECK ("CountedQuantity" >= 0),
                CONSTRAINT "FK_InventoryCountHistory_InventoryEntries_InventoryEntryId"
                    FOREIGN KEY ("InventoryEntryId") REFERENCES "__InventoryEntries_old" ("Id") ON DELETE CASCADE
            );

            INSERT INTO "__InventoryCountHistory_old" (
                "Id", "InventoryEntryId", "CountedQuantity", "CountedAtUtc", "PreviousQuantity", "QuantityChange")
            SELECT
                "Id", "InventoryEntryId", CAST("CountedQuantity" AS INTEGER), "CountedAtUtc",
                CAST("PreviousQuantity" AS INTEGER), CAST("QuantityChange" AS INTEGER)
            FROM "InventoryCountHistory";

            DROP TABLE "InventoryCountHistory";
            DROP TABLE "InventoryEntries";

            ALTER TABLE "__InventoryEntries_old" RENAME TO "InventoryEntries";
            ALTER TABLE "__InventoryCountHistory_old" RENAME TO "InventoryCountHistory";

            CREATE INDEX "IX_InventoryEntries_CategoryId" ON "InventoryEntries" ("CategoryId");
            CREATE INDEX "IX_InventoryEntries_ItemId_IsCommodity" ON "InventoryEntries" ("ItemId", "IsCommodity");
            CREATE INDEX "IX_InventoryEntries_LocationId" ON "InventoryEntries" ("LocationId");
            CREATE INDEX "IX_InventoryCountHistory_InventoryEntryId_CountedAtUtc"
                ON "InventoryCountHistory" ("InventoryEntryId", "CountedAtUtc");
            """);
    }
}
