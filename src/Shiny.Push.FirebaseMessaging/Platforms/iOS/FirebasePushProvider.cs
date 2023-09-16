using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.Core;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.Push;


public class FirebasePushProvider : NotifyPropertyChanged, IPushProvider, IPushTagSupport
{
    readonly FirebaseConfiguration config;


    public FirebasePushProvider(
        FirebaseConfiguration config
    )
    {
        this.config = config;
    }


    public async Task<string> Register(NSData nativeToken)
    {
        this.TryStartFirebase();

        Messaging.SharedInstance.ApnsToken = nativeToken;
        var fcmToken = await Messaging.SharedInstance.FetchTokenAsync();

        if (fcmToken == null)
            throw new InvalidOperationException("FCM Token is null");

        return fcmToken;
    }


    public Task UnRegister()
    {
        this.RegisteredTags = null;
        return Messaging.SharedInstance.DeleteTokenAsync();
    }


    string[]? registeredTags;
    public string[]? RegisteredTags
    {
        get => this.registeredTags;
        set => this.Set(ref this.registeredTags, value);
    }


    public async Task AddTag(string tag)
    {
        var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
        tags.Add(tag);

        await Messaging.SharedInstance.SubscribeAsync(tag);
        this.RegisteredTags = tags.ToArray();
    }


    public async Task RemoveTag(string tag)
    {
        await Messaging
            .SharedInstance
            .UnsubscribeAsync(tag)
            .ConfigureAwait(false);

        if (this.RegisteredTags != null)
        {
            var tags = this.RegisteredTags.ToList();
            if (tags.Remove(tag))
                this.RegisteredTags = tags.ToArray();
        }
    }


    public async Task ClearTags()
    {
        if (this.RegisteredTags != null)
        {
            foreach (var tag in this.RegisteredTags)
            {
                await Messaging
                    .SharedInstance
                    .UnsubscribeAsync(tag)
                    .ConfigureAwait(false);
            }
        }
        this.RegisteredTags = null;
    }


    public async Task SetTags(params string[]? tags)
    {
        await this.ClearTags().ConfigureAwait(false);
        if (tags != null)
        {
            foreach (var tag in tags)
                await this.AddTag(tag).ConfigureAwait(false);
        }
    }


    protected virtual void TryStartFirebase()
    {
        if (App.DefaultInstance == null)
        {
            if (this.config.UseEmbeddedConfiguration)
            {
                App.Configure();
                if (Messaging.SharedInstance == null)
                    throw new ArgumentException("Failed to configure firebase messaging - ensure you have GoogleService-Info.plist included in your iOS project and that it is set to a BundleResource");

                Messaging.SharedInstance!.AutoInitEnabled = true;
            }
            else
            {
                App.Configure(new Options(
                    this.config.AppId!,
                    this.config.SenderId!
                )
                {
                    ApiKey = this.config.ApiKey,
                    ProjectId = this.config.ProjectId
                });
            }
        }
    }
}