using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Jobs;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(JobInfo))]
[JsonSerializable(typeof(List<JobInfo>))]
[JsonSerializable(typeof(JobInfo[]))]
internal partial class ShinyJobsJsonContext : JsonSerializerContext;
