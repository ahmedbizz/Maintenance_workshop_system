using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class change_userID_To_String : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_userGroups_AspNetUsers_UserId1",
                table: "userGroups");

            migrationBuilder.DropIndex(
                name: "IX_userGroups_UserId1",
                table: "userGroups");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "userGroups");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "userGroups",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_userGroups_UserId",
                table: "userGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_userGroups_AspNetUsers_UserId",
                table: "userGroups",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_userGroups_AspNetUsers_UserId",
                table: "userGroups");

            migrationBuilder.DropIndex(
                name: "IX_userGroups_UserId",
                table: "userGroups");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "userGroups",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "userGroups",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_userGroups_UserId1",
                table: "userGroups",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_userGroups_AspNetUsers_UserId1",
                table: "userGroups",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
