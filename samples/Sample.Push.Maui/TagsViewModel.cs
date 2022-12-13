namespace Sample;


public class TagsViewModel : ViewModel
{
    public TagsViewModel(IPushManager pushManager)
    {
        this.Load = this.LoadingCommand(async () =>
        {
            if (pushManager.Tags!.RegisteredTags == null)
                return;

            this.Tags = pushManager
                .Tags!
                .RegisteredTags
                .Select(tag => new CommandItem
                (
                    tag,
                    null,
                    this.ConfirmCommand(
                        $"Are you sure you wish to remove tag '{tag}'?",
                        async () =>
                        {
                            await pushManager.Tags!.RemoveTag(tag);
                            this.Load!.Execute(null);
                        }
                    )
                ))
                .ToList();

            this.RaisePropertyChanged(nameof(this.Tags));
        });

        this.Add = new Command(async () =>
        {
            var result = await this.Prompt("Name of tag?");
            if (!result.IsEmpty())
            {
                await this.Loading(() => pushManager.Tags!.AddTag(result));
                this.Load!.Execute(null);
            }
        });

        this.Clear = this.ConfirmCommand("Are you sure you wish to clear all tags?", async () =>
        {
            await this.Loading(() => pushManager.Tags!.ClearTags());
            this.Load.Execute(null);
        });
    }


    public ICommand Load { get; }
    public ICommand Add { get; }
    public ICommand Clear { get; }
    public IList<CommandItem> Tags { get; private set; } = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        this.Load.Execute(null);
    }
}
