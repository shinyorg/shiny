using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AVFoundation;

using Foundation;

using Speech;
using UIKit;


namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : AbstractSpeechRecognizer
    {
        public override async Task<AccessState> RequestAccess()
        {
            var status = AccessState.Available;

            if (!UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                status = AccessState.NotSupported;
            }
            else
            {
                var nativeStatus = SFSpeechRecognizer.AuthorizationStatus;
                if (nativeStatus != SFSpeechRecognizerAuthorizationStatus.NotDetermined)
                    status = FromNative(nativeStatus);

                else
                {
                    var tcs = new TaskCompletionSource<AccessState>();
                    SFSpeechRecognizer.RequestAuthorization(x => tcs.SetResult(FromNative(x)));
                    status = await tcs.Task.ConfigureAwait(false);
                }
            }
            return status;
        }


        static AccessState FromNative(SFSpeechRecognizerAuthorizationStatus status)
        {
            switch (status)
            {
                case SFSpeechRecognizerAuthorizationStatus.Authorized:
                    return AccessState.Available;

                case SFSpeechRecognizerAuthorizationStatus.Denied:
                    return AccessState.Denied;

                case SFSpeechRecognizerAuthorizationStatus.Restricted:
                    return AccessState.Restricted;

                default:
                    return AccessState.Unknown;
            }
        }


        public override IObservable<string> ListenUntilPause(CultureInfo? culture = null) => this.Listen(true, culture);
        public override IObservable<string> ContinuousDictation(CultureInfo? culture = null) => this.Listen(false, culture);


        protected virtual IObservable<string> Listen(bool completeOnEndOfSpeech, CultureInfo? culture) => Observable.Create<string>(ob =>
        {
            var speechRecognizer = culture == null
                ? new SFSpeechRecognizer()
                : new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

            if (!speechRecognizer.Available)
                throw new ArgumentException("Speech recognizer is not available");

            var speechRequest = new SFSpeechAudioBufferRecognitionRequest();
            var audioEngine = new AVAudioEngine();
            var format = audioEngine.InputNode.GetBusOutputFormat(0);

            if (!completeOnEndOfSpeech)
                speechRequest.TaskHint = SFSpeechRecognitionTaskHint.Dictation;

            audioEngine.InputNode.InstallTapOnBus(
                0,
                1024,
                format,
                (buffer, when) => speechRequest.Append(buffer)
            );
            audioEngine.Prepare();
            audioEngine.StartAndReturnError(out var error);

            if (error != null)
                throw new ArgumentException("Error starting audio engine - " + error.LocalizedDescription);

            this.ListenSubject.OnNext(true);

            var currentIndex = 0;
            var cancel = false;
            var task = speechRecognizer.GetRecognitionTask(speechRequest, (result, err) =>
            {
                if (cancel)
                    return;

                if (err != null)
                {
                    ob.OnError(new Exception(err.LocalizedDescription));
                }
                else
                {
                    if (result.Final && completeOnEndOfSpeech)
                    {
                        currentIndex = 0;
                        ob.OnNext(result.BestTranscription.FormattedString);
                        ob.OnCompleted();
                    }
                    else
                    {
                        for (var i = currentIndex; i < result.BestTranscription.Segments.Length; i++)
                        {
                            var s = result.BestTranscription.Segments[i].Substring;
                            currentIndex++;
                            ob.OnNext(s);
                        }
                    }
                }
            });

            return () =>
            {
                cancel = true;
                task?.Cancel();
                task?.Dispose();
                audioEngine.Stop();
                audioEngine.InputNode?.RemoveTapOnBus(0);
                audioEngine.Dispose();
                speechRequest.EndAudio();
                speechRequest.Dispose();
                speechRecognizer.Dispose();
                this.ListenSubject.OnNext(false);
            };
        });
    }
}