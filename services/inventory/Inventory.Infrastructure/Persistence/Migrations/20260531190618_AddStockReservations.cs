using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                    table.CheckConstraint("ck_stock_reservations_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_stock_reservations_storage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_location_id",
                table: "stock_reservations",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_reference",
                table: "stock_reservations",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_sku",
                table: "stock_reservations",
                column: "sku");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_sku_warehouse_id_location_id_status",
                table: "stock_reservations",
                columns: new[] { "sku", "warehouse_id", "location_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_status",
                table: "stock_reservations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_warehouse_id",
                table: "stock_reservations",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_reservations");
        }
    }
}
