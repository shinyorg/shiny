using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using WinSpeechRecognizer = Windows.Media.SpeechRecognition.SpeechRecognizer;
using System.Globalization;

namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : AbstractSpeechRecognizer
    {
        const int NoCaptureDevicesHResult = -1072845856;


        public override async Task<AccessState> RequestAccess()
        {
            var access = AccessState.Available;
            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    MediaCategory = MediaCategory.Speech
                };
                using (var capture = new MediaCapture())
                    await capture.InitializeAsync(settings);
            }
            catch (TypeLoadException)
            {
                access = AccessState.NotSupported;
            }
            catch (UnauthorizedAccessException)
            {
                access = AccessState.Denied;
            }
            catch (Exception ex)
            {
                // Thrown when an audio capture device is not present.
                access = ex.HResult == NoCaptureDevicesHResult
                    ? AccessState.NotSupported
                    : AccessState.Unknown;
            }
            return access;
        }


        public override IObservable<string> ListenUntilPause(CultureInfo? culture = null) => Observable.FromAsync(async ct =>
        {
            var speech = this.Create(culture);

            await speech.CompileConstraintsAsync();
            this.ListenSubject.OnNext(true);
            var result = await speech.RecognizeAsync();
            this.ListenSubject.OnNext(false);

            return result.Text;
        });


        public override IObservable<string> ContinuousDictation(CultureInfo? culture = null) => Observable.Create<string>(async ob =>
        {
            var speech = this.Create(culture);
            await speech.CompileConstraintsAsync();

            var handler = new TypedEventHandler<SpeechContinuousRecognitionSession, SpeechContinuousRecognitionResultGeneratedEventArgs>((sender, args) =>
                ob.OnNext(args.Result.Text)
            );
            speech.ContinuousRecognitionSession.ResultGenerated += handler;
            await speech.ContinuousRecognitionSession.StartAsync();
            this.ListenSubject.OnNext(true);

            return () =>
            {
                //speech.ContinuousRecognitionSession.StopAsync();
                speech.ContinuousRecognitionSession.ResultGenerated -= handler;
                this.ListenSubject.OnNext(false);
                speech.Dispose();
            };
        });



        protected WinSpeechRecognizer Create(CultureInfo? culture)
        {
            if (culture == null)
                return new WinSpeechRecognizer();

            var lang = new Windows.Globalization.Language(culture.Name);
            return new WinSpeechRecognizer(lang);
        }
        //        //{
        //        //    var grammar = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "webSearch");
        //        //    speech.UIOptions.AudiblePrompt = "Say what you want to search for...";
        //        //    speech.UIOptions.ExampleText = @"Ex. &#39;weather for London&#39;";
        //        //    speech.Constraints.Add(webSearchGrammar);
        //        //}
        //        //speech.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.FromDays(1)
    }
}