using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class ManagerId_in_device : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "managerId",
                table: "device",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_managerId",
                table: "device",
                column: "managerId");

            migrationBuilder.AddForeignKey(
                name: "FK_device_AspNetUsers_managerId",
                table: "device",
                column: "managerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_AspNetUsers_managerId",
                table: "device");

            migrationBuilder.DropIndex(
                name: "IX_device_managerId",
                table: "device");

            migrationBuilder.DropColumn(
                name: "managerId",
                table: "device");
        }
    }
}
