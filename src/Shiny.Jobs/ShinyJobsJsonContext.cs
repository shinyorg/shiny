using System.Text.Json.Serialization;

namespace Shiny.Jobs;

[JsonSerializable(typeof(JobInfo))]
internal partial class ShinyJobsJsonContext : JsonSerializerContext;
