using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Speech;


namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : AbstractSpeechRecognizer
    {
        readonly IAndroidContext context;
        readonly object syncLock = new object();


        public SpeechRecognizerImpl(IAndroidContext context) => this.context = context;

        public override async Task<AccessState> RequestAccess()
        {
            if (!SpeechRecognizer.IsRecognitionAvailable(this.context.AppContext))
                return AccessState.NotSupported;

            if (!this.context.IsInManifest(Manifest.Permission.RecordAudio))
                return AccessState.NotSetup;

            return await this.context.RequestAccess(Manifest.Permission.RecordAudio);
        }


        public override IObservable<string> ListenUntilPause(CultureInfo? culture) => Observable.Create<string>(ob =>
        {
            var final = "";
            var listener = new SpeechRecognitionListener
            {
                ReadyForSpeech = () => this.ListenSubject.OnNext(true),
                Error = ex => ob.OnError(new Exception("Failure in speech engine - " + ex)),
                PartialResults = sentence =>
                {
                    lock (this.syncLock)
                        final = sentence;
                },
                FinalResults = sentence =>
                {
                    lock (this.syncLock)
                        final = sentence;
                },
                EndOfSpeech = () =>
                {
                    lock (this.syncLock)
                    {
                        ob.OnNext(final);
                        ob.OnCompleted();
                        this.ListenSubject.OnNext(false);
                    }
                }
            };
            var speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.context.AppContext);
            speechRecognizer.SetRecognitionListener(listener);
            speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));

            return () =>
            {
                this.ListenSubject.OnNext(false);
                speechRecognizer.StopListening();
                speechRecognizer.Destroy();
            };
        });


        public override IObservable<string> ContinuousDictation(CultureInfo? culture = null) => Observable.Create<string>(ob =>
        {
            var stop = false;
            var currentIndex = 0;
            var speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(Application.Context);
            var listener = new SpeechRecognitionListener();

            listener.ReadyForSpeech = () => this.ListenSubject.OnNext(true);
            listener.PartialResults = sentence =>
            {
                lock (this.syncLock)
                {
                    sentence = sentence.Trim();
                    if (currentIndex > sentence.Length)
                        currentIndex = 0;

                    var newPart = sentence.Substring(currentIndex);
                    currentIndex = sentence.Length;
                    ob.OnNext(newPart);
                }
            };

            listener.EndOfSpeech = () =>
            {
                lock (this.syncLock)
                {
                    currentIndex = 0;
                    speechRecognizer.Destroy();

                    speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.context.AppContext);
                    speechRecognizer.SetRecognitionListener(listener);
                    speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));
                }
            };
            listener.Error = ex =>
            {
                switch (ex)
                {
                    case SpeechRecognizerError.Client:
                    case SpeechRecognizerError.RecognizerBusy:
                    case SpeechRecognizerError.SpeechTimeout:
                        lock (this.syncLock)
                        {
                            if (stop)
                                return;

                            speechRecognizer.Destroy();

                            speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.context.AppContext);
                            speechRecognizer.SetRecognitionListener(listener);
                            speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));
                        }
                        break;

                    default:
                        ob.OnError(new Exception($"Could not start speech recognizer - ERROR: {ex}"));
                        break;
                }
            };
            speechRecognizer.SetRecognitionListener(listener);
            speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));


            return () =>
            {
                stop = true;
                speechRecognizer?.StopListening();
                speechRecognizer?.Destroy();
                this.ListenSubject.OnNext(false);
            };
        });


        protected virtual Intent CreateSpeechIntent(bool partialResults, CultureInfo? culture)
        {
            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, Java.Util.Locale.Default);

            if (culture == null)
            {
                intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);
            }
            else
            {
                var javaLocale = Java.Util.Locale.ForLanguageTag(culture.Name);
                intent.PutExtra(RecognizerIntent.ExtraLanguage, javaLocale);
            }
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraCallingPackage, this.context.Package.PackageName);
            //intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            //intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
            //intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
            //intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
            intent.PutExtra(RecognizerIntent.ExtraPartialResults, partialResults);

            return intent;
        }
    }
}


