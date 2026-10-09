using System.Text.Json;

namespace Shiny.Wearables;


/// <summary>
/// String and key/value overloads of <see cref="IWearableManager"/>, so a payload does not have to be encoded by hand.
/// Text goes as UTF-8; key/values go as a JSON object. See
/// <see cref="WearableData"/> for the value types allowed.
/// </summary>
public static class WearableManagerExtensions
{
    /// <summary>
    /// Sends plain text as a live message and returns the reply as text.
    /// </summary>
    /// <exception cref="WearableException">Not supported, no reachable wearable, or the send failed.</exception>
    public static async Task<string> SendMessage(this IWearableManager manager, string path, string data, string? nodeId = null, CancellationToken cancelToken = default)
    {
        var reply = await manager.SendMessage(path, WearableData.FromString(data), nodeId, cancelToken).ConfigureAwait(false);
        return WearableData.GetString(reply);
    }


    /// <summary>
    /// Sends key/values as a JSON object in a live message and returns the reply's key/values — empty when the
    /// companion app replied with nothing.
    /// </summary>
    /// <exception cref="WearableException">Not supported, no reachable wearable, or the send failed.</exception>
    /// <exception cref="JsonException">The reply is not a JSON object.</exception>
    public static async Task<IReadOnlyDictionary<string, JsonElement>> SendMessage<TValue>(this IWearableManager manager, string path, IReadOnlyDictionary<string, TValue> values, string? nodeId = null, CancellationToken cancelToken = default)
    {
        var reply = await manager.SendMessage(path, WearableData.FromValues(values), nodeId, cancelToken).ConfigureAwait(false);
        return WearableData.GetValues(reply);
    }


    /// <summary>Replaces the shared context with plain text.</summary>
    public static Task UpdateContext(this IWearableManager manager, string data, CancellationToken cancelToken = default)
        => manager.UpdateContext(WearableData.FromString(data), cancelToken);


    /// <summary>Replaces the shared context with key/values, sent as a JSON object.</summary>
    public static Task UpdateContext<TValue>(this IWearableManager manager, IReadOnlyDictionary<string, TValue> values, CancellationToken cancelToken = default)
        => manager.UpdateContext(WearableData.FromValues(values), cancelToken);


    /// <summary>Queues plain text for the wearable.</summary>
    /// <returns>The transfer's id.</returns>
    public static Task<string> Transfer(this IWearableManager manager, string path, string data, CancellationToken cancelToken = default)
        => manager.Transfer(path, WearableData.FromString(data), cancelToken);


    /// <summary>Queues key/values, sent as a JSON object, for the wearable.</summary>
    /// <returns>The transfer's id.</returns>
    public static Task<string> Transfer<TValue>(this IWearableManager manager, string path, IReadOnlyDictionary<string, TValue> values, CancellationToken cancelToken = default)
        => manager.Transfer(path, WearableData.FromValues(values), cancelToken);
}
