using System;
using System.Collections.Generic;
#if APPLE
using System.IO;
using System.Linq;
using Foundation;
#endif

namespace Shiny.Push;


/// <summary>
/// The payload keys Shiny reads a push image URL from.
/// </summary>
public static class PushImages
{
    /// <summary>
    /// The keys checked, in order, when ShinyPushImageKeys is not set. A dotted key (fcm_options.image, which
    /// Firebase sends for an APNs image) walks nested dictionaries.
    /// </summary>
    public static IReadOnlyList<string> DefaultKeys { get; } = ["image", "image-url", "imageUrl", "attachment-url", "fcm_options.image"];


    /// <summary>
    /// Returns the first value under <paramref name="keys"/> that is an absolute http(s) URL.
    /// </summary>
    /// <param name="data">The flat push data.</param>
    /// <param name="keys">The keys to check, in order; defaults to <see cref="DefaultKeys"/>.</param>
    public static string? Find(IDictionary<string, string>? data, IEnumerable<string>? keys = null)
    {
        if (data == null)
            return null;

        foreach (var key in keys ?? DefaultKeys)
        {
            if (data.TryGetValue(key, out var value) && IsWebUrl(value))
                return value.Trim();
        }
        return null;
    }


    internal static bool IsWebUrl(string? value)
        => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);


#if APPLE
    /// <summary>
    /// The keys the app's notification service extension was built with (ShinyPushImageKeys), read from its embedded
    /// Info.plist, so the URL reported to C# is the one the extension attached. <see cref="DefaultKeys"/> without one.
    /// </summary>
    public static IReadOnlyList<string> AppleKeys => appleKeys.Value;
    static readonly Lazy<IReadOnlyList<string>> appleKeys = new(() =>
    {
        try
        {
            var plugins = NSBundle.MainBundle.BuiltinPluginsPath;
            if (plugins != null)
            {
                var path = Path.Combine(plugins, "ShinyNotificationService.appex", "Info.plist");
                if (File.Exists(path) && NSMutableDictionary.FromFile(path)?["ShinyPushImageKeys"] is NSArray array && array.Count > 0)
                    return array.ToArray<NSString>().Select(x => x.ToString()).ToList();
            }
        }
        catch
        {
            // a missing or unreadable extension plist means the defaults
        }
        return DefaultKeys;
    });


    /// <summary>
    /// Returns the first value under <paramref name="keys"/> (defaults to <see cref="AppleKeys"/>) that is an
    /// absolute http(s) URL, walking nested dictionaries for a dotted key.
    /// </summary>
    public static string? Find(NSDictionary? userInfo, IEnumerable<string>? keys = null)
    {
        if (userInfo == null)
            return null;

        foreach (var key in keys ?? AppleKeys)
        {
            NSObject? current = userInfo;
            foreach (var part in key.Split('.'))
                current = (current as NSDictionary)?[part];

            var value = (current as NSString)?.ToString();
            if (IsWebUrl(value))
                return value!.Trim();
        }
        return null;
    }
#endif
}
