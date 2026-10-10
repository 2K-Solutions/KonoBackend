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
using Kono.Infrastructure.Contracts.Restaurants;



namespace Kono.Infrastructure.Restaurants.Services;

public class MainRestaurantServices
{
    private readonly KonoDbContext _context;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IOwnerRepository _ownerRepository;
    private readonly IUserRepository _userRepository;

    public MainRestaurantServices(KonoDbContext context,
        IRestaurantRepository restaurantRepository,
        ITableRepository tableRepository,
        IOwnerRepository ownerRepository,
        IUserRepository userRepository)
    {
        _context = context;
        _restaurantRepository = restaurantRepository;
        _tableRepository = tableRepository;
        _ownerRepository = ownerRepository;
        _userRepository = userRepository;
    }

    
    // Create a new restaurant by admin, Returns: Basic rest. info
    public async Task<RestaurantBasic?> CreateRestaurant(CreateRestaurantRequest request)
    {
        var owner = await _ownerRepository.GetByIdAsync(request.OwnerId);
        if (owner is null) return null;

        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            RestaurantName = request.RestaurantName.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            IsActive = true,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await _restaurantRepository.AddRestaurantAsync(restaurant);

        return new RestaurantBasic(restaurant.Id, restaurant.RestaurantName, restaurant.City, restaurant.Address);
    }

    // Gets restaurant by ownerId, Returns: List of restaurants with basic info
    public async Task<List<RestaurantBasic>> GetRestaurant(Guid ownerId)
    {
        var restaurants = await _restaurantRepository.GetRestaurantsByOwnerIdAsync(ownerId);

        return restaurants
            .Select(r => new RestaurantBasic(r.Id, r.RestaurantName, r.City, r.Address))
            .ToList();
    }

    // Fetches 5 users that dont have restaurantId, Returns: List of 5 basic user info
    public async Task<List<BasicUserInfo>> GetSomeAvailableUsersAsync([FromQuery] string query)
    {
        var availableUsers = await _restaurantRepository.GetSomeAvailableUsersAsync();

        return availableUsers
            .Select(u => new BasicUserInfo(u.Id, u.Email, u.Username, u.FirstName, u.SecondName))
            .Where(u => EF.Functions.ILike(u.Username, $"%{query}%"))
            .OrderBy(u => u.Username)
            .ToList();
    }

    // Checks if a user is eligible for an invite to a restaurant, Returns: InviteResult with success flags and message
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

    // Generates a new invite for a user to join a restaurant, Returns: The created RestaurantInvite
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

    // Retrieves pending invites from owner for the user(worker), Returns: List of PendingInviteResponse
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

    // Retrieves pending invites that owner sent, Returns: List of PendingInviteResponse
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

    // Accepts an invite for a user to join a restaurant, Returns: MembershipResult with success flags and message
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

    // Declines an invite for a user to join a restaurant, Returns: MembershipResult with success flags and message
    public async Task<MembershipResult> DeclineInvite(Guid inviteId, Guid userId)
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

        invite.Status = RestaurantInviteStatus.Declined;
        invite.RespondedAt = now;

        await _context.SaveChangesAsync();

        return MembershipResult.Ok(invite.Id, invite.RestaurantId);
    }

    // Enables Owner to kick a user from their restaurant, Returns: MembershipResult with success flags and message
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

    // Retrieves users of a restaurant for the owner, Returns: List of BasicUserInfo
    public async Task<List<BasicUserInfo>> GetRestaurantUsers(Guid restaurantId, Guid ownerId)
    {
        var users = await _restaurantRepository.GetRestaurantUsersByRestaurantandOwnerIdAsync(restaurantId, ownerId);

        return users
            .Select(u => new BasicUserInfo(u.Id, u.Email, u.Username, u.FirstName, u.SecondName))
            .ToList();
    }

    public async Task<Tables> AddTable(Guid restaurantId, AddTableRequest request, Guid ownerId)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant is null || restaurant.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("You do not own this restaurant.");
        }

        var table = new Tables
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            TableNumber = request.TableNumber,
        };

        await _tableRepository.AddTableAsync(table);
        await _context.SaveChangesAsync();

        return table;
    }

    public async Task<Tables> EditTableNumber(Guid restaurantId, Guid tableId, EditTableRequest request, Guid ownerId)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant is null || restaurant.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("You do not own this restaurant.");
        }

        var table = await _tableRepository.GetTableByIdAsync(tableId);
        if (table is null || table.RestaurantId != restaurantId)
        {
            throw new KeyNotFoundException("Table not found in this restaurant.");
        }

        table.TableNumber = request.TableNumber;

        await _tableRepository.UpdateTableAsync(table);
        await _context.SaveChangesAsync();

        return table;
    }

    public async Task<List<Tables>> GetTables(Guid restaurantId, Guid ownerId)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId);
        if (restaurant is null || restaurant.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("You do not own this restaurant.");
        }

        var tables = await _tableRepository.GetTablesByRestaurantIdAsync(restaurantId);
        return tables;
    }
}