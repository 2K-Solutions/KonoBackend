namespace KonoInfrastructure.Contracts.Restaurants;

public enum MembershipError { None, NotFound, BadRequest, Forbidden }

public sealed record MembershipResponse(Guid Id, Guid? RestaurantId);

public sealed record MembershipResult(MembershipResponse? Response, MembershipError Error = MembershipError.None, string? Message = null)
{
    public static MembershipResult Ok(Guid userId, Guid? restaurantId) => new(new MembershipResponse(userId, restaurantId));
    public static MembershipResult Fail(MembershipError error, string message) => new(null, error, message);
}
