using Capitec_Transaction_Aggregation_API.DTOs;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}
