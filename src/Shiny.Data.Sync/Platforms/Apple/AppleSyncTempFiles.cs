using System.IO;
using Shiny.Hosting;

namespace Shiny.Data.Sync;


internal static class AppleSyncTempFiles
{
    public static string GetUploadTempFilePath(IPlatform platform, string operationId)
        => Path.Combine(platform.Cache.FullName, $"sync-{operationId}.json");
}
