using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny
{
    public static partial class Extensions
    {
        public static bool Is(this IPlatform platform, string platformName)
            => platform.Name.Equals(platformName);


        public static bool IsAndroid(this IPlatform platform) => platform.Is(KnownPlatforms.Android);
        public static bool IsIos(this IPlatform platform) => platform.Is(KnownPlatforms.iOS);
        public static bool IsUwp(this IPlatform platform) => platform.Is(KnownPlatforms.Uwp);
        public static bool IsNetstandard(this IPlatform platform) => platform.Is(KnownPlatforms.NetStandard);


        public static async Task<T> InvokeOnMainThreadAsync<T>(this IPlatform platform, Func<Task<T>> func, CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<T>();
            using (cancelToken.Register(() => tcs.TrySetCanceled()))
            {
                platform.InvokeOnMainThread(async () =>
                {
                    try
                    {
                        var result = await func();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
            }
            return await tcs.Task.ConfigureAwait(false);
        }


        public static async Task<T> InvokeOnMainThreadAsync<T>(this IPlatform platform, Func<T> func, CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<T>();
            using (cancelToken.Register(() => tcs.TrySetCanceled()))
            {
                platform.InvokeOnMainThread(() =>
                {
                    try
                    {
                        var result = func();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
            }
            return await tcs.Task.ConfigureAwait(false);
        }


        public static async Task InvokeOnMainThreadAsync(this IPlatform platform, Action action, CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<object>();
            using (cancelToken.Register(() => tcs.TrySetCanceled()))
            {
                platform.InvokeOnMainThread(() =>
                {
                    try
                    {
                        action();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
            }
            await tcs.Task.ConfigureAwait(false);
        }


        public static string ResourceToFilePath(this IPlatform platform, Assembly assembly, string resourceName)
        {
            var path = Path.Combine(platform.AppData.FullName, resourceName);
            if (!File.Exists(path))
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fs = File.Create(path))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
            return path;
        }
    }
}
