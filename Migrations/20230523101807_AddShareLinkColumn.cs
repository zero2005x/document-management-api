using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentLibrary2.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinkColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareLink",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareLink",
                table: "Documents");
        }
    }
}
