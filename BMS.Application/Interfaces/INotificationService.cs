using BMS.Application.DTOs.Notifications;

namespace BMS.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId);
    Task<int>                          GetUnreadCountAsync(string userId);
    Task<NotificationDto>              CreateAsync(CreateNotificationDto dto);
    Task                               MarkAsReadAsync(int id);
    Task                               MarkAllAsReadAsync(string userId);
}
