using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Shiny.SpeechRecognition;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Speech
{
    public class DictationViewModel : ViewModel
    {
        public DictationViewModel(ISpeechRecognizer speech, IUserDialogs dialogs)
        {
            IDisposable token = null;
            speech
                .WhenListeningStatusChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.ListenText = x
                    ? "Stop Listening"
                    : "Start Dictation"
                );


            this.ToggleListen = ReactiveCommand.Create(()  =>
            {
                if (token == null)
                {
                    if (this.UseContinuous)
                    {
                        token = speech
                            .ContinuousDictation()
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(
                                x => this.Text += " " + x,
                                ex => dialogs.Alert(ex.ToString())
                            );
                    }
                    else
                    {
                        token = speech
                            .ListenUntilPause()
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(
                                x => this.Text = x,
                                ex => dialogs.Alert(ex.ToString())
                            );
                    }
                }
                else
                {
                    token.Dispose();
                    token = null;
                }
            });
        }


        public ICommand ToggleListen { get; }
        [Reactive] public bool UseContinuous { get; set; } = true;
        [Reactive] public string ListenText { get; private set; } = "Start Listening";
        [Reactive] public string Text { get; private set; }
    }
}
