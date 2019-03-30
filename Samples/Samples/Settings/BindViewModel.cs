using System;
using System.Reactive.Linq;
using Shiny.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Settings
{
    public class BindViewModel : ViewModel
    {
        readonly ISettings settings;


        public BindViewModel(ISettings settings)
        {
            this.settings = settings;
            this.WhenAnyValue(
                x => x.YourText,
                x => x.IsChecked
            )
            .Skip(1)
            .Subscribe(_ => this.LastUpdated = DateTime.Now);
        }


        [Reactive] public bool IsChecked { get; set; }
        [Reactive] public string YourText { get; set; }
        [Reactive] public DateTime? LastUpdated { get; set; }


        protected override void OnStart()
        {
            base.OnStart();
            this.settings.Bind(this, "AppSettings");
        }


        public override void Destroy()
        {
            base.Destroy();
            this.settings.UnBind(this);
        }
    }
}
