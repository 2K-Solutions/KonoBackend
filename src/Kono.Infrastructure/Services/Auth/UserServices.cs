using Microsoft.EntityFrameworkCore;
using Kono.Infrastructure.Persistence;
using Kono.Identity.Domain.Users;
using Kono.Identity.Repositories;
using KonoInfrastructure.Contracts.Auth;
using Kono.Infrastructure.Auth.Repositories;

namespace Kono.Infrastructure.Auth.Services;

public class UserServices
{
    private readonly IUserRepository _userRepository;

    public UserServices(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<BasicUserInfo>> GetUnemployedUsersAsync()
    {
        var unemployed = await _userRepository.GetUnemployedUsersAsync();

        return unemployed.Select(u => new BasicUserInfo(u.Id, u.Email, u.Username, u.FirstName, u.SecondName)).ToList();
    }
}