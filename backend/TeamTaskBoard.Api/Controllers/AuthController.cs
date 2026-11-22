using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskBoard.Api.DTOs;
using TeamTaskBoard.Api.Services;

namespace TeamTaskBoard.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public sealed class AuthController(LocalUserService localUsers) : ControllerBase
{
    [HttpGet("me")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var user = await localUsers.SynchronizeAsync(User, cancellationToken);
        return Ok(new CurrentUserDto(user.Id, user.Email));
    }
}
