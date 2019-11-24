using System;
using System.Threading.Tasks;

namespace Shiny.Push
{
    public class PushNotificationManager : IPushNotificationManager
    {
        public Task<PushAccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}
