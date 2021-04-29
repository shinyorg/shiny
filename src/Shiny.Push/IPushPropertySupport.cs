using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    public interface IPushPropertySupport
    {
        /// <summary>
        /// The list of available properties, if null, any property is supported
        /// </summary>
        string[]? SupportedProperties { get; }

        IReadOnlyDictionary<string, string> CurrentProperties { get; }

        void ClearProperties();
        void RemoveProperty(string property);
        void SetProperty(string property, string value);
    }
}
