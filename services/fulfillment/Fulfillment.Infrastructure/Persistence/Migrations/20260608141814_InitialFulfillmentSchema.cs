using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFulfillmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_created_at",
                table: "shipments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_order_id",
                table: "shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_status",
                table: "shipments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipments");
        }
    }
}
