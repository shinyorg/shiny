using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Shiny;
using Shiny.SpeechRecognition;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace Sample
{
    public class ConversationViewModel : SampleViewModel
    {
        readonly ISpeechRecognizer speech;


        public ConversationViewModel()
        {
            this.speech = ShinyHost.Resolve<ISpeechRecognizer>();
            this.Start = new Command(() => this.DoConversation());

            this.speech
                .WhenListeningStatusChanged()
                .SubOnMainThread(x => this.IsListening = x);
        }


        public ICommand Start { get; }
        public ObservableCollection<ListItemViewModel> Items { get; } = new ObservableCollection<ListItemViewModel>();

        bool isListening;
        public bool IsListening
        {
            get => this.isListening;
            private set => this.Set(ref this.isListening, value);
        }


        async Task DoConversation()
        {
            this.Items.Clear();

            using (var cancelSrc = new CancellationTokenSource())
            {
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
        }


        async Task Computer(string speak)
        {
            this.Add(speak, true);
            await TextToSpeech.SpeakAsync(speak);
        }


        void Add(string msg, bool bot = false) => Device.BeginInvokeOnMainThread(() => this.Items.Add(new ListItemViewModel
        {
            Text = msg,
            From = bot ? "Computer" : "You"
        }));
    }
}
