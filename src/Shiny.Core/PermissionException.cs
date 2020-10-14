using System;


namespace Shiny
{
    public class PermissionException : Exception
    {
        public PermissionException(string module, AccessState badStatus) : base($"{module} had status of {badStatus}") { }
    }
}
