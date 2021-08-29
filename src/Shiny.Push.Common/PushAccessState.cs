using System;


namespace Shiny.Push
{
    public struct PushAccessState
    {
        public static PushAccessState Denied { get; } = new PushAccessState(AccessState.Denied, null);


        public PushAccessState(AccessState status, string? registrationToken)
        {
            this.Status = status;
            this.RegistrationToken = registrationToken;
        }


        public AccessState Status { get; }
        public string? RegistrationToken { get; }
    }
}
