using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_RestaurantID",
                table: "Users",
                column: "RestaurantID");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_RestaurantId",
                table: "Tables",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_OwnerId",
                table: "Restaurants",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantInvites_OwnerId",
                table: "RestaurantInvites",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantInvites_RestaurantId",
                table: "RestaurantInvites",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantInvites_UserId",
                table: "RestaurantInvites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInfo_MenuItemId",
                table: "OrderInfo",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInfo_OrderId",
                table: "OrderInfo",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_RestaurantId",
                table: "MenuItem",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodStatistics_MenuItemId",
                table: "FoodStatistics",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DrinkStatistics_MenuItemId",
                table: "DrinkStatistics",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentOrders_RestaurantId",
                table: "CurrentOrders",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentOrders_TableId",
                table: "CurrentOrders",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentOrders_UserId",
                table: "CurrentOrders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrentOrders_Restaurants_RestaurantId",
                table: "CurrentOrders",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrentOrders_Tables_TableId",
                table: "CurrentOrders",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrentOrders_Users_UserId",
                table: "CurrentOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DrinkStatistics_MenuItem_MenuItemId",
                table: "DrinkStatistics",
                column: "MenuItemId",
                principalTable: "MenuItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodStatistics_MenuItem_MenuItemId",
                table: "FoodStatistics",
                column: "MenuItemId",
                principalTable: "MenuItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItem_Restaurants_RestaurantId",
                table: "MenuItem",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderInfo_CurrentOrders_OrderId",
                table: "OrderInfo",
                column: "OrderId",
                principalTable: "CurrentOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderInfo_MenuItem_MenuItemId",
                table: "OrderInfo",
                column: "MenuItemId",
                principalTable: "MenuItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantInvites_Owners_OwnerId",
                table: "RestaurantInvites",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantInvites_Restaurants_RestaurantId",
                table: "RestaurantInvites",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantInvites_Users_UserId",
                table: "RestaurantInvites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Owners_OwnerId",
                table: "Restaurants",
                column: "OwnerId",
                principalTable: "Owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Restaurants_RestaurantId",
                table: "Tables",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_RestaurantID",
                table: "Users",
                column: "RestaurantID",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrentOrders_Restaurants_RestaurantId",
                table: "CurrentOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrentOrders_Tables_TableId",
                table: "CurrentOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrentOrders_Users_UserId",
                table: "CurrentOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_DrinkStatistics_MenuItem_MenuItemId",
                table: "DrinkStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodStatistics_MenuItem_MenuItemId",
                table: "FoodStatistics");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItem_Restaurants_RestaurantId",
                table: "MenuItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderInfo_CurrentOrders_OrderId",
                table: "OrderInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderInfo_MenuItem_MenuItemId",
                table: "OrderInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantInvites_Owners_OwnerId",
                table: "RestaurantInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantInvites_Restaurants_RestaurantId",
                table: "RestaurantInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantInvites_Users_UserId",
                table: "RestaurantInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Owners_OwnerId",
                table: "Restaurants");

            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Restaurants_RestaurantId",
                table: "Tables");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_RestaurantID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RestaurantID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tables_RestaurantId",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_OwnerId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantInvites_OwnerId",
                table: "RestaurantInvites");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantInvites_RestaurantId",
                table: "RestaurantInvites");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantInvites_UserId",
                table: "RestaurantInvites");

            migrationBuilder.DropIndex(
                name: "IX_OrderInfo_MenuItemId",
                table: "OrderInfo");

            migrationBuilder.DropIndex(
                name: "IX_OrderInfo_OrderId",
                table: "OrderInfo");

            migrationBuilder.DropIndex(
                name: "IX_MenuItem_RestaurantId",
                table: "MenuItem");

            migrationBuilder.DropIndex(
                name: "IX_FoodStatistics_MenuItemId",
                table: "FoodStatistics");

            migrationBuilder.DropIndex(
                name: "IX_DrinkStatistics_MenuItemId",
                table: "DrinkStatistics");

            migrationBuilder.DropIndex(
                name: "IX_CurrentOrders_RestaurantId",
                table: "CurrentOrders");

            migrationBuilder.DropIndex(
                name: "IX_CurrentOrders_TableId",
                table: "CurrentOrders");

            migrationBuilder.DropIndex(
                name: "IX_CurrentOrders_UserId",
                table: "CurrentOrders");
        }
    }
}
