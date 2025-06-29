using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class storedI_Table_SperpartItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "SparePartItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SparePartItem_StoreId",
                table: "SparePartItem",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_SparePartItem_stores_StoreId",
                table: "SparePartItem",
                column: "StoreId",
                principalTable: "stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SparePartItem_stores_StoreId",
                table: "SparePartItem");

            migrationBuilder.DropIndex(
                name: "IX_SparePartItem_StoreId",
                table: "SparePartItem");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SparePartItem");
        }
    }
}
