using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentLibrary2.Migrations
{
    /// <inheritdoc />
    public partial class AddSasTokenAndSasExpirationTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "ShareExpirationTime",
                table: "Documents",
                newName: "SasExpirationTime");

            migrationBuilder.AddColumn<string>(
                name: "SasToken",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SasToken",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "SasExpirationTime",
                table: "Documents",
                newName: "ShareExpirationTime");

            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "Documents",
                type: "text",
                nullable: true);
        }
    }
}
