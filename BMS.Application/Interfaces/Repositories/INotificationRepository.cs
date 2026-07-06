using BMS.Application.DTOs.Notifications;

namespace BMS.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId);
    Task<NotificationDto?>             GetByIdAsync(int id);
    Task<int>                          GetUnreadCountAsync(string userId);
    Task<NotificationDto>              CreateAsync(CreateNotificationDto dto);
    Task                               MarkAsReadAsync(int id);
    Task                               MarkAllAsReadAsync(string userId);
}
