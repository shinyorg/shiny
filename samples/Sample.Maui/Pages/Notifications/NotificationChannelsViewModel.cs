using Shiny.Notifications;

namespace Sample.Maui.Pages.Notifications;

[ShellMap<NotificationChannelsPage>("notificationchannels")]
public partial class NotificationChannelsViewModel(INotificationManager notifications) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;

    public List<ChannelItemViewModel> Channels
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdentifierEditable))]
    bool isEditing;

    public bool IsIdentifierEditable => !this.IsEditing;

    [ObservableProperty] string channelId = string.Empty;
    [ObservableProperty] string channelDescription = string.Empty;

    public List<string> ImportanceOptions { get; } = ["Low", "Normal", "High", "Critical"];
    [ObservableProperty] int selectedImportanceIndex = 1;

    public List<string> SoundOptions { get; } = ["None", "Default", "High", "Custom"];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomSound))]
    int selectedSoundIndex = 1;
    public bool IsCustomSound => this.SelectedSoundIndex == 3;
    [ObservableProperty] string customSoundPath = string.Empty;

    public bool IsAndroid => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsIos => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.iOS;

    [ObservableProperty] bool androidBlockable;
    [ObservableProperty] bool androidAllowBubbles;
    [ObservableProperty] bool androidShowBadge = true;
    [ObservableProperty] bool androidEnableLights;
    [ObservableProperty] bool androidEnableVibration = true;
    [ObservableProperty] bool androidBypassDnd;

    public ObservableCollection<ChannelActionItemViewModel> Actions { get; } = [];
    [ObservableProperty] string actionId = string.Empty;
    [ObservableProperty] string actionTitle = string.Empty;
    public List<string> ActionTypeOptions { get; } = ["None", "TextReply", "Destructive", "OpenApp"];
    [ObservableProperty] int selectedActionTypeIndex;

    public void OnAppearing() => this.LoadChannels();
    public void OnDisappearing() { }

    void LoadChannels()
    {
        this.Channels = notifications.GetChannels().Select(ch => new ChannelItemViewModel
        {
            Identifier = ch.Identifier,
            Importance = ch.Importance.ToString(),
            Sound = ch.Sound.ToString(),
            ActionCount = ch.Actions.Count
        }).ToList();
    }

    [RelayCommand]
    void SelectChannel(ChannelItemViewModel? item)
    {
        if (item == null) return;

        var channel = notifications.GetChannel(item.Identifier);
        if (channel == null) return;

        this.IsEditing = true;
        this.ChannelId = channel.Identifier;
        this.ChannelDescription = channel.Description ?? string.Empty;
        this.SelectedImportanceIndex = (int)channel.Importance - 1;
        this.SelectedSoundIndex = (int)channel.Sound;
        this.CustomSoundPath = channel.CustomSoundPath ?? string.Empty;

        this.Actions.Clear();
        foreach (var action in channel.Actions)
        {
            this.Actions.Add(new ChannelActionItemViewModel
            {
                Identifier = action.Identifier,
                Title = action.Title,
                ActionType = action.ActionType.ToString()
            });
        }

#if ANDROID
        if (channel is AndroidChannel ac)
        {
            this.AndroidBlockable = ac.Blockable ?? false;
            this.AndroidAllowBubbles = ac.AllowBubbles ?? false;
            this.AndroidShowBadge = ac.ShowBadge ?? true;
            this.AndroidEnableLights = ac.EnableLights ?? false;
            this.AndroidEnableVibration = ac.EnableVibration ?? true;
            this.AndroidBypassDnd = ac.BypassDnd ?? false;
        }
#endif

        this.Status = $"Editing: {channel.Identifier}";
    }

    [RelayCommand]
    void NewChannel()
    {
        this.IsEditing = false;
        this.ChannelId = string.Empty;
        this.ChannelDescription = string.Empty;
        this.SelectedImportanceIndex = 1;
        this.SelectedSoundIndex = 1;
        this.CustomSoundPath = string.Empty;
        this.Actions.Clear();
        this.AndroidBlockable = false;
        this.AndroidAllowBubbles = false;
        this.AndroidShowBadge = true;
        this.AndroidEnableLights = false;
        this.AndroidEnableVibration = true;
        this.AndroidBypassDnd = false;
        this.Status = string.Empty;
    }

    [RelayCommand]
    void AddAction()
    {
        if (string.IsNullOrWhiteSpace(this.ActionId))
        {
            this.Status = "Action identifier is required";
            return;
        }

        this.Actions.Add(new ChannelActionItemViewModel
        {
            Identifier = this.ActionId,
            Title = string.IsNullOrWhiteSpace(this.ActionTitle) ? this.ActionId : this.ActionTitle,
            ActionType = this.ActionTypeOptions[this.SelectedActionTypeIndex]
        });
        this.ActionId = string.Empty;
        this.ActionTitle = string.Empty;
        this.SelectedActionTypeIndex = 0;
    }

    [RelayCommand]
    void RemoveAction(ChannelActionItemViewModel? action)
    {
        if (action != null)
            this.Actions.Remove(action);
    }

    [RelayCommand]
    void SaveChannel()
    {
        if (string.IsNullOrWhiteSpace(this.ChannelId))
        {
            this.Status = "Channel identifier is required";
            return;
        }

        try
        {
            var channel = this.BuildChannel();

            if (this.IsEditing)
                notifications.RemoveChannel(this.ChannelId);

            notifications.AddChannel(channel);
            this.Status = $"Channel '{this.ChannelId}' saved";
            this.LoadChannels();
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }

    Channel BuildChannel()
    {
        Channel channel;

#if ANDROID
        channel = new AndroidChannel
        {
            Blockable = this.AndroidBlockable,
            AllowBubbles = this.AndroidAllowBubbles,
            ShowBadge = this.AndroidShowBadge,
            EnableLights = this.AndroidEnableLights,
            EnableVibration = this.AndroidEnableVibration,
            BypassDnd = this.AndroidBypassDnd
        };
#elif IOS
        channel = new AppleChannel();
#else
        channel = new Channel();
#endif

        channel.Identifier = this.ChannelId;
        channel.Description = string.IsNullOrWhiteSpace(this.ChannelDescription) ? null : this.ChannelDescription;
        channel.Importance = (ChannelImportance)(this.SelectedImportanceIndex + 1);
        channel.Sound = (ChannelSound)this.SelectedSoundIndex;
        if (channel.Sound == ChannelSound.Custom && !string.IsNullOrWhiteSpace(this.CustomSoundPath))
            channel.CustomSoundPath = this.CustomSoundPath;

        channel.Actions = this.Actions.Select(a => ChannelAction.Create(
            a.Identifier,
            a.Title,
            Enum.Parse<ChannelActionType>(a.ActionType)
        )).ToList();

        return channel;
    }

    [RelayCommand]
    void DeleteChannel()
    {
        if (string.IsNullOrWhiteSpace(this.ChannelId))
            return;

        try
        {
            notifications.RemoveChannel(this.ChannelId);
            this.Status = $"Channel '{this.ChannelId}' deleted";
            this.NewChannel();
            this.LoadChannels();
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }
}

public partial class ChannelItemViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string importance = string.Empty;
    [ObservableProperty] string sound = string.Empty;
    [ObservableProperty] int actionCount;
}

public partial class ChannelActionItemViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string actionType = string.Empty;
}
