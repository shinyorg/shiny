using System;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;

namespace Samples
{
    public class MainViewModel : ViewModel
    {
        readonly SampleSqliteConnection conn;


        public MainViewModel(INavigationService navigationService, SampleSqliteConnection conn)
        {
            this.conn = conn;
            this.Navigate = ReactiveCommand.CreateFromTask<string>(async arg =>
            {
                this.IsPresented = false;
                await navigationService.Navigate(arg);
            });
        }


        public ICommand Navigate { get; }
        [Reactive] public bool IsPresented { get; set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            var conn2 = ShinyHost.Resolve<SampleSqliteConnection>();
            var same = Object.ReferenceEquals(this.conn, conn2);
            var hash1 = this.conn.GetHashCode();
            var hash2 = conn2.GetHashCode();
            Console.WriteLine($"Same: {same} - 1: {hash1} - 2: {hash2}");
        }
    }
}
