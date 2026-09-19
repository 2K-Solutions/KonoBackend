using Kono.Identity.Domain.Owners;
using Kono.Identity.Domain.Restaurants;
using Kono.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(KonoDbContext context)
    {
        await context.Database.MigrateAsync();

        var mainRestaurantId = Guid.NewGuid();

        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new User
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = mainRestaurantId,
                    Email = "user@kono.app",
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Username = "kono_user",
                    FirstName = "Kono",
                    SecondName = "User",
                    PhoneNumber = "+1234567890",
                    UserRole = UserRole.Waiter,
                    MobilePhoneType = "Android",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = null,
                    Email = "mobile@kono.app",
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Username = "mobile_user",
                    FirstName = "Mobile",
                    SecondName = "Client",
                    PhoneNumber = "+1987654321",
                    UserRole = UserRole.Waiter,
                    MobilePhoneType = "iOS",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });
        }

        if (!await context.Owners.AnyAsync())
        {
            var owner = new Owner
            {
                Id = Guid.NewGuid(),
                Email = "owner@kono.app",
                Password = BCrypt.Net.BCrypt.HashPassword("OwnerPass123!"),
                FirstName = "Kono",
                SecondName = "Owner",
                PhoneNumber = "+12345678",
                IsActive = true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };
            context.Owners.Add(owner);

            context.Restaurants.AddRange(
                new Restaurant
                {
                    Id = mainRestaurantId,
                    OwnerId = owner.Id,
                    RestaurantName = "Kono Rijeka",
                    City = "Rijeka",
                    Address = "Example Address 1",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                },
                new Restaurant
                {
                    Id = Guid.NewGuid(),
                    OwnerId = owner.Id,
                    RestaurantName = "Kono Zagreb",
                    City = "Zagreb",
                    Address = "Example Address 2",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });

        }
        if(!await context.MenuItems.AnyAsync())
        {
            var menuRestaurantId = await context.Restaurants
                .Where(r => r.RestaurantName == "Kono Rijeka")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (menuRestaurantId != Guid.Empty)
            {
                context.MenuItems.AddRange(
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        IsDrink = false,
                        Name = "Margherita"
                    },
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        IsDrink = false,
                        Name = "Cevapi"
                    },
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        IsDrink = true,
                        Name = "Coca Cola"
                    }

                );
            }
        }
        

        await context.SaveChangesAsync();
    }
}
