using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentLibrary2.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SasToken",
                table: "Documents",
                nullable: false);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
