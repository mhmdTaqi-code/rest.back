using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDiningSystem.Infrastructure.Data;

#nullable disable

namespace SmartDiningSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260404183500_StoreOrderStatusAsInteger")]
    public partial class StoreOrderStatusAsInteger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Orders'
                          AND column_name = 'Status'
                          AND data_type IN ('character varying', 'text')
                    ) THEN
                        IF EXISTS (
                            SELECT 1
                            FROM "Orders"
                            WHERE lower(btrim("Status")) NOT IN ('orderreceived', 'received', 'preparing', 'ready', 'served')
                        ) THEN
                            RAISE EXCEPTION 'Orders.Status contains unsupported values and cannot be converted safely.';
                        END IF;

                        ALTER TABLE "Orders"
                        ALTER COLUMN "Status" TYPE integer
                        USING CASE
                            WHEN lower(btrim("Status")) IN ('orderreceived', 'received') THEN 0
                            WHEN lower(btrim("Status")) = 'preparing' THEN 1
                            WHEN lower(btrim("Status")) = 'ready' THEN 2
                            WHEN lower(btrim("Status")) = 'served' THEN 3
                        END;
                    END IF;
                END
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Orders'
                          AND column_name = 'Status'
                          AND data_type = 'integer'
                    ) THEN
                        ALTER TABLE "Orders"
                        ALTER COLUMN "Status" TYPE character varying(32)
                        USING CASE "Status"
                            WHEN 0 THEN 'OrderReceived'
                            WHEN 1 THEN 'Preparing'
                            WHEN 2 THEN 'Ready'
                            WHEN 3 THEN 'Served'
                            ELSE 'OrderReceived'
                        END;
                    END IF;
                END
                $$;
                """);
        }
    }
}
