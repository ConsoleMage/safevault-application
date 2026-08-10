namespace safevault_application.Helpers;

public static class ValidationHelpers
{
    public static bool IsValidInput(string input, string allowedSpecialCharacters = "")
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var validCharacters = allowedSpecialCharacters.ToHashSet();

        return input.All(c => char.IsLetterOrDigit(c) || validCharacters.Contains(c));
    }

    public static bool IsValidXSSInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var normalizedInput = input.ToLowerInvariant();
        return !normalizedInput.Contains("<script") && !normalizedInput.Contains("<iframe");
    }
}
