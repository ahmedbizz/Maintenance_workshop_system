using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class Add_in_MaintenanceCard_Colms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedByEngineerAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToTechnicianAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RepairedAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SparePartsRequestedAt",
                table: "maintenanceCards",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByEngineerAt",
                table: "maintenanceCards");

            migrationBuilder.DropColumn(
                name: "AssignedToTechnicianAt",
                table: "maintenanceCards");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "maintenanceCards");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "maintenanceCards");

            migrationBuilder.DropColumn(
                name: "RepairedAt",
                table: "maintenanceCards");

            migrationBuilder.DropColumn(
                name: "SparePartsRequestedAt",
                table: "maintenanceCards");
        }
    }
}
