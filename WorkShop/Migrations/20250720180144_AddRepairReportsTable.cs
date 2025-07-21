using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepairReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ErrorKeyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedFix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsedParts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicianName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepairedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairReports_device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairReports_DeviceId",
                table: "RepairReports",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairReports");
        }
    }
}
