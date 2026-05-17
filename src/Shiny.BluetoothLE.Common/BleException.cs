using System;


namespace Shiny.BluetoothLE
{
    /// <summary>
    /// Base exception type for all Shiny BluetoothLE failures.
    /// </summary>
    public class BleException : Exception
    {
        /// <summary>
        /// Creates a new instance with the supplied message.
        /// </summary>
        /// <param name="message">The error description.</param>
        public BleException(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance with the supplied message and inner exception.
        /// </summary>
        /// <param name="message">The error description.</param>
        /// <param name="exception">The underlying exception.</param>
        public BleException(string message, Exception exception) : base(message, exception) { }
    }
}
