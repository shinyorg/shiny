using System;
using Shiny.Stores;


namespace Shiny.Tests.Core.Stores
{
    [ObjectStoreBinder("file")]
    public class AttributeTestBind : NotifyPropertyChanged
    {
        string? testString;
        public string? TestString
        {
            get => this.testString;
            set => this.Set(ref this.testString, value);
        }
    }
}
