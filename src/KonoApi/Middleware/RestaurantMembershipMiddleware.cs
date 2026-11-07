using System.Security.Claims;
using Kono.Identity.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace KonoApi.Middleware;

public class RestaurantMembershipMiddleware
{
    public const string StaleTokenCode = "restaurant_membership_changed";

    private readonly RequestDelegate _next;

    public RestaurantMembershipMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        var accountType = context.User.FindFirst("accountType")?.Value;

        if ((accountType != "worker" && accountType != "unemployed") || !RequiresAuthorization(context))
        {
            await _next(context);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            await _next(context);
            return;
        }

        Guid? claimRestaurantId = Guid.TryParse(context.User.FindFirst("restaurantId")?.Value, out var rid) ? rid : null;

        var user = await userRepository.GetByIdAsync(userId);
        var expectedAccountType = user?.RestaurantId == null ? "unemployed" : "worker";

        if (user == null || user.RestaurantId != claimRestaurantId || accountType != expectedAccountType)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                code = StaleTokenCode,
                message = "Your restaurant membership has changed. Refresh your token."
            });
            return;
        }

        await _next(context);
    }

    private static bool RequiresAuthorization(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return false;
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null) return false;
        return endpoint.Metadata.GetMetadata<IAuthorizeData>() != null;
    }
}
