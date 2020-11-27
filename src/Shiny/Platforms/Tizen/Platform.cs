using System;
using System.Threading.Tasks;
using Tizen.Security;
using Tizen.System;


namespace Shiny
{
    public enum PlatformNamespace
    {
        System,
        Feature
    }


    public static class Platform
    {
        public static T Get<T>(string item, PlatformNamespace ns = PlatformNamespace.Feature)
        {
            var uri = $"http://tizen.org/{ns.ToString().ToLower()}/{item}";
            Information.TryGetValue<T>(uri, out var value);
            return value;
        }


        public const string Location = "location";
        public const string Health = "healthinfo";

        static AccessState ToAccessState(this CheckResult check)
        {
            switch (check)
            {
                case CheckResult.Allow:
                    return AccessState.Available;

                case CheckResult.Deny:
                    return AccessState.Denied;

                case CheckResult.Ask:
                default:
                    return AccessState.Unknown;
            }
        }


        public static AccessState GetCurrentAccess(string permission) => PrivacyPrivilegeManager
            .CheckPermission("http://tizen.org/privilege/" + permission)
            .ToAccessState();


        public static bool HasPermission(string permission)
            => GetCurrentAccess(permission) == AccessState.Available;


        public static async Task<AccessState> RequestAccess(string permission)
        {
            var priv = "http://tizen.org/privilege/" + permission;
            var result = PrivacyPrivilegeManager
                .CheckPermission(priv)
                .ToAccessState();

            if (result == AccessState.Unknown)
                result = await RequestPermission(priv);

            return result;
        }


        static async Task<AccessState> RequestPermission(string priv)
        {
            var result = AccessState.Unknown;
            var tcs = new TaskCompletionSource<AccessState>();
            var handler = new EventHandler<RequestResponseEventArgs>((sender, args) =>
            {
                switch (args.result)
                {
                    case RequestResult.AllowForever:
                        tcs.TrySetResult(AccessState.Available);
                        break;

                    default:
                        tcs.TrySetResult(AccessState.Denied);
                        break;
                }
            });
            PrivacyPrivilegeManager.ResponseContext? context = null;

            try
            {
                PrivacyPrivilegeManager.GetResponseContext(priv).TryGetTarget(out context);

                if (context == null)
                    tcs.SetResult(AccessState.NotSupported);
                else
                {
                    context.ResponseFetched += handler;
                    PrivacyPrivilegeManager.RequestPermission(priv);
                }
                result = await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                if (context != null)
                    context.ResponseFetched -= handler;
            }
            return result;
        }
    }
}
