using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Shiny.Tests.Core.Stores
{
    public class TestBind : NotifyPropertyChanged
    {
        string? stringProperty;
        public string? StringProperty
        {
            get => this.stringProperty;
            set => this.Set(ref this.stringProperty, value);
        }


        Guid? nullProperty;
        public Guid? NullableProperty
        {
            get => this.nullProperty;
            set => this.Set(ref this.nullProperty, value);
        }


        int intValue;
        public int IntValue
        {
            get => this.intValue;
            set => this.Set(ref this.intValue, value);
        }
    }
}
