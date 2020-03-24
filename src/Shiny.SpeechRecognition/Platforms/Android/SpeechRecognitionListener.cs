using System;
using System.Linq;
using Android.OS;
using Android.Speech;
using Debug = System.Diagnostics.Debug;


namespace Shiny.SpeechRecognition
{
    public class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
    {
        public Action? StartOfSpeech { get; set; }
        public Action? EndOfSpeech { get; set; }
        public Action? ReadyForSpeech { get; set; }
        public Action<SpeechRecognizerError>? Error { get; set; }
        public Action<string>? FinalResults { get; set; }
        public Action<string>? PartialResults { get; set; }
        public Action<float>? RmsChanged { get; set; }


        public void OnBeginningOfSpeech()
        {
            Debug.WriteLine("Beginning of Speech");
            this.StartOfSpeech?.Invoke();
        }


        public void OnBufferReceived(byte[] buffer) => Debug.WriteLine("Buffer Received");


        public void OnEndOfSpeech()
        {
            Debug.WriteLine("End of Speech");
            this.EndOfSpeech?.Invoke();
        }


        public void OnError(SpeechRecognizerError error)
        {
            Debug.WriteLine("Error: " + error);
            this.Error?.Invoke(error);
        }


        public void OnEvent(int eventType, Bundle @params) => Debug.WriteLine("OnEvent: " + eventType);


        public void OnReadyForSpeech(Bundle @params)
        {
            Debug.WriteLine("Ready for Speech");
            this.ReadyForSpeech?.Invoke();
        }


        public void OnPartialResults(Bundle bundle)
        {
            Debug.WriteLine("OnPartialResults");
            this.SendResults(bundle, this.PartialResults);
        }


        public void OnResults(Bundle bundle)
        {
            Debug.WriteLine("Speech Results");
            this.SendResults(bundle, this.FinalResults);
        }


        public void OnRmsChanged(float rmsdB)
        {
            Debug.WriteLine("RMS Changed: " + rmsdB);
            this.RmsChanged?.Invoke(rmsdB);
        }


        void SendResults(Bundle bundle, Action<string>? action)
        {
            var matches = bundle.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches == null || matches.Count == 0)
            {
                Debug.WriteLine("Matches value is null in bundle");
                return;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich && matches.Count > 1)
            {
                var scores = bundle.GetFloatArray(SpeechRecognizer.ConfidenceScores);
                var best = 0;
                for (var i = 0; i < scores.Length; i++)
                {
                    if (scores[best] < scores[i])
                        best = i;
                }
                var winner = matches[best];
                action?.Invoke(winner);
            }
            else
            {
                action?.Invoke(matches.First());
            }
        }
    }
}