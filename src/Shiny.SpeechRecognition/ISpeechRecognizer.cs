using System;
using System.Globalization;
using System.Threading.Tasks;


namespace Shiny.SpeechRecognition
{
    public interface ISpeechRecognizer
    {
        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <returns></returns>
        Task<AccessState> RequestAccess();


        /// <summary>
        /// Optimal command for listening to a sentence.  Completes when user pauses
        /// </summary>
        /// <returns></returns>
        IObservable<string> ListenUntilPause(CultureInfo? culture = null);


        /// <summary>
        /// Continuous dictation.  Returns text as made available.  Dispose to stop dictation.
        /// </summary>
        /// <returns></returns>
        IObservable<string> ContinuousDictation(CultureInfo? culture = null);


        /// <summary>
        /// When listening status changes
        /// </summary>
        /// <returns></returns>
        IObservable<bool> WhenListeningStatusChanged();
    }
}
