namespace SourceEx.Identity.API.Security;

/// <summary>
/// Applies a minimum password policy for interactive identity flows.
/// </summary>
public static class PasswordPolicy
{
    public static string[] Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors.ToArray();
        }

        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters long.");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit.");

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
            errors.Add("Password must contain at least one non-alphanumeric character.");

        return errors.ToArray();
    }
}
