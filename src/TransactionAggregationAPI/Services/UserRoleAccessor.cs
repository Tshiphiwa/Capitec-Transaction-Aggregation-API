using System.Security.Claims;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Capitec_Transaction_Aggregation_API.Services;

public class UserRoleAccessor : IUserRoleAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserRoleAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserRole GetCurrentUserRole()
    {
        var role = _httpContextAccessor.HttpContext?
            .User?
            .FindFirstValue(ClaimTypes.Role);

        return Enum.TryParse<UserRole>(role, out var userRole)
            ? userRole
            : UserRole.Analyst;
    }
}
