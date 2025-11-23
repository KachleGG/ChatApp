using System.Text.RegularExpressions;

namespace Chatter.Helpers;

public static partial class Validator
{
    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[a-zA-Z\s'-]{2,50}$")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")]
    private static partial Regex StrongPasswordRegex();

    public static bool IsValidName(string? name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return false;
        }

        return NameRegex().IsMatch(name.Trim());
    }
    public static bool IsValidEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email)) {
            return false;
        }

        // More comprehensive email validation using regex
        return EmailRegex().IsMatch(email.Trim());
    }
    public static bool IsValidPassword(string? password) {
        if (string.IsNullOrWhiteSpace(password)) {
            return false;
        }

        // Minimum length requirement
        return password.Length >= 6;
    }
    public static bool IsStrongPassword(string? password) {
        if (string.IsNullOrWhiteSpace(password)) {
            return false;
        }

        return StrongPasswordRegex().IsMatch(password);
    }
    public static string GetPasswordStrengthMessage(string? password) {
        if (string.IsNullOrWhiteSpace(password)) {
            return "Password is required.";
        }

        if (password.Length < 6) {
            return "Password must be at least 6 characters long.";
        }

        if (password.Length < 8) {
            return "Password should be at least 8 characters long for better security.";
        }

        var missingRequirements = new List<string>();

        if (!password.Any(char.IsUpper)) {
            missingRequirements.Add("an uppercase letter");
        }

        if (!password.Any(char.IsLower)) {
            missingRequirements.Add("a lowercase letter");
        }

        if (!password.Any(char.IsDigit)) {
            missingRequirements.Add("a number");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) {
            missingRequirements.Add("a special character");
        }

        if (missingRequirements.Any()) {
            return $"For a strong password, include: {string.Join(", ", missingRequirements)}.";
        }

        return "Password is strong.";
    }

    public static bool IsValidMessage(string? message) {
        return !string.IsNullOrEmpty(message);
    }
    public static bool IsValidUserId(int userId) {
        return userId > 0;
    }
    public static string SanitizeEmail(string email) {
        return email.Trim().ToLower();
    }
    public static string SanitizeName(string name) {
        // Trim and normalize multiple spaces to single space
        return Regex.Replace(name.Trim(), @"\s+", " ");
    }
}
