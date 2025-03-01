using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Shiny.Extensions.Configuration.Infrastructure;


public class RemoteConfigurationProvider(
    RemoteConfig config, 
    Func<RemoteConfig, CancellationToken, Task<object>>? getData
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
            this.LoadAsync().GetAwaiter().GetResult();
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
        try
        {
            // var wasWaiting = this.semaphore.CurrentCount == 0;
            // there can only be one!
            await this.semaphore.WaitAsync(cancellationToken);

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
                var obj = await getData.Invoke(config, cancellationToken).ConfigureAwait(false);
                var json = JsonSerializer.Serialize(obj);
                await this.WriteJson(json, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    Timer? timer;
    public TimeSpan? AutoRefreshTimeSpan { get; private set; }
    public void StartAutoRefresh(TimeSpan refreshInterval)
    {
        this.timer = new Timer { Interval = refreshInterval.TotalMilliseconds };
        this.timer.Elapsed += async (_, __) =>
        {
            this.timer.Stop();
            try
            {
                await this.LoadAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            this.timer.Stop();
        };
        this.timer.Start();
    }

    
    public void StopAutoRefresh()
    {
        this.timer?.Stop();
        this.timer = null;
        this.AutoRefreshTimeSpan = null;
    }


    async Task WriteJson(string json, CancellationToken cancellationToken)
    {
        await File
            .WriteAllTextAsync(config.ConfigurationFilePath, json, cancellationToken)
            .ConfigureAwait(false);

        this.LastLoaded = DateTimeOffset.UtcNow;
    }
}