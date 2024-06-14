using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Firebase.Analytics.iOS.Binding;
using Shiny.Firebase.Messaging.iOS.Binding;
using Foundation;

namespace Shiny.Push;


public class FirebasePushProvider(FirebaseConfiguration config) : NotifyPropertyChanged, IPushProvider, IPushTagSupport
{

    public async Task<string> Register(NSData nativeToken)
    {
        this.TryStartFirebase();
        var fcmToken = await FirebaseMessaging.RegisterAsync(nativeToken);
        if (fcmToken == null)
             throw new InvalidOperationException("FCM Token is null");
        
        return fcmToken;
    }


    public Task UnRegister()
    {
        this.RegisteredTags = null;
        return FirebaseMessaging.UnRegisterAsync();
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

        await FirebaseMessaging.SubscribeAsync(tag).ConfigureAwait(false);
        this.RegisteredTags = tags.ToArray();
    }


    public async Task RemoveTag(string tag)
    {
        await FirebaseMessaging
            .UnSubscribeAsync(tag)
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
                await FirebaseMessaging
                    .UnSubscribeAsync(tag)
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
        if (!FirebaseApplication.IsConfigured)
        {
            if (config.UseEmbeddedConfiguration)
            {
                FirebaseApplication.AutoConfigure();
                FirebaseMessaging.IsAutoInitEnabled = true;
            }
            else
            {
                FirebaseApplication.Configure(
                    config.AppId!,
                    config.SenderId!,
                    config.ApiKey,
                    config.ProjectId
                );
            }
        
            if (!FirebaseApplication.IsConfigured)
                throw new InvalidOperationException("Firebase Application failed to configure - please check your settings");
        }
    }
}