using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public static class Extensions
    {
        /// <summary>
        /// Asserts necessary status for push notifications to operate
        /// </summary>
        /// <param name="state"></param>
        public static void Assert(this PushAccessState state)
        {
            if (state.Status != AccessState.Available)
                throw new PermissionException("Push registration fail", state.Status);
        }
    }
}
