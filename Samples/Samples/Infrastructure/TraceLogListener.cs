#if DEBUG
using System.Diagnostics;
using Xamarin.Forms.Internals;


namespace Samples.Infrastructure
{
    public class TraceLogListener : LogListener
    {
        public override void Warning(string category, string message) =>
            Trace.WriteLine($"  {category}: {message}");
    }
}
#endif
