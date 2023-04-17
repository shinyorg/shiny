using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Speech;
using Microsoft.Extensions.Logging;

namespace Shiny.SpeechRecognition;


public class SpeechRecognizerImpl : Java.Lang.Object, ISpeechRecognizer, IRecognitionListener
{
    readonly Subject<bool> listenSubj = new();
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    readonly object syncLock = new();


    public SpeechRecognizerImpl(AndroidPlatform platform, ILogger<SpeechRecognizerImpl> logger)
    {
        this.platform = platform;
        this.logger = logger;
    }


    public IObservable<bool> WhenListeningStatusChanged() => this.listenSubj;


    public async Task<AccessState> RequestAccess()
    {
        if (!SpeechRecognizer.IsRecognitionAvailable(this.platform.AppContext))
            return AccessState.NotSupported;

        if (!this.platform.IsInManifest(Manifest.Permission.RecordAudio))
            return AccessState.NotSetup;

        return await this.platform.RequestAccess(Manifest.Permission.RecordAudio);
    }


    public IObservable<string> ListenUntilPause(CultureInfo? culture) => Observable
        .FromAsync(async ct => (await this.RequestAccess()).Assert())
        .Select(x => Observable.Create<string>(ob =>
        {
            var final = "";
            this.onError = ex => ob.OnError(new InvalidOperationException("Failure in speech engine - " + ex));
            this.onPartialResult = sentence =>
            {
                lock (this.syncLock)
                    final = sentence;
            };

            this.onResults = sentence =>
            {
                lock (this.syncLock)
                    final = sentence;
            };
            this.onEndOfSpeech = () =>
            {
                lock (this.syncLock)
                {
                    ob.OnNext(final);
                    ob.OnCompleted();
                }
            };
            var speechRecognizer = this.CreateSpeechRecognizer(true, culture);

            return () =>
            {
                this.listenSubj.OnNext(false);
                if (speechRecognizer != null)
                {
                    speechRecognizer.StopListening();
                    speechRecognizer.Destroy();
                }
            };
        }))
        .Switch();


    public IObservable<string> ContinuousDictation(CultureInfo? culture = null) => Observable.Create<string>(ob =>
    {
        var currentIndex = 0;
        SpeechRecognizer speechRecognizer = null!;

        this.onPartialResult = sentence =>
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

        this.onEndOfSpeech = () =>
        {
            lock (this.syncLock)
            {
                currentIndex = 0;
                speechRecognizer.Destroy();

                // TODO: sometimes this will lock into NoMatch exception
                speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.platform.AppContext)!;
                //if (SpeechRecognizer.IsRecognitionAvailable)
                speechRecognizer.SetRecognitionListener(this);
                speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));
            }
        };
        this.onError = ex =>
        {
            switch (ex)
            {
                case SpeechRecognizerError.Client:
                case SpeechRecognizerError.RecognizerBusy:
                case SpeechRecognizerError.SpeechTimeout:
                    lock (this.syncLock)
                    {
                        speechRecognizer.Destroy();

                        speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.platform.AppContext)!;
                        speechRecognizer.SetRecognitionListener(this);
                        speechRecognizer.StartListening(this.CreateSpeechIntent(true, culture));
                    }
                    break;

                default:
                    ob.OnError(new Exception($"Could not start speech recognizer - ERROR: {ex}"));
                    break;
            }
        };

        speechRecognizer = this.CreateSpeechRecognizer(true, culture);

        return () =>
        {
            this.ResetHooks();
            speechRecognizer?.StopListening();
            speechRecognizer?.Destroy();
            this.listenSubj.OnNext(false);
        };
    });


    protected SpeechRecognizer CreateSpeechRecognizer(bool partialResults, CultureInfo? culture)
    {
        var speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this.platform.AppContext)!;
        speechRecognizer.SetRecognitionListener(this);
        speechRecognizer.StartListening(this.CreateSpeechIntent(partialResults, culture));
        return speechRecognizer;
    }

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
        intent.PutExtra(RecognizerIntent.ExtraCallingPackage, this.platform.AppContext.PackageName);
        //intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
        //intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
        //intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
        //intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
        intent.PutExtra(RecognizerIntent.ExtraPartialResults, partialResults);

        return intent;
    }


    protected void ResetHooks()
    {
        this.onBeginningOfSpeech = null;
        this.onBufferReceived = null;
        this.onEndOfSpeech = null;
        this.onError = null;
        this.onReadyForSpeech = null;
        this.onPartialResult = null;
        this.onResults = null;
        this.onRmsChanged = null;
    }


    Action? onBeginningOfSpeech;
    public void OnBeginningOfSpeech()
    {
        this.logger.LogDebug("Beginning of Speech");
        this.onBeginningOfSpeech?.Invoke();
    }


    Action<byte[]?>? onBufferReceived;
    public void OnBufferReceived(byte[]? buffer)
    {
        this.logger.LogDebug("Buffer Received");
        this.onBufferReceived?.Invoke(buffer);
    }


    Action? onEndOfSpeech;
    public void OnEndOfSpeech()
    {
        this.logger.LogDebug("End of Speech");
        this.onEndOfSpeech?.Invoke();
    }


    Action<SpeechRecognizerError>? onError;
    public void OnError(SpeechRecognizerError error)
    {
        this.logger.LogDebug("Error: " + error);
        this.onError?.Invoke(error);
    }


    public void OnEvent(int eventType, Bundle? @params)
        => this.logger.LogDebug("OnEvent: " + eventType);


    Action<Bundle?>? onReadyForSpeech;
    public void OnReadyForSpeech(Bundle? @params)
    {
        this.logger.LogDebug("Ready for Speech");
        this.onReadyForSpeech?.Invoke(@params);
        this.listenSubj.OnNext(true);
    }


    Action<string?>? onPartialResult;
    public void OnPartialResults(Bundle? bundle)
    {
        this.logger.LogDebug("OnPartialResults");
        if (this.onPartialResult != null)
            this.SendResults(bundle, s => this.onPartialResult.Invoke(s));
    }


    Action<string?>? onResults;
    public void OnResults(Bundle? bundle)
    {
        this.logger.LogDebug("Speech Results");
        if (this.onResults != null)
            this.SendResults(bundle, s => this.onResults(s));
    }


    Action<float>? onRmsChanged;
    public void OnRmsChanged(float rmsdB)
    {
        this.logger.LogDebug("RMS Changed: " + rmsdB);
        this.onRmsChanged?.Invoke(rmsdB);
    }


    protected void SendResults(Bundle? bundle, Action<string>? action)
    {
        if (bundle == null)
            return;
        
        var matches = bundle.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
        if (matches == null || matches.Count == 0)
        {
            this.logger.LogDebug("Matches value is null in bundle");
            return;
        }
        foreach (var match in matches)
            action?.Invoke(match);

        //var scores = bundle.GetFloatArray(SpeechRecognizer.ConfidenceScores);
        //if (scores != null)
        //{
        //    var best = 0;
        //    for (var i = 0; i < scores.Length; i++)
        //    {
        //        if (scores[best] < scores[i])
        //            best = i;
        //    }
        //    var winner = matches[best];
        //}
        //else
        //{

        //}
        //action?.Invoke(winner);
    }
}