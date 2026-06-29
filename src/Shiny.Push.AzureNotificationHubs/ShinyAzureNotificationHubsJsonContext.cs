using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Push.AzureNotificationHubs;

// AzureNotificationHubsPushProvider persists RegisteredTags as a string[] via IKeyValueStore.
// The key/value store routes non-primitive values (anything other than bool/int/long/float/string)
// through ISerializer, so the string[] (and its List<string> equivalent) needs a registered
// JsonTypeInfo to avoid "No JsonTypeInfo registered for type 'System.String[]'".
[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
internal partial class ShinyAzureNotificationHubsJsonContext : JsonSerializerContext;
