using System.Globalization;

namespace Travio.Core.Helpers;

public static class DestinationReviewCursor
{
    public static string Encode(DateTime createdAtUtc, int reviewId)
    {
        return $"{createdAtUtc.ToUniversalTime():O}|{reviewId}";
    }

    public static bool TryDecode(string? cursor, out DateTime createdAtUtc, out int reviewId)
    {
        createdAtUtc = default;
        reviewId = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        var parts = cursor.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out createdAtUtc))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out reviewId))
        {
            return false;
        }

        createdAtUtc = createdAtUtc.ToUniversalTime();
        return true;
    }
}
