using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateUsersAndOwnersTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "Owners",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Owners",
                newName: "OwnerID");
        }
    }
}
