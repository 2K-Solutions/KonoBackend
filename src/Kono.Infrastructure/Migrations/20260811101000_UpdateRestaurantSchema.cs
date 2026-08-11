using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kono.Infrastructure.Migrations;

public partial class UpdateRestaurantSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Name",
            table: "Restaurants",
            newName: "RestaurantName");

        migrationBuilder.AddColumn<string>(
            name: "City",
            table: "Restaurants",
            type: "varchar(256)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Address",
            table: "Restaurants",
            type: "varchar(256)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Restaurants",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "City",
            table: "Restaurants");

        migrationBuilder.DropColumn(
            name: "Address",
            table: "Restaurants");

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "Restaurants");

        migrationBuilder.RenameColumn(
            name: "RestaurantName",
            table: "Restaurants",
            newName: "Name");
    }
}