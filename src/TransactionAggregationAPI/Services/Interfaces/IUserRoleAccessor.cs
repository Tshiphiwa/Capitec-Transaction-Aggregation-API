using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface IUserRoleAccessor
{
    UserRole GetCurrentUserRole();
}
