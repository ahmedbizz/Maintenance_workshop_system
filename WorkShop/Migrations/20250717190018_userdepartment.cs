using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class userdepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComingFromDepartmentId",
                table: "device",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EngineerId",
                table: "device",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FaultDescription",
                table: "device",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_device_EngineerId",
                table: "device",
                column: "EngineerId");

            migrationBuilder.AddForeignKey(
                name: "FK_device_AspNetUsers_EngineerId",
                table: "device",
                column: "EngineerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_AspNetUsers_EngineerId",
                table: "device");

            migrationBuilder.DropIndex(
                name: "IX_device_EngineerId",
                table: "device");

            migrationBuilder.DropColumn(
                name: "ComingFromDepartmentId",
                table: "device");

            migrationBuilder.DropColumn(
                name: "EngineerId",
                table: "device");

            migrationBuilder.DropColumn(
                name: "FaultDescription",
                table: "device");
        }
    }
}
