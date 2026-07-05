namespace BMS.Application.DTOs.Auth;

/// <summary>
/// Request body for POST /api/auth/assign-role.
/// Only an Admin can submit this request (enforced at the controller level).
/// Assignable roles: Manager, Viewer.
/// Assigning "Admin" is intentionally blocked in the service layer —
/// Admin accounts are provisioned exclusively via the AdminSeeder.
/// </summary>
public class AssignRoleRequestDto
{
    public string UserEmail { get; set; } = string.Empty;
    public string NewRole   { get; set; } = string.Empty;
}

/// <summary>
/// Returned after a successful role assignment.
/// </summary>
public class AssignRoleResponseDto
{
    public string UserEmail  { get; set; } = string.Empty;
    public string PreviousRole { get; set; } = string.Empty;
    public string NewRole    { get; set; } = string.Empty;
}
