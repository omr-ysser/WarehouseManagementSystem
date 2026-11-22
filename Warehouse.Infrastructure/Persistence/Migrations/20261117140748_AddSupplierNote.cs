using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierDocuments_SupplierId_Type",
                table: "SupplierDocuments");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoteCreatedAtUtc",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoteCreatedByAccountId",
                table: "Suppliers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "SupplierDocuments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAtUtc",
                table: "SupplierDocuments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UploadedByAccountId",
                table: "SupplierDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDocuments_SupplierId_Type",
                table: "SupplierDocuments",
                columns: new[] { "SupplierId", "Type" },
                unique: true,
                filter: "[SupplierId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierDocuments_SupplierId_Type",
                table: "SupplierDocuments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NoteCreatedAtUtc",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NoteCreatedByAccountId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UploadedAtUtc",
                table: "SupplierDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedByAccountId",
                table: "SupplierDocuments");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "SupplierDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDocuments_SupplierId_Type",
                table: "SupplierDocuments",
                columns: new[] { "SupplierId", "Type" },
                unique: true);
        }
    }
}
