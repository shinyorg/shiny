//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Shiny.Push;


//namespace Shiny
//{
//    public static class ShinyPush
//    {
//        public static IPushManager Current { get; } = ShinyHost.Resolve<IPushManager>();

//        public static IObservable<IDictionary<string, string>> WhenReceived() => Current.WhenReceived();
//        public static Task<PushAccessState> RequestAccess() => Current.RequestAccess();
//        public static string? CurrentRegistrationToken => Current.CurrentRegistrationToken;
//        public static DateTime? CurrentRegistrationTokenDate => Current.CurrentRegistrationTokenDate;
//    }
//}
