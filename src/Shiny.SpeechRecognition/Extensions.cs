using System;
using System.Reactive.Linq;

namespace Shiny.SpeechRecognition;

public static class Extensions
{
    /// <summary>
    /// Optimal observable for taking command (yes/no/maybe/go away/etc)
    /// </summary>
    /// <param name="keywords"></param>
    /// <returns></returns>
    public static IObservable<string?> ListenForFirstKeyword(this ISpeechRecognizer speechRecognizer, params string[] keywords)
        => speechRecognizer
            .ContinuousDictation()
            .Select(x =>
            {
                var values = x.Split(' ');
                foreach (var value in values)
                {
                    foreach (var keyword in keywords)
                    {
                        if (value.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                            return value;
                    }
                }
                return null;
            })
            .Where(x => x != null)
            .Take(1);

}
