using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;

namespace Shiny.Push;


static class PushImageLoader
{
    // FCM gives a data message about 10 seconds before the process may be stopped
    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(8) };
    const long MaxBytes = 10 * 1024 * 1024;
    const int MaxDimension = 1440;


    public static async Task<Bitmap?> TryLoad(string imageUri, CancellationToken cancelToken)
    {
        using var response = await http
            .GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancelToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxBytes)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancelToken).ConfigureAwait(false);
        if (bytes.Length == 0 || bytes.Length > MaxBytes)
            return null;

        return Decode(bytes);
    }


    // a full size photo decoded as-is can run to tens of MB; BigPictureStyle never shows more than a screen width
    static Bitmap? Decode(byte[] bytes)
    {
        var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
            return null;

        var sample = 1;
        while (Math.Max(bounds.OutWidth, bounds.OutHeight) / (sample * 2) >= MaxDimension)
            sample *= 2;

        return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, new BitmapFactory.Options { InSampleSize = sample });
    }
}
