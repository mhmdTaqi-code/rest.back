using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Data.Migrations
{
    public partial class RepairMenuSchemaForSeeding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "MenuCategories"
                (
                    "Id" uuid NOT NULL,
                    "RestaurantId" uuid NOT NULL,
                    "Name" character varying(150) NOT NULL,
                    "Description" character varying(1000) NULL,
                    "DisplayOrder" integer NOT NULL DEFAULT 0,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_MenuCategories" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_MenuCategories_Restaurants_RestaurantId"
                        FOREIGN KEY ("RestaurantId") REFERENCES "Restaurants" ("Id") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ADD COLUMN IF NOT EXISTS "MenuCategoryId" uuid NULL;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ADD COLUMN IF NOT EXISTS "ImageUrl" character varying(1000) NOT NULL DEFAULT '';
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ADD COLUMN IF NOT EXISTS "DisplayOrder" integer NOT NULL DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuCategories"
                ADD COLUMN IF NOT EXISTS "Description" character varying(1000) NULL;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuCategories"
                ALTER COLUMN "DisplayOrder" SET DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ALTER COLUMN "DisplayOrder" SET DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_MenuCategories_RestaurantId"
                ON "MenuCategories" ("RestaurantId");
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_MenuCategories_RestaurantId_Name"
                ON "MenuCategories" ("RestaurantId", "Name");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_MenuItems_MenuCategoryId"
                ON "MenuItems" ("MenuCategoryId");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_MenuItems_MenuCategories_MenuCategoryId') THEN
                        ALTER TABLE "MenuItems"
                        ADD CONSTRAINT "FK_MenuItems_MenuCategories_MenuCategoryId"
                        FOREIGN KEY ("MenuCategoryId") REFERENCES "MenuCategories" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                DROP CONSTRAINT IF EXISTS "CK_MenuItems_Price_Positive";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ADD CONSTRAINT "CK_MenuItems_Price_Positive"
                CHECK ("Price" >= 0);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                DROP CONSTRAINT IF EXISTS "CK_MenuItems_Price_Positive";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "MenuItems"
                ADD CONSTRAINT "CK_MenuItems_Price_Positive"
                CHECK ("Price" > 0);
                """);
        }
    }
}
