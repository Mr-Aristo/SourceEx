namespace SourceEx.API.Security;

/// <summary>
/// Known authorization policy names for the API.
/// </summary>
public static class AuthorizationPolicies
{
    public const string AuthenticatedUser = "authenticated-user";
    public const string ExpenseApprover = "expense-approver";
}
