using System.Globalization;
using System.Text.Json;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.shared.Contracts.DeviceIntegration;

namespace LightWeight.DeviceIntegration.Infrastructure.FatSecret;

/// <summary>
/// Reads the answer of food_entries.get_month v2. FatSecret's JSON comes from XML: the numbers are strings,
/// "day" is an object when there is only one day, and it is missing when the month has no entries
/// </summary>
internal static class FatSecretMonthParser
{
    private static readonly DateOnly Epoch = new(1970, 1, 1);

    /// <summary>The date parameter of FatSecret: days since 1970-01-01</summary>
    public static int ToDateInt(DateOnly date) => date.DayNumber - Epoch.DayNumber;

    public static DateOnly FromDateInt(int dateInt) => Epoch.AddDays(dateInt);

    public static IReadOnlyList<FatSecretDiaryDay> Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement error))
        {
            int code = error.TryGetProperty("code", out JsonElement c) ? ReadInt(c) : 0;
            // 9: invalid access token (revoked by the user or expired)
            if (code == 9)
                throw new FatSecretAccessRevokedException();
            throw new InvalidOperationException($"FatSecret error {code}");
        }

        // FatSecret always sends "month", empty or not. Without it the answer is not what we expect: failing keeps
        // the history import pending instead of marking it done with nothing
        if (!root.TryGetProperty("month", out JsonElement month) || month.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("FatSecret food_entries.get_month answered without a month");
        if (!month.TryGetProperty("day", out JsonElement day))
            return [];

        IEnumerable<JsonElement> days = day.ValueKind switch
        {
            JsonValueKind.Array => day.EnumerateArray(),
            JsonValueKind.Object => [day],
            _ => []
        };

        return days
            .Select(d => new FatSecretDiaryDay(
                FromDateInt(ReadInt(d.GetProperty("date_int"))),
                ReadDecimal(d, "calories"),
                ReadDecimal(d, "protein"),
                ReadDecimal(d, "carbohydrate"),
                ReadDecimal(d, "fat")))
            .ToList();
    }

    private static int ReadInt(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? int.Parse(value.GetString()!, CultureInfo.InvariantCulture)
        : value.GetInt32();

    /// <summary>A missing nutrient counts as 0 (FatSecret omits it when no food of the day has it)</summary>
    private static decimal ReadDecimal(JsonElement day, string name)
    {
        if (!day.TryGetProperty(name, out JsonElement value))
            return 0m;
        return value.ValueKind == JsonValueKind.String
            ? decimal.Parse(value.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture)
            : value.GetDecimal();
    }
}
