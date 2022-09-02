using Shiny;
using System.Collections.Generic;
using System.Windows.Input;


namespace Sample
{
    public class LogsViewModel : ViewModel
    {
        public LogsViewModel(SampleSqliteConnection conn)
        {
            this.Load = this.LoadingCommand(async () =>
            {
                this.Events = await conn
                    .Events
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();
            });

            this.Clear = this.ConfirmCommand(
                "Are you sure you want to the logs?",
                async () =>
                {
                    await conn.DeleteAllAsync<ShinyEvent>();
                    this.Load.Execute(null);
                }
            );
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
