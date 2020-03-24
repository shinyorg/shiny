using System;
using System.Collections.Generic;


namespace $safeprojectname$.Mocks
{
    public class MockLocalize : ILocalize
    {
        public IDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();


        public string this[string key]
        {
            get => this.Values[key];
            set => this.Values[key] = value;
        }


        public string GetEnumString(Enum value) => this.Values[""];
    }
}
