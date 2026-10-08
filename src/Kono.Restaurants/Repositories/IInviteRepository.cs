using Kono.Identity.Domain.Users;
using Kono.Restaurants.Domain;

namespace Kono.Restaurants.Repositories;
public interface IInviteRepository
{
    Task<List<RestaurantInvite>> GetAllInvitesAsync();
    Task<RestaurantInvite> GetInviteByIdAsync(Guid inviteId);
    Task AddInviteAsync(RestaurantInvite invite);
    Task UpdateInviteAsync(RestaurantInvite invite);
    Task DeleteInviteAsync(Guid inviteId);
    Task <List<User>> GetFewAvailableUsersAsync();
}