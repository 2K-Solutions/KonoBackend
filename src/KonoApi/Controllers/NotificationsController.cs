using Kono.Infrastructure.Contracts.Notifications;
using Kono.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KonoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var accountType = User.FindFirst("accountType")?.Value;
        if (accountType != "unemployed" && accountType != "worker") return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Device token is required" });

        await _notificationService.RegisterDeviceAsync(userId, request.Token.Trim(), request.Platform);

        return Ok(new { message = "Device registered" });
    }

    [HttpDelete("delete-device")]
    public async Task<IActionResult> RemoveDevice([FromBody] RemoveDeviceRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Device token is required" });

        await _notificationService.RemoveDeviceAsync(userId, request.Token.Trim());

        return NoContent();
    }
}
