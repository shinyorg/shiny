using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Tizen.Uix.Stt;

namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : AbstractSpeechRecognizer
    {
        public override IObservable<string> ContinuousDictation() => this.Listen(RecognitionType.Free);
        public override IObservable<string> ListenUntilPause() => this.Listen(RecognitionType.Partial, SilenceDetection.True);

        //http://tizen.org/feature/speech.recognition
        public override Task<AccessState> RequestAccess() => Platform.RequestAccess("speech.recognition");


        IObservable<string> Listen(RecognitionType type, SilenceDetection? silence = null) => Observable.Create<string>(ob =>
        {
            var handler = new EventHandler<RecognitionResultEventArgs>((sender, args) =>
            {
                //args.Result == ResultEvent.ResultEvent.FinalResult
                //args.Result == ResultEvent.PartialResult
            });

            var client = new SttClient();
            client.RecognitionResult += handler;
            client.Start("en-US", type);
            if (silence != null)
                client.SetSilenceDetection(silence.Value);

            return () =>
            {
                client.Stop();
                client.RecognitionResult -= handler;
            };
        });
    }
}
