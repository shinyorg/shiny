using System;
using Prism.Navigation;
using ReactiveUI.Fody.Helpers;


namespace Samples.Logging
{
    public class BigTextViewViewModel : ViewModel
    {
        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            this.Text = parameters.GetValue<string>("Text");
            this.Title = parameters.GetValue<string>("Title") ?? "Text";
        }


        [Reactive] public string? Text { get; private set; }
    }
}
