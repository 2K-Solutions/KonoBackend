using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kono.Infrastructure.Migrations;

public partial class FixPhoneNumberColumnTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "Owners",
            type: "varchar(100)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "Users",
            type: "varchar(100)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "PhoneNumber",
            table: "Owners",
            type: "integer",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(100)");

        migrationBuilder.AlterColumn<int>(
            name: "PhoneNumber",
            table: "Users",
            type: "integer",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(100)");
    }
}
