using System;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushManager : IPushManager
    {
        public Task<PushAccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}
