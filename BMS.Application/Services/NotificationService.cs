using BMS.Application.Common;
using BMS.Application.DTOs.Notifications;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService     _currentUser;

    public NotificationService(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser            = currentUser;
    }

    public Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return _notificationRepository.GetByUserIdAsync(resolvedUserId);
    }

    public Task<int> GetUnreadCountAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return _notificationRepository.GetUnreadCountAsync(resolvedUserId);
    }

    public Task<NotificationDto> CreateAsync(CreateNotificationDto dto) =>
        _notificationRepository.CreateAsync(dto);

    public async Task MarkAsReadAsync(int id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification is null)
            throw new KeyNotFoundException($"Notification {id} not found.");

        DataScope.EnsureViewerUserAccess(_currentUser, notification.UserId);
        await _notificationRepository.MarkAsReadAsync(id);
    }

    public Task MarkAllAsReadAsync(string userId)
    {
        var resolvedUserId = DataScope.ResolveUserId(_currentUser, userId);
        return _notificationRepository.MarkAllAsReadAsync(resolvedUserId);
    }
}
