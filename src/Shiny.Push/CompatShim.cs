using System;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny
{
    public static class CrossPush
    {
        static IPushManager Current { get; } = ShinyHost.Resolve<IPushManager>();

        public static Task<PushAccessState> RequestAccess() => Current.RequestAccess();
    }
}
