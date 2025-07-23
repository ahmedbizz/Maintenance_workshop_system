using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class ErrorKeyword_Device : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorKeyword",
                table: "device",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorKeyword",
                table: "device");
        }
    }
}
