namespace Sample.Dev;


public class FileViewViewModel : ViewModel
{
    public FileViewViewModel(BaseServices services) : base(services) {}


    [Reactive] public string Content { get; private set; }

    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);
        var path = parameters.GetValue<string>("FilePath");
        this.Content = File.ReadAllText(path);
        this.Title = Path.GetFileNameWithoutExtension(path);
    }
}