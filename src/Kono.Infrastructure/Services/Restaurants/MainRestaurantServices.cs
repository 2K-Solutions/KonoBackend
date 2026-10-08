using Kono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kono.Restaurants.Domain;
using Kono.Restaurants.Repositories;
using Kono.Identity.Repositories;
using KonoInfrastructure.Contracts.Restaurants;
using KonoInfrastructure.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Kono.Infrastructure.Contracts.MenuItems;
using Kono.Menu.Domain;



namespace Kono.Infrastructure.Restaurants.Services;

public class MainRestaurantServices
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IOwnerRepository _ownerRepository;
    private readonly IUserRepository _userRepository;

    public MainRestaurantServices(KonoDbContext context,
        IRestaurantRepository restaurantRepository,
        IOwnerRepository ownerRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _ownerRepository = ownerRepository;
        _userRepository = userRepository;
    }

    

    public async Task<List<RestaurantBasic>> GetRestaurant(Guid ownerId)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByOwnerIdAsync(ownerId);

        return restaurants
            .Select(r => new RestaurantBasic(r.Id, r.RestaurantName, r.City, r.Address))
            .ToList();
    }

    public async Task<List<BasicUserInfo>> GetSomeAvailableUsersAsync([FromQuery] string query)
    {
        var availableUsers = await _restaurantRepository.GetSomeAvailableUsersAsync();

        return availableUsers
            .Select(u => new BasicUserInfo(u.Id, u.Email, u.Username, u.FirstName, u.SecondName))
            .Where(u => EF.Functions.ILike(u.Username, $"%{query}%"))
            .OrderBy(u => u.Username)
            .ToList();
    }

    public async Task<InviteResult> CheckUserInvites(Guid userId, Guid restaurantId)
    {
        var ownerId = await _restaurantRepository.GetOwnerIdByRestaurantIdAsync(restaurantId);
        if (ownerId == Guid.Empty) return new InviteResult { RestaurantSuccess = false, Message = "Restaurant not found" };

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant is null || restaurant.OwnerId != ownerId || restaurant.DeletedAt != null) return new InviteResult { RestaurantSuccess = false, Message = "Restaurant not found or you are not the owner" };


        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user is null) return new InviteResult { UserSuccess = false, Message = "User not found" };
        if (user.RestaurantId != null) return new InviteResult { UserSuccess = false, Message = "User already belongs to a restaurant" };

        var now = DateTime.UtcNow;

        var existingPendingInvite = await _context.RestaurantInvites
            .FirstOrDefaultAsync(i => i.UserId == userId
                && i.Status == RestaurantInviteStatus.Pending
                && i.RestaurantId == restaurantId
                && i.ExpiresAt > now);
        if (existingPendingInvite != null) return new InviteResult { PendingInviteSuccess = false, Message = "User already has a pending invite" };
        return new InviteResult { Message = "User is eligible for an invite" };
    }

    public async Task<RestaurantInvite> GenerateInvite(TimeSpan LifeTime, Guid userId, Guid restaurantId)
    {
        var now = DateTime.UtcNow;
        var invite = new RestaurantInvite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantId = restaurantId,
            Status = RestaurantInviteStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.Add(LifeTime) 
        };

        await _context.RestaurantInvites.AddAsync(invite);
        await _context.SaveChangesAsync();

        return invite;
    }

    public async Task<List<PendingInviteResponse>> GetPendingInvitesForUser(Guid userId)
    {
        var now = DateTime.UtcNow;

        var invites = await _context.RestaurantInvites
            .Where(i => i.UserId == userId && i.Status == RestaurantInviteStatus.Pending && i.ExpiresAt > now)
            .Join(_context.Restaurants, i => i.RestaurantId, r => r.Id, (i, r) =>
                new PendingInviteResponse(i.Id, i.UserId, i.RestaurantId, r.RestaurantName, i.ExpiresAt))
            .ToListAsync();
        
        return invites;
    }

    public async Task<List<PendingInviteResponse>> GetPendingInvitesForOwner(Guid ownerId, Guid restaurantId)
    {
        var now = DateTime.UtcNow;

        var invites = await _context.RestaurantInvites
            .Where(i => i.RestaurantId == restaurantId && i.Status == RestaurantInviteStatus.Pending && i.ExpiresAt > now)
            .Join(_context.Restaurants, i => i.RestaurantId, r => r.Id, (i, r) => new { Invite = i, Restaurant = r })
            .Where(ir => ir.Restaurant.OwnerId == ownerId && ir.Restaurant.DeletedAt == null)
            .Join(_context.Users, ir => ir.Invite.UserId, u => u.Id, (ir, u) =>
                new PendingInviteResponse(ir.Invite.Id, ir.Invite.UserId, ir.Invite.RestaurantId, ir.Restaurant.RestaurantName, ir.Invite.ExpiresAt))
            .ToListAsync();

        return invites;
    }

    public async Task<MembershipResult> AcceptInvite(Guid inviteId, Guid userId)
    {
        var invite = await _context.RestaurantInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == userId);
        if (invite is null) return MembershipResult.Fail(MembershipError.NotFound, "Invite not found");

        if (invite.Status != RestaurantInviteStatus.Pending)
            return MembershipResult.Fail(MembershipError.BadRequest, "Invite is no longer pending");

        var now = DateTime.UtcNow;
        if (invite.ExpiresAt <= now)
        {
            invite.Status = RestaurantInviteStatus.Expired;
            await _context.SaveChangesAsync();
            return MembershipResult.Fail(MembershipError.BadRequest, "Invite has expired");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return MembershipResult.Fail(MembershipError.NotFound, "User not found");
        if (user.RestaurantId != null) return MembershipResult.Fail(MembershipError.BadRequest, "User already belongs to a restaurant");

        user.RestaurantId = invite.RestaurantId;
        invite.Status = RestaurantInviteStatus.Accepted;
        invite.RespondedAt = now;

        await _context.SaveChangesAsync();

        return MembershipResult.Ok(user.Id, user.RestaurantId);
    }

    public async Task<MembershipResult> KickUser(Guid userId, Guid ownerId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return MembershipResult.Fail(MembershipError.NotFound, "User not found");
        if (user.RestaurantId is null) return MembershipResult.Fail(MembershipError.BadRequest, "User does not belong to a restaurant");

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(user.RestaurantId.Value);
        if (restaurant is null || restaurant.OwnerId != ownerId || restaurant.DeletedAt != null)
            return MembershipResult.Fail(MembershipError.Forbidden, "You do not own this user's restaurant");

        user.RestaurantId = null;
        await _userRepository.SaveChangesAsync();

        return MembershipResult.Ok(user.Id, user.RestaurantId);
    }

    public async Task<MenuItemCreatedResponse> AddFoodItem(Guid restaurantId, CreateMenuItemRequest request)
    {
        var existingItem = await _context.MenuItem.FirstOrDefaultAsync(mi => mi.RestaurantId == restaurantId && mi.Name == request.Name);
        if(existingItem != null && existingItem.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A menu item with the same name already exists for this restaurant.");
        }
        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Name = request.Name,
            Price = request.Price,
            IsDrink = request.IsDrink
        };

        _context.MenuItem.Add(menuItem);
        await _context.SaveChangesAsync();

        return new MenuItemCreatedResponse(menuItem.Id, menuItem.RestaurantId, menuItem.Name, menuItem.Price, menuItem.IsDrink, "Menu item created successfully");
    }
}