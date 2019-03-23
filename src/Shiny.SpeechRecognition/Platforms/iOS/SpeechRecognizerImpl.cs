using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AVFoundation;
using Speech;
using UIKit;


namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : AbstractSpeechRecognizer
    {
        public override IObservable<AccessState> RequestAccess() => Observable.Create<AccessState>(ob =>
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                ob.Respond(AccessState.NotSupported);

            else
            {
                var status = SFSpeechRecognizer.AuthorizationStatus;
                if (status != SFSpeechRecognizerAuthorizationStatus.NotDetermined)
                    ob.Respond(FromNative(status));

                else
                    SFSpeechRecognizer.RequestAuthorization(x => ob.Respond(FromNative(x)));
            }
            return Disposable.Empty;
        });


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


        public override IObservable<string> ListenUntilPause() => this.Listen(true);
        public override IObservable<string> ContinuousDictation() => this.Listen(false);


        protected virtual IObservable<string> Listen(bool completeOnEndOfSpeech) => Observable.Create<string>(ob =>
        {
            var speechRecognizer = new SFSpeechRecognizer();
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