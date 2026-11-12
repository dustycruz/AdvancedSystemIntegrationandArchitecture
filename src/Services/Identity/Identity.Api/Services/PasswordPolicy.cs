namespace Identity.Api.Services;

/// <summary>Password rules from contracts/identity.v1.yaml: at least 8 characters with upper case, lower case and a digit.</summary>
public static class PasswordPolicy
{
    public static IReadOnlyList<string> Validate(string password)
    {
        var errors = new List<string>();
        if (password.Length < 8)
        {
            errors.Add("Use at least 8 characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Add at least one upper-case letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Add at least one lower-case letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Add at least one digit.");
        }

        return errors;
    }
}
