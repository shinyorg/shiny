namespace Sample.Dev;


public class AppDataViewModel : ViewModel
{
    public AppDataViewModel(BaseServices services) : base(services)
    {
        this.Load = ReactiveCommand.Create(() =>
            this.Files = new DirectoryInfo(this.Platform.AppData.FullName).GetFiles().ToList()
        );

        this.WhenAnyValueSelected(
            x => x.SelectedFile,
            async x =>
            {
                var canOpen = (x.Length > 0 && x.Length < 1000000);
                if (canOpen)
                {
                    await this.Dialogs.DisplayActionSheetAsync(
                        "Actions",
                        ActionSheetButton.CreateButton(
                            "Open",
                            () => this.Navigation.Navigate(
                                nameof(FileViewPage),
                                ("FilePath", x.FullName)
                            )
                        ),
                        ActionSheetButton.CreateDestroyButton(
                            "Delete",
                            () => this.DeleteAction(x)
                        )
                    );
                }
                else
                {
                    await this.DeleteAction(x);
                }
            }
        );
    }


    public ICommand Load { get; }

    [Reactive] public List<FileInfo> Files { get; set; }
    [Reactive] public FileInfo? SelectedFile { get; set; }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }


    async Task DeleteAction(FileInfo file)
    {
        var confirm = await this.Dialogs.DisplayAlertAsync(
            "Confirm",
            $"Are you sure you wish to delete '{file.Name}'? You will need to restart your app after performing this operation",
            "Yes",
            "No"
        );
        if (confirm)
        {
            try
            {
                file.Delete();
                this.Load.Execute(null);
            }
            catch
            {
                await this.Dialogs.DisplayAlertAsync("ERROR", "Unable to delete file", "OK");
            }
        }
    }
}