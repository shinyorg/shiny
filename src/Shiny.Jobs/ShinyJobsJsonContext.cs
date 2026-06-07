using System.Text.Json.Serialization;

namespace Shiny.Jobs;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(JobInfo))]
internal partial class ShinyJobsJsonContext : JsonSerializerContext;
