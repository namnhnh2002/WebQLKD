using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamIT.Business.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1CFinal2ActiveTableConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TableOrders_TableId",
                table: "TableOrders");

            migrationBuilder.CreateIndex(
                name: "IX_TableOrders_TableId",
                table: "TableOrders",
                column: "TableId",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TableOrders_TableId",
                table: "TableOrders");

            migrationBuilder.CreateIndex(
                name: "IX_TableOrders_TableId",
                table: "TableOrders",
                column: "TableId");
        }
    }
}
