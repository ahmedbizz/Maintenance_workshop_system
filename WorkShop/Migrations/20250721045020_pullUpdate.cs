using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class pullUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "RepairReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RepairReports_ProductId",
                table: "RepairReports",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairReports_products_ProductId",
                table: "RepairReports",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepairReports_products_ProductId",
                table: "RepairReports");

            migrationBuilder.DropIndex(
                name: "IX_RepairReports_ProductId",
                table: "RepairReports");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "RepairReports");
        }
    }
}
