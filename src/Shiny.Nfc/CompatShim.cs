using System;
using System.Threading.Tasks;
using Shiny.Nfc;


namespace Shiny
{
    public static class CrossNfc
    {
        static INfcManager Current { get; } = ShinyHost.Resolve<INfcManager>();

        public static bool IsListening => Current.IsListening;
        public static Task<AccessState> RequestAccess(bool forWrite = false) => Current.RequestAccess(forWrite);
        public static void StartListener() => Current.StartListener();
        public static void StopListening() => Current.StopListening();
    }
}
