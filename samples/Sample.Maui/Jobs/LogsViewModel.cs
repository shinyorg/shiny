using Shiny;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Forms;


namespace Sample
{
    public class LogsViewModel : SampleViewModel
    {
        public LogsViewModel()
        {
            var conn = ShinyHost.Resolve<SampleSqliteConnection>();
            this.Load = new Command(async () =>
            {
                this.IsBusy = true;
                this.Events = await conn
                    .Events
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();
                this.IsBusy = false;
            });

            this.Clear = new Command(async () =>
            {
                this.IsBusy = true;
                await conn.DeleteAllAsync<ShinyEvent>();
                this.Load.Execute(null);
            });
        }

        public ICommand Clear { get; }
        public ICommand Load { get; }


        List<ShinyEvent> events;
        public List<ShinyEvent> Events
        {
            get => this.events;
            set
            {
                this.events = value;
                this.RaisePropertyChanged();
            }
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}
