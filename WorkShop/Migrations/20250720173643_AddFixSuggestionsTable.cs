using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class AddFixSuggestionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ErrorKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedFix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedSpareParts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixSuggestions_device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixSuggestions_DeviceId",
                table: "FixSuggestions",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixSuggestions");
        }
    }
}
