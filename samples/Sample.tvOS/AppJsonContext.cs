using System.Text.Json.Serialization;
using Sample.tvOS.Services;

namespace Sample.tvOS;


/// <summary>
/// Anything that goes through Shiny's ISerializer needs an exact-type registration here - the
/// concrete type plus the List/array forms. This is what keeps the sample AOT- and trim-safe.
/// </summary>
[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(Viewing))]
[JsonSerializable(typeof(List<Viewing>))]
[JsonSerializable(typeof(Viewing[]))]
public partial class AppJsonContext : JsonSerializerContext;
