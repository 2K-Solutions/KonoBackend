using Kono.Identity.Domain.Owners;
using Kono.Restaurants.Domain;
using Kono.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Kono.Orders.Domain;
using Kono.Menu.Domain;

namespace Kono.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(KonoDbContext context, string? adminEmail, string? adminPassword)
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
            var owner1 = new Owner
            {
                Id = Guid.NewGuid(),
                Email = "owner@kono.app",
                Password = BCrypt.Net.BCrypt.HashPassword("12345678"),
                FirstName = "Kono",
                SecondName = "Owner",
                PhoneNumber = "+12345678",
                IsActive = true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };
            
            context.Owners.Add(owner1);

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                context.Owners.Add(new Owner
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    FirstName = "Kono",
                    SecondName = "Admin",
                    PhoneNumber = "+12345678",
                    IsActive = true,
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });
            }

            context.Restaurants.AddRange(
                new Restaurant
                {
                    Id = mainRestaurantId,
                    OwnerId = owner1.Id,
                    RestaurantName = "Kono Rijeka",
                    City = "Rijeka",
                    Address = "Example Address 1",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                },
                new Restaurant
                {
                    Id = Guid.NewGuid(),
                    OwnerId = owner1.Id,
                    RestaurantName = "Kono Zagreb",
                    City = "Zagreb",
                    Address = "Example Address 2",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });

        }
        await context.SaveChangesAsync();

        if(!await context.MenuItem.AnyAsync())
        {
            var menuRestaurantId = await context.Restaurants
                .Where(r => r.RestaurantName == "Kono Rijeka")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (menuRestaurantId != Guid.Empty)
            {
                context.MenuItem.AddRange(
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        Price = 10.99m,
                        IsDrink = false,
                        Name = "Margherita"
                    },
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        Price = 12.00m,
                        IsDrink = false,
                        Name = "Cevapi"
                    },
                    new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = menuRestaurantId,
                        Price = 2.50m,
                        IsDrink = true,
                        Name = "Coca Cola"
                    }

                );
            }
        }
        

        await context.SaveChangesAsync();
    }
}
