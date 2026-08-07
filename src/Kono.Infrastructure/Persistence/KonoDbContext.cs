using Kono.Identity.Domain.Owners;
using Kono.Identity.Domain.RefreshTokens;
using Kono.Identity.Domain.Restaurants;
using Kono.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kono.Infrastructure.Persistence;

public class KonoDbContext : DbContext
{
    public KonoDbContext(DbContextOptions<KonoDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RestaurantId)
                .HasColumnName("RestaurantID")
                .HasColumnType("uuid");

            entity.Property(e => e.Email)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.Password)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.Username)
                .HasColumnType("varchar(50)")
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.SecondName)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasColumnType("varchar(100)")
                .IsRequired();

            entity.Property(e => e.UserRole)
                .HasColumnType("integer");

            entity.Property(e => e.MobilePhoneType)
                .HasColumnType("varchar(256)");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.ToTable("Owners");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.Password)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.SecondName)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasColumnType("varchar(100)")
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasColumnType("boolean");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.Token)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.ExpiryDate)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .HasColumnType("boolean")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.RevokedAt)
                .HasColumnType("timestamp with time zone");
         });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("Restaurants");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OwnerId)
                .HasColumnType("uuid")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnType("varchar(256)")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp with time zone");
        });
     }
 }