using BMS.Infrastructure.Enums;

namespace BMS.Infrastructure.Entities;

public class Notification
{
    public int              Id               { get; set; }
    public string           UserId           { get; set; } = string.Empty;
    public string           Title            { get; set; } = string.Empty;
    public string           Message          { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public bool             IsRead           { get; set; } = false;
    public DateTime         CreatedAt        { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
