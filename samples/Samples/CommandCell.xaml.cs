using System;
using System.Windows.Input;
using Xamarin.Forms;


namespace Samples
{
    public partial class CommandCell : ContentView
    {
        public CommandCell()
        {
            this.InitializeComponent();
            this.PrimaryCommandText = "Run";
            this.PrimaryCommandColor = Color.DodgerBlue;

            this.SecondaryCommandText = "Cancel";
            this.SecondaryCommandColor = Color.Red;
        }


        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(CommandCell),
            String.Empty
        );
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }


        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(CommandCell),
            String.Empty
        );
        public string Detail
        {
            get => (string)this.GetValue(DetailProperty);
            set => this.SetValue(DetailProperty, value);
        }


        public static readonly BindableProperty PrimaryCommandTextProperty = BindableProperty.Create(
            nameof(PrimaryCommandText),
            typeof(string),
            typeof(CommandCell),
            "Run"
        );
        public string PrimaryCommandText
        {
            get => (string)this.GetValue(PrimaryCommandTextProperty);
            set => this.SetValue(PrimaryCommandTextProperty, value);
        }


        public static readonly BindableProperty PrimaryCommandColorProperty = BindableProperty.Create(
            nameof(PrimaryCommandColor),
            typeof(Color),
            typeof(CommandCell),
            Color.DodgerBlue
        );
        public Color PrimaryCommandColor
        {
            get => (Color)this.GetValue(PrimaryCommandColorProperty);
            set => this.SetValue(PrimaryCommandColorProperty, value);
        }


        public static readonly BindableProperty PrimaryCommandProperty = BindableProperty.Create(
            nameof(PrimaryCommandText),
            typeof(ICommand),
            typeof(CommandCell),
            null
        );
        public ICommand PrimaryCommand
        {
            get => (ICommand)this.GetValue(PrimaryCommandProperty);
            set => this.SetValue(PrimaryCommandProperty, value);
        }



        public static readonly BindableProperty SecondaryCommandTextProperty = BindableProperty.Create(
            nameof(SecondaryCommandText),
            typeof(string),
            typeof(CommandCell),
            "Cancel"
        );
        public string SecondaryCommandText
        {
            get => (string)this.GetValue(SecondaryCommandTextProperty);
            set => this.SetValue(SecondaryCommandTextProperty, value);
        }


        public static readonly BindableProperty SecondaryCommandColorProperty = BindableProperty.Create(
            nameof(SecondaryCommandColor),
            typeof(Color),
            typeof(CommandCell),
            Color.Red
        );
        public Color SecondaryCommandColor
        {
            get => (Color)this.GetValue(SecondaryCommandColorProperty);
            set => this.SetValue(SecondaryCommandColorProperty, value);
        }


        public static readonly BindableProperty SecondaryCommandProperty = BindableProperty.Create(
            nameof(SecondaryCommand),
            typeof(ICommand),
            typeof(CommandCell),
            null
        );
        public ICommand SecondaryCommand
        {
            get => (ICommand)this.GetValue(SecondaryCommandProperty);
            set => this.SetValue(SecondaryCommandProperty, value);
        }
    }
}
