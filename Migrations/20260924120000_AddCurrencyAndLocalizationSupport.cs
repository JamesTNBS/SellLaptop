using Microsoft.EntityFrameworkCore.Migrations;
using Laptop.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Laptop.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260924120000_AddCurrencyAndLocalizationSupport")]
    public partial class AddCurrencyAndLocalizationSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Currency", table: "Products", type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "USD");
            migrationBuilder.AddColumn<string>(name: "Currency", table: "CartItems", type: "nvarchar(max)", nullable: false, defaultValue: "USD");
            migrationBuilder.AddColumn<string>(name: "Currency", table: "OrderItems", type: "nvarchar(max)", nullable: false, defaultValue: "USD");
            migrationBuilder.AddColumn<string>(name: "Currency", table: "Orders", type: "nvarchar(max)", nullable: false, defaultValue: "USD");
            migrationBuilder.AddColumn<decimal>(name: "ExchangeRate", table: "Orders", type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Currency", table: "Products");
            migrationBuilder.DropColumn(name: "Currency", table: "CartItems");
            migrationBuilder.DropColumn(name: "Currency", table: "OrderItems");
            migrationBuilder.DropColumn(name: "Currency", table: "Orders");
            migrationBuilder.DropColumn(name: "ExchangeRate", table: "Orders");
        }
    }
}
