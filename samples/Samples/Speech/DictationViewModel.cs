using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.SpeechRecognition;


namespace Samples.Speech
{
    public class DictationViewModel : ViewModel
    {
        public DictationViewModel(ISpeechRecognizer speech, IDialogs dialogs)
        {
            speech
                .WhenListeningStatusChanged()
                .SubOnMainThread(x => this.IsListening = x);


            this.ToggleListen = ReactiveCommand.Create(()  =>
            {
                if (this.IsListening)
                {
                    this.Deactivate();
                }
                else
                {
                    if (this.UseContinuous)
                    {
                        speech
                            .ContinuousDictation()
                            .SubOnMainThread(
                                x => this.Text += " " + x,
                                ex => dialogs.Alert(ex.ToString())
                            )
                            .DisposedBy(this.DeactivateWith);
                    }
                    else
                    {
                        speech
                            .ListenUntilPause()
                            .SubOnMainThread(
                                x => this.Text = x,
                                ex => dialogs.Alert(ex.ToString())
                            )
                            .DisposedBy(this.DeactivateWith);
                    }
                }
            });
        }


        public ICommand ToggleListen { get; }
        [Reactive] public bool IsListening { get; private set; }
        [Reactive] public bool UseContinuous { get; set; } = true;
        //[Reactive] public bool ListenInBackground { get; set; }
        [Reactive] public string Text { get; private set; }
    }
}
