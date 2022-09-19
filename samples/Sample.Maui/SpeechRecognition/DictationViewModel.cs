using Shiny.SpeechRecognition;

namespace Sample.SpeechRecognition;


public class DictationViewModel : ViewModel
{
    readonly ISpeechRecognizer speech;


    public DictationViewModel(BaseServices services, ISpeechRecognizer speech) : base(services)
    {
        this.speech = speech;

        this.ToggleListen = new Command(()  =>
        {
            if (this.IsListening)
            {
                // TODO
                //this.dictSub?.Dispose();
            }
            else
            {
                if (this.UseContinuous)
                {
                    this.speech
                        .ContinuousDictation()
                        .SubOnMainThread(
                            x => this.Text += " " + x,
                            ex => this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK")
                        )
                        .DisposedBy(this.DestroyWith);
                }
                else
                {
                    this.speech
                        .ListenUntilPause()
                        .SubOnMainThread(
                            x => this.Text = x,
                            ex => this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK")
                        )
                        .DisposedBy(this.DestroyWith);
                }
            }
        });
    }


    public ICommand ToggleListen { get; }
    [Reactive] public bool IsListening { get; private set; }
    [Reactive] public bool UseContinuous { get; set; }
    [Reactive] public string Text { get; private set; }

    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.speech
            .WhenListeningStatusChanged()
            .SubOnMainThread(x => this.IsListening = x);

        return base.InitializeAsync(parameters);
    }
}
