using System;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushManager
    {
        /// <summary>
        /// This is an observable intended for use in the foreground
        /// </summary>
        /// <returns></returns>
        IObservable<PushNotification> WhenReceived();

        /// <summary>
        /// This is when the token was registered
        /// </summary>
        DateTime? CurrentRegistrationTokenDate { get; }

        /// <summary>
        /// The current registration token
        /// </summary>
        string? CurrentRegistrationToken { get; }

        /// <summary>
        /// Requests platform permission to send push notifications
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);

        /// <summary>
        /// Unregisters from push notifications
        /// </summary>
        /// <returns></returns>
        Task UnRegister();
    }
}
