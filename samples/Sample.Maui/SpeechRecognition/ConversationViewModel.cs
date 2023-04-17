
using Shiny.SpeechRecognition;

namespace Sample.SpeechRecognition;


public class ConversationViewModel : ViewModel
{
    readonly ISpeechRecognizer speech;
    readonly ITextToSpeech textToSpeech;


    public ConversationViewModel(BaseServices services, ISpeechRecognizer speech, ITextToSpeech textToSpeech) : base(services)
    {
        this.speech = speech;
        this.Start = new Command(() => this.DoConversation());

        this.speech
            .WhenListeningStatusChanged()
            .SubOnMainThread(x => this.IsListening = x)
            .DisposedBy(this.DestroyWith);
    }


    public ICommand Start { get; }
    public ObservableCollection<ListItemViewModel> Items { get; } = new ObservableCollection<ListItemViewModel>();

    [Reactive] public bool IsListening { get; private set; }


    async Task DoConversation()
    {
        try
        {
            this.Items.Clear();

            using var cancelSrc = new CancellationTokenSource();
            await this.Computer("Please tell me your name");
            var name = await this.speech.ListenUntilPause().ToTask(cancelSrc.Token);
            this.Add(name);

            await this.Computer($"Hello {name}.  Are you male or female?");
            var sex = await this.speech.ListenForFirstKeyword("male", "female");
            this.Add(sex);

            var next = sex.Equals("male", StringComparison.CurrentCultureIgnoreCase) ? "Yo dude" : "Sup";
            await this.Computer(next);

            await this.Computer("Tell me something interesting about yourself");
            next = await this.speech.ListenUntilPause().ToTask(cancelSrc.Token);
            this.Add(next);

            await this.Computer("Interesting");
        }
        catch (Exception ex)
        {
            await this.Alert(ex.ToString());
        }
    }


    async Task Computer(string speak)
    {
        this.Add(speak, true);
        await TextToSpeech.SpeakAsync(speak);
    }


    void Add(string msg, bool bot = false) => this.Items.Add(new ListItemViewModel
    (
        bot,
        bot ? "Computer" : "You",
        msg
    ));
}
