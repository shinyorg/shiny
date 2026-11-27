using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shiny.Extensions.Configuration.Infrastructure;


public class RemoteConfigurationProvider(
    RemoteConfig config, 
    Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData
) : ConfigurationProvider, IRemoteConfigurationProvider
{
    const string LAST_LOAD_KEY = "__LastLoaded";
    
    public override void Load()
    {
        base.Load();
        if (File.Exists(config.ConfigurationFilePath))
            this.LastLoaded = File.GetLastWriteTime(config.ConfigurationFilePath);
        
        if (config.WaitForLoadOnStartup)
        {
            // Task.Run so a getData callback that captures the caller's (UI) synchronization context can't deadlock
            // the blocking wait. A failed remote load must not fail the whole configuration build (no network on
            // first launch, a server outage) - the cached file, when there is one, keeps serving.
            try
            {
                Task.Run(() => this.LoadAsync()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        else
        {
            this.LoadAsync().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Console.WriteLine(x.Exception);
            });
        }
    }


    public DateTimeOffset? LastLoaded 
    {
        get
        {
            if (!this.Data.ContainsKey(LAST_LOAD_KEY))
                return null;

            return DateTimeOffset.Parse(this.Data[LAST_LOAD_KEY]);
        }
        private set => this.Data[LAST_LOAD_KEY] = value.ToString();
    }


    HttpClient? httpClient;
    readonly SemaphoreSlim semaphore = new(1);
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // there can only be one! - acquired outside the try so a cancelled wait never releases a slot it didn't take
        await this.semaphore
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (this.LastLoaded != null)
            {
                var ts = DateTimeOffset.UtcNow.Subtract(this.LastLoaded.Value);
                if (ts.TotalSeconds < 30)
                    return;
            }
            // // if I was waiting, just return, the config will have been called and loaded
            // if (wasWaiting)
            //     return;

            if (getData == null)
            {
                this.httpClient ??= new();
                var json = await this.httpClient
                    .GetStringAsync(config.Uri, cancellationToken)
                    .ConfigureAwait(false);

                JsonObject.Parse(json);
                await this.WriteJson(json, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var node = await getData.Invoke(config, cancellationToken).ConfigureAwait(false);
                var json = node.ToJsonString();
                await this.WriteJson(json, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    async Task WriteJson(string json, CancellationToken cancellationToken)
    {
        await File
            .WriteAllTextAsync(config.ConfigurationFilePath, json, cancellationToken)
            .ConfigureAwait(false);

        this.LastLoaded = DateTimeOffset.UtcNow;
    }
}