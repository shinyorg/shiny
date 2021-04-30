using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    public interface IPushPropertySupport
    {
        IReadOnlyDictionary<string, string> CurrentProperties { get; }

        void ClearProperties();
        void RemoveProperty(string property);
        void SetProperty(string property, string value);
    }
}
