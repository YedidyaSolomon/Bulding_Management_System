using BMS.API.Wrappers;
using BMS.Application.DTOs.Notifications;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMS.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService) =>
        _notificationService = notificationService;

    /// <summary>GET /api/notifications — current user's notifications</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationDto>>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("User ID not found in token.");

        var notifications = await _notificationService.GetByUserIdAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(notifications));
    }

    /// <summary>GET /api/notifications/unread-count</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("User ID not found in token.");

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>PUT /api/notifications/{id}/read — mark one as read</summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Notification marked as read."));
    }

    /// <summary>PUT /api/notifications/read-all — mark all as read</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("User ID not found in token.");

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<object>.Ok(null!, "All notifications marked as read."));
    }

    /// <summary>POST /api/notifications — create a notification (Admin/Manager only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
    {
        var notification = await _notificationService.CreateAsync(dto);
        return StatusCode(201, ApiResponse<NotificationDto>.Ok(notification, "Notification created."));
    }
}
