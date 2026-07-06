using BMS.Application.DTOs.Notifications;
using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using BMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return notifications.Select(MapToDto);
    }

    public async Task<NotificationDto?> GetByIdAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        return notification is null ? null : MapToDto(notification);
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId           = dto.UserId,
            Title            = dto.Title,
            Message          = dto.Message,
            NotificationType = Enum.Parse<NotificationType>(dto.NotificationType, ignoreCase: true),
            IsRead           = false,
            CreatedAt        = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return MapToDto(notification);
    }

    public async Task MarkAsReadAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id)
                           ?? throw new KeyNotFoundException($"Notification {id} not found.");

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _context.SaveChangesAsync();
    }

    // ── Mapping ─────────────────────────────────────────────────────────────

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id               = n.Id,
        UserId           = n.UserId,
        Title            = n.Title,
        Message          = n.Message,
        NotificationType = n.NotificationType.ToString(),
        IsRead           = n.IsRead,
        CreatedAt        = n.CreatedAt
    };
}
