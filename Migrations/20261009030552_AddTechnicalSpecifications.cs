using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptop.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TechnicalSpecifications",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TechnicalSpecifications",
                table: "Products");
        }
    }
}
