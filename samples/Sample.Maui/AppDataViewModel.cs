namespace Sample;


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
                if (x.Length > 1000000)
                {
                    await this.Dialogs.DisplayAlertAsync("Error", "File is too big to open", "Cancel");
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

    [Reactive] public List<FileInfo> Files { get; set; }
    [Reactive] public FileInfo? SelectedFile { get; set; }

    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }
}