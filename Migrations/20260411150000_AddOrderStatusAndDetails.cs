using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptop.Migrations
{
    public partial class AddOrderStatusAndDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StateOrProvince",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AddressLine1", table: "Orders");
            migrationBuilder.DropColumn(name: "AddressLine2", table: "Orders");
            migrationBuilder.DropColumn(name: "City", table: "Orders");
            migrationBuilder.DropColumn(name: "Email", table: "Orders");
            migrationBuilder.DropColumn(name: "FullName", table: "Orders");
            migrationBuilder.DropColumn(name: "Notes", table: "Orders");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Orders");
            migrationBuilder.DropColumn(name: "Phone", table: "Orders");
            migrationBuilder.DropColumn(name: "PostalCode", table: "Orders");
            migrationBuilder.DropColumn(name: "StateOrProvince", table: "Orders");
            migrationBuilder.DropColumn(name: "Status", table: "Orders");
        }
    }
}
