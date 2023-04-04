namespace Sample;


public class AppDataViewModel : ViewModel
{
    public AppDataViewModel(BaseServices services) : base(services)
    {
        this.Load = ReactiveCommand.Create(() =>
            this.Files = new DirectoryInfo(this.Platform.AppData.FullName).GetFiles().ToList()
        );

        this.Purge = ReactiveCommand.Create(async () =>
        {
            var confirm = await this.Dialogs.DisplayAlertAsync(
                "Confirm",
                "Are you sure you wish to delete appdata? You will need to restart your app after performing this operation",
                "Yes",
                "No"
            );
            if (!confirm)
                return;

            var files = new DirectoryInfo(this.Platform.AppData.FullName).GetFiles();
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch { } // catch and release
            }
            this.Load.Execute(null);
        });

        this.WhenAnyValueSelected(
            x => x.SelectedFile,
            async x =>
            {
                if (x.Length == 0)
                {
                    await this.Dialogs.DisplayAlertAsync("Error", "File has no content", "OK");
                    return;
                }
                if (x.Length > 1000000)
                {
                    await this.Dialogs.DisplayAlertAsync("Error", "File is too big to open", "OK");
                    return;
                }
                await this.Navigation.Navigate(
                    nameof(FileViewPage),
                    ("FilePath", x.FullName)
                );
            }
        );
    }


    public ICommand Load { get; }
    public ICommand Purge { get; }

    [Reactive] public List<FileInfo> Files { get; set; }
    [Reactive] public FileInfo? SelectedFile { get; set; }

    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }
}