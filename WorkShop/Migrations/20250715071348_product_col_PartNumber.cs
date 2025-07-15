using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class product_col_PartNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerialNumber",
                table: "products",
                newName: "PartNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartNumber",
                table: "products",
                newName: "SerialNumber");
        }
    }
}
