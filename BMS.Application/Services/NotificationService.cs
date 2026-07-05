using BMS.Application.DTOs.Notifications;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId) =>
        _notificationRepository.GetByUserIdAsync(userId);

    public Task<int> GetUnreadCountAsync(string userId) =>
        _notificationRepository.GetUnreadCountAsync(userId);

    public Task<NotificationDto> CreateAsync(CreateNotificationDto dto) =>
        _notificationRepository.CreateAsync(dto);

    public Task MarkAsReadAsync(int id) =>
        _notificationRepository.MarkAsReadAsync(id);

    public Task MarkAllAsReadAsync(string userId) =>
        _notificationRepository.MarkAllAsReadAsync(userId);
}
