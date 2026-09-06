using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }
}