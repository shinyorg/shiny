using System;


namespace Shiny
{
    /// <summary>
    /// Thrown when a Shiny module cannot proceed because the platform reported a non-<see cref="AccessState.Available"/> permission state.
    /// </summary>
    public class PermissionException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="PermissionException"/> describing which module was denied and the reported access state.
        /// </summary>
        /// <param name="module">The Shiny module or feature name that requested access (e.g. "Notifications", "GPS").</param>
        /// <param name="badStatus">The non-available <see cref="AccessState"/> returned by the platform.</param>
        public PermissionException(string module, AccessState badStatus) : base($"{module} had status of {badStatus}") { }
    }
}
