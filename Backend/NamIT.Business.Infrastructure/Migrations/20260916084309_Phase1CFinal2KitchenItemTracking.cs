using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamIT.Business.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1CFinal2KitchenItemTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "KitchenOrderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderItems_OrderItemId",
                table: "KitchenOrderItems",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderItems_OrderItems_OrderItemId",
                table: "KitchenOrderItems",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderItems_OrderItems_OrderItemId",
                table: "KitchenOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_KitchenOrderItems_OrderItemId",
                table: "KitchenOrderItems");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "KitchenOrderItems");
        }
    }
}
