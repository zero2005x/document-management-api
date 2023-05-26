using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentLibrary2.Migrations
{
    /// <inheritdoc />
    public partial class AddShareTokenAndExpirationTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShareExpirationTime",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareExpirationTime",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "Documents");
        }
    }
}
