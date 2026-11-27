using System.IO;
using Android.App;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Shiny.Extensions.Configuration;


/// <summary>
/// A JSON file from the Android asset folder. Unlike <c>AddJsonStream</c>, whose one stream can only be read
/// once, every provider opens the asset again - so the source survives being built more than once (another
/// <c>Build()</c>, <c>AddRemote</c>'s intermediate build) and <c>IConfigurationRoot.Reload()</c>.
/// </summary>
sealed class AndroidAssetJsonConfigurationSource(string fileName) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new AndroidAssetJsonConfigurationProvider(new JsonStreamConfigurationSource(), fileName);
}


sealed class AndroidAssetJsonConfigurationProvider(JsonStreamConfigurationSource source, string fileName)
    : JsonStreamConfigurationProvider(source)
{
    public override void Load()
    {
        using var stream = Application.Context.Assets!.Open(fileName);
        this.Load(stream);
    }
}
