using Shiny.SpeechRecognition;

namespace Sample.SpeechRecognition;


public class DictationViewModel : ViewModel
{
    readonly ISpeechRecognizer speech;
    IDisposable? listenSub;
    IDisposable? dictSub;


    public DictationViewModel(BaseServices services, ISpeechRecognizer speech) : base(services)
    {
        this.speech = speech;

        this.ToggleListen = new Command(()  =>
        {
            if (this.IsListening)
            {
                this.dictSub?.Dispose();
            }
            else
            {
                if (this.UseContinuous)
                {
                    this.dictSub = speech
                        .ContinuousDictation()
                        .SubOnMainThread(
                            x => this.Text += " " + x,
                            ex => this.Alert(ex.ToString())
                        );
                }
                else
                {
                    this.dictSub = speech
                        .ListenUntilPause()
                        .SubOnMainThread(
                            x => this.Text = x,
                            ex => this.Alert(ex.ToString())
                        );
                }
            }
        });
    }


    public ICommand ToggleListen { get; }


    bool listen;
    public bool IsListening
    {
        get => this.listen;
        private set => this.Set(ref this.listen, value);
    }


    bool cont = true;
    public bool UseContinuous
    {
        get => this.cont;
        set => this.Set(ref this.cont, value);
    }


    string text;
    public string Text
    {
        get => this.text;
        private set => this.Set(ref this.text, value);
    }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.listenSub = speech
            .WhenListeningStatusChanged()
            .SubOnMainThread(x => this.IsListening = x);
    }


    public override void OnDisappearing()
    {
        base.OnDisappearing();
        this.dictSub?.Dispose();
        this.listenSub?.Dispose();
    }
}
