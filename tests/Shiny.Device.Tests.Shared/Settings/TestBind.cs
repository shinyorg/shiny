using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Shiny.Device.Tests.Settings
{
    public class TestBind : INotifyPropertyChanged
    {
        string stringProperty;
        public string StringProperty
        {
            get => this.stringProperty;
            set
            {
                this.stringProperty = value;
                this.OnPropertyChanged();
            }
        }


        Guid? nullProperty;
        public Guid? NullableProperty
        {
            get => this.nullProperty;
            set
            {
                this.nullProperty = value;
                this.OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
