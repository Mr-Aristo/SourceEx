namespace BuildingBlocks.Security;

/// <summary>
/// Well-known application role names.
/// </summary>
public static class RoleNames
{
    public const string Employee = "employee";
    public const string Manager = "manager";
    public const string Finance = "finance";
    public const string Admin = "admin";

    public static readonly IReadOnlyCollection<string> All = [Employee, Manager, Finance, Admin];
    public static readonly IReadOnlyCollection<string> ApprovalRoles = [Manager, Finance, Admin];
}
