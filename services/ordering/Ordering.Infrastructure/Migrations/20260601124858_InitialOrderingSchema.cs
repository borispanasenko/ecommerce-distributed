using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrderingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    customer_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_currency_length", "char_length(currency) = 3");
                    table.CheckConstraint("ck_orders_total_amount_minor_non_negative", "total_amount_minor >= 0");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    unit_price_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    line_total_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    inventory_reservation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.CheckConstraint("ck_order_items_currency_length", "char_length(currency) = 3");
                    table.CheckConstraint("ck_order_items_line_total_amount_minor_non_negative", "line_total_amount_minor >= 0");
                    table.CheckConstraint("ck_order_items_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_order_items_unit_price_amount_minor_non_negative", "unit_price_amount_minor >= 0");
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_inventory_reservation_id",
                table: "order_items",
                column: "inventory_reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_variant_id",
                table: "order_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_sku",
                table: "order_items",
                column: "sku");

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_email",
                table: "orders",
                column: "customer_email");

            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                table: "orders",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
