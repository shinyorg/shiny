namespace Sample;


public class TagsViewModel : ViewModel
{
    public TagsViewModel(IPushManager pushManager)
    {
        var push = pushManager as IPushTagSupport;

        this.Add = new Command(async () =>
        {
            var result = await this.Prompt("Name of tag?");
            if (!result.IsEmpty())
            {
                await this.Loading(() => push.AddTag(result));
                this.Load.Execute(null);
            }
        });

        this.Clear = this.ConfirmCommand("Are you sure you wish to clear all tags?", async () =>
        {
            await this.Loading(() => push.ClearTags());
            this.Load.Execute(null);
        });

        this.Load = this.LoadingCommand(async () =>
        {
            if (push.RegisteredTags == null)
                return;

            this.Tags = push
                .RegisteredTags
                .Select(tag => new CommandItem
                (
                    tag,
                    null,
                    this.ConfirmCommand(
                        $"Are you sure you wish to remove tag '{tag}'?",
                        async () =>
                        {
                            await push.TryRemoveTag(tag);
                            this.Load.Execute(null);
                        }
                    )
                ))
                .ToList();

            this.RaisePropertyChanged(nameof(this.Tags));
        });
    }


    public ICommand Load { get; }
    public ICommand Add { get; }
    public ICommand Clear { get; }
    public IList<CommandItem> Tags { get; private set; }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        this.Load.Execute(null);
    }
}
