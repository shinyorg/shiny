using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Notifications;


/// <summary>
/// Copies or downloads <see cref="Notification.ImageUri"/> into a file Shiny owns. Every platform needs a local file:
/// iOS/macOS move the attachment into their own store, Android decodes it when the notification is shown (possibly
/// from an alarm long after Send), and Windows toasts can't load remote images for unpackaged apps.
/// </summary>
static class NotificationImages
{
    // iOS's own limit for an image attachment
    public const long MaxBytes = 10 * 1024 * 1024;
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };


    /// <summary>
    /// Stores the image as <paramref name="directory"/>/<paramref name="name"/>.ext and returns its path, or null (logged)
    /// when it can't be read - the notification is still sent, without the image.
    /// </summary>
    public static async Task<string?> TryStore(string imageUri, string directory, string name, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(directory);
            Delete(directory, name);

            if (Uri.TryCreate(imageUri, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                return await Download(uri, directory, name, logger).ConfigureAwait(false);

            var local = uri?.IsFile == true ? uri.LocalPath : imageUri;
            if (!File.Exists(local))
            {
                logger.LogWarning("Notification image '{ImageUri}' does not exist - sending without it", imageUri);
                return null;
            }
            if (new FileInfo(local).Length > MaxBytes)
            {
                logger.LogWarning("Notification image '{ImageUri}' is larger than {MaxBytes} bytes - sending without it", imageUri, MaxBytes);
                return null;
            }

            // a copy, because iOS moves the attachment file it is given
            var path = Path.Combine(directory, name + Path.GetExtension(local).ToLowerInvariant());
            File.Copy(local, path, true);
            return path;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification image '{ImageUri}' could not be stored - sending without it", imageUri);
            return null;
        }
    }


    /// <summary>
    /// The stored image for <paramref name="name"/>, if there is one
    /// </summary>
    public static string? Find(string directory, string name)
    {
        if (!Directory.Exists(directory))
            return null;

        foreach (var file in Directory.EnumerateFiles(directory, name + ".*"))
            return file;

        return null;
    }


    public static void Delete(string directory, string name)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                foreach (var file in Directory.EnumerateFiles(directory, name + ".*"))
                    File.Delete(file);
            }
        }
        catch
        {
            // a leftover image file is harmless and is replaced the next time the id is used
        }
    }


    static async Task<string?> Download(Uri uri, string directory, string name, ILogger logger)
    {
        using var response = await http
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Notification image '{ImageUri}' returned HTTP {Status} - sending without it", uri, (int)response.StatusCode);
            return null;
        }
        if (response.Content.Headers.ContentLength > MaxBytes)
        {
            logger.LogWarning("Notification image '{ImageUri}' is larger than {MaxBytes} bytes - sending without it", uri, MaxBytes);
            return null;
        }

        // the platforms pick the image type from the file extension
        var ext = GetExtension(response.Content.Headers.ContentType?.MediaType) ?? Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (String.IsNullOrEmpty(ext))
        {
            logger.LogWarning("Notification image '{ImageUri}' has no recognizable image type - sending without it", uri);
            return null;
        }

        var path = Path.Combine(directory, name + ext);
        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        if (bytes.Length == 0 || bytes.Length > MaxBytes)
        {
            logger.LogWarning("Notification image '{ImageUri}' is empty or larger than {MaxBytes} bytes - sending without it", uri, MaxBytes);
            return null;
        }
        await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
        return path;
    }


    static string? GetExtension(string? mediaType) => mediaType?.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/heic" => ".heic",
        "image/heif" => ".heif",
        "image/webp" => ".webp",
        "image/bmp" => ".bmp",
        _ => null
    };
}
