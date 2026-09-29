using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.AppFunctions;

/// <summary>
/// Reads wire arguments for generated code. Missing required values and malformed values throw
/// <see cref="AppFunctionException"/> (<see cref="AppFunctionErrorCode.InvalidArgument"/>) — neither platform enforces
/// required parameters itself. Numbers and booleans are also accepted as strings.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AppFunctionArguments
{
    public static string? GetString(JsonElement args, string name, bool required)
    {
        if (!TryGet(args, name, required, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            _ => throw Invalid(name, "a string")
        };
    }

    public static int? GetInt32(JsonElement args, string name, bool required)
    {
        var value = GetInt64(args, name, required);
        if (value is < Int32.MinValue or > Int32.MaxValue)
            throw Invalid(name, "a whole number in range");
        return (int?)value;
    }

    public static long? GetInt64(JsonElement args, string name, bool required)
    {
        if (!TryGet(args, name, required, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetInt64(out var l))
                return l;
            if (value.TryGetDouble(out var d) && d == Math.Floor(d) && d >= Int64.MinValue && d <= Int64.MaxValue)
                return (long)d;
        }
        if (value.ValueKind == JsonValueKind.String && Int64.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw Invalid(name, "a whole number");
    }

    public static double? GetDouble(JsonElement args, string name, bool required)
    {
        if (!TryGet(args, name, required, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number)
            return value.GetDouble();
        if (value.ValueKind == JsonValueKind.String && Double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw Invalid(name, "a number");
    }

    public static bool? GetBoolean(JsonElement args, string name, bool required)
    {
        if (!TryGet(args, name, required, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when Boolean.TryParse(value.GetString(), out var b) => b,
            _ => throw Invalid(name, "true or false")
        };
    }

    public static DateTimeOffset? GetDateTimeOffset(JsonElement args, string name, bool required)
    {
        if (!TryGet(args, name, required, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return dto;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var epochMs))
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMs);

        throw Invalid(name, "an ISO 8601 date");
    }

    public static TEnum? GetEnum<TEnum>(JsonElement args, string name, bool required) where TEnum : struct, Enum
    {
        var text = GetString(args, name, required);
        if (text == null)
            return null;

        if (Enum.TryParse<TEnum>(text, true, out var result) && Enum.IsDefined(result))
            return result;

        throw Invalid(name, "one of: " + String.Join(", ", Enum.GetNames<TEnum>()));
    }

    /// <summary>Resolves an entity id through its <see cref="IAppEntityQuery{TEntity}"/>. An unknown id is <see cref="AppFunctionErrorCode.NotFound"/>.</summary>
    public static async ValueTask<TEntity?> GetEntity<TEntity>(JsonElement args, string name, bool required, IServiceProvider services, CancellationToken cancellationToken)
    {
        var id = GetString(args, name, required);
        if (id == null)
            return default;

        var query = services.GetRequiredService<IAppEntityQuery<TEntity>>();
        var found = await query.GetByIds([id], cancellationToken).ConfigureAwait(false);
        if (found.Count == 0)
            throw new AppFunctionException(AppFunctionErrorCode.NotFound, $"'{id}' was not found");

        return found[0];
    }

    public static void WriteDateTimeOffset(Utf8JsonWriter writer, DateTimeOffset value)
        => writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture)); // millisecond precision: what ISO8601DateFormatter parses

    static bool TryGet(JsonElement args, string name, bool required, out JsonElement value)
    {
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null)
            return true;

        if (required)
            throw new AppFunctionException(AppFunctionErrorCode.InvalidArgument, $"{name} is required");

        value = default;
        return false;
    }

    static AppFunctionException Invalid(string name, string expected)
        => new(AppFunctionErrorCode.InvalidArgument, $"{name} must be {expected}");
}
