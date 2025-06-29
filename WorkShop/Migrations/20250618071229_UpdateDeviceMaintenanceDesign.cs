using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkShop.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeviceMaintenanceDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deviceLogs_products_productId",
                table: "deviceLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_ReceiverId",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_maintenanceCards_DeviceId",
                table: "maintenanceCards");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "notifications");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_ReceiverId",
                table: "notifications",
                newName: "IX_notifications_ReceiverId");

            migrationBuilder.RenameColumn(
                name: "productId",
                table: "deviceLogs",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_deviceLogs_productId",
                table: "deviceLogs",
                newName: "IX_deviceLogs_ProductId");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "deviceLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "deviceLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "deviceLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "deviceLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "deviceLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "deviceLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notifications",
                table: "notifications",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenanceCards_DeviceId",
                table: "maintenanceCards",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deviceLogs_DeviceId",
                table: "deviceLogs",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_deviceLogs_device_DeviceId",
                table: "deviceLogs",
                column: "DeviceId",
                principalTable: "device",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_deviceLogs_products_ProductId",
                table: "deviceLogs",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_AspNetUsers_ReceiverId",
                table: "notifications",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deviceLogs_device_DeviceId",
                table: "deviceLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_deviceLogs_products_ProductId",
                table: "deviceLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_AspNetUsers_ReceiverId",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notifications",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_maintenanceCards_DeviceId",
                table: "maintenanceCards");

            migrationBuilder.DropIndex(
                name: "IX_deviceLogs_DeviceId",
                table: "deviceLogs");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "deviceLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "deviceLogs");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "deviceLogs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "deviceLogs");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "deviceLogs");

            migrationBuilder.RenameTable(
                name: "notifications",
                newName: "Notifications");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_ReceiverId",
                table: "Notifications",
                newName: "IX_Notifications_ReceiverId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "deviceLogs",
                newName: "productId");

            migrationBuilder.RenameIndex(
                name: "IX_deviceLogs_ProductId",
                table: "deviceLogs",
                newName: "IX_deviceLogs_productId");

            migrationBuilder.AlterColumn<int>(
                name: "productId",
                table: "deviceLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenanceCards_DeviceId",
                table: "maintenanceCards",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_deviceLogs_products_productId",
                table: "deviceLogs",
                column: "productId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_ReceiverId",
                table: "Notifications",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
