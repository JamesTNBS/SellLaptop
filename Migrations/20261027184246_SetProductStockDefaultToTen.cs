using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptop.Migrations
{
    /// <inheritdoc />
    public partial class SetProductStockDefaultToTen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("UPDATE [Products] SET [StockQuantity] = 10;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 10);
        }
    }
}
