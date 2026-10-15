using Kono.Infrastructure.Persistence;
using Kono.Infrastructure.Auth.Services;
using Kono.Infrastructure.Restaurants.Services;
using Kono.Infrastructure.Auth.Repositories;
using Kono.Infrastructure.Repositories.RestaurantRepository;
using Kono.Identity.Repositories;
using Kono.Restaurants.Repositories;
using Kono.Menu.Repositories;
using Kono.Orders.Repositories;
using Kono.Infrastructure.Orders.Repositories;
using Kono.Infrastructure.Services.MenuItems;
using Kono.Infrastructure.Services.Orders;
using Kono.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

// Ensure Npgsql treats DateTime like legacy behavior to avoid Kind mismatch errors
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Local dev: load secrets from a .env file (searched upward from the working directory).
// Variables already set in the real environment win, so production config is never overridden.
var dotEnvDir = LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Kono API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Description = "Enter your valid JWT token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", bearerScheme);

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", _),
            new List<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is not configured");
builder.Services.AddDbContext<KonoDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add JWT authentication
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt__SigningKey is not configured");
var key = Encoding.ASCII.GetBytes(jwtSigningKey);

builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

var firebasePath = builder.Configuration["Firebase_CredentialsPath"];
if (!string.IsNullOrWhiteSpace(firebasePath))
{
    if (!Path.IsPathRooted(firebasePath) && dotEnvDir != null)
        firebasePath = Path.GetFullPath(Path.Combine(dotEnvDir, firebasePath));

    FirebaseApp.Create(new AppOptions
    {
        Credential = CredentialFactory.FromFile<ServiceAccountCredential>(firebasePath).ToGoogleCredential()
    });
}

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOwnerRepository, OwnerRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepo>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IFoodStatsRepository, FoodStatsRepository>();
builder.Services.AddScoped<IOrdersRepository, OrderRepository>();
builder.Services.AddScoped<UserServices>();
builder.Services.AddScoped<MainRestaurantServices>();
builder.Services.AddScoped<MenuItemService>();
builder.Services.AddScoped<MainOrderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<RestaurantInviteCleanupService>();
builder.Services.AddControllers();

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    var adminEmail = builder.Configuration["Admin:Emails"]?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault();
    var adminPassword = builder.Configuration["Admin:Password"];
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<KonoDbContext>();
        try
        {
            await DataSeeder.SeedAsync(context, adminEmail, adminPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: seeding failed: {ex.Message}");
        }
    }
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok("Yurr"));

app.MapControllers();

app.Run();

// Returns the folder containing .env (or null), so relative paths in it can be resolved from there.
static string? LoadDotEnv()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".env")))
        dir = dir.Parent;
    if (dir == null) return null;

    foreach (var rawLine in File.ReadAllLines(Path.Combine(dir.FullName, ".env")))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;

        var separator = line.IndexOf('=');
        if (separator <= 0) continue;

        var name = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"');

        if (Environment.GetEnvironmentVariable(name) == null)
            Environment.SetEnvironmentVariable(name, value);
    }

    return dir.FullName;
}
