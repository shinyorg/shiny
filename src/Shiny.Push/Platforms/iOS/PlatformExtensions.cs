using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public static class PlatformExtensions
    {
        /// <summary>
        /// Attempts to auto-start iOS by requesting access and checking if reg token is changed, if token changes, it also fires the delegate event
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="container"></param>
        /// <param name="logger"></param>
        /// <returns>Returns true if token is changed</returns>
        public static async Task<bool> AutoStartIfApplicable(this INativeAdapter adapter,
                                                             PushContainer container,
                                                             ILogger logger)
        {
            var changed = false;
            if (!container.CurrentRegistrationToken.IsEmpty())
            {
                logger.LogInformation("Container has active push registration token - attempting to start");
                var result = await adapter.RequestAccess();
                logger.LogDebug($"Auto-Start Permission: {result.Status} - {result.RegistrationToken}");

                if (result.RegistrationToken.IsEmpty())
                {
                    logger.LogDebug("No registration token - removing stored tokens");
                    container.ClearRegistration();
                }
                else if (result.RegistrationToken?.Equals(container.CurrentRegistrationToken) ?? false)
                {
                    logger.LogInformation("Push registration token updated");
                    container.SetCurrentToken(result.RegistrationToken!, true);
                    await container.OnTokenRefreshed(container.CurrentRegistrationToken!);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
