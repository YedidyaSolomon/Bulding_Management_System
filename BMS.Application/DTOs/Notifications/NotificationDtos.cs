namespace BMS.Application.DTOs.Notifications;

public class NotificationDto
{
    public int      Id               { get; set; }
    public string   UserId           { get; set; } = string.Empty;
    public string   Title            { get; set; } = string.Empty;
    public string   Message          { get; set; } = string.Empty;
    public string   NotificationType { get; set; } = string.Empty;
    public bool     IsRead           { get; set; }
    public DateTime CreatedAt        { get; set; }
}

public class CreateNotificationDto
{
    public string UserId           { get; set; } = string.Empty;
    public string Title            { get; set; } = string.Empty;
    public string Message          { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
}
