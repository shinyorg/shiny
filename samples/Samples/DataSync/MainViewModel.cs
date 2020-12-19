using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.DataSync;
using Samples.Infrastructure;
using Prism.Navigation;
using Bogus;


namespace Samples.DataSync
{
    public class MyEntity : ISyncEntity
    {
        public string EntityId { get; set; } = Guid.NewGuid().ToString();
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }


    public class MainViewModel : ViewModel
    {
        readonly IDataSyncManager manager;
        readonly INavigationService navigator;


        public MainViewModel(IDataSyncManager manager,
                             SampleDataSyncDelegate sdelegate,
                             INavigationService navigator,
                             IDialogs dialogs,
                             Shiny.IMessageBus messageBus)
        {
            this.manager = manager;
            this.navigator = navigator;

            var faker = new Faker<MyEntity>()
                .RuleFor(x => x.FirstName, (f, _) => f.Name.FirstName())
                .RuleFor(x => x.LastName, (f, _) => f.Name.LastName());

            this.IsSyncEnabled = manager.Enabled;
            this.AllowOutgoing = sdelegate.AllowOutgoing;

            this.GenerateTestItem = ReactiveCommand.CreateFromTask(async () =>
            {
                var entity = faker.Generate(1).First();
                await manager.Save(entity, SyncOperation.Create);
                await this.BindList();
            });

            this.ForceRun = ReactiveCommand.CreateFromTask(() =>
                //dialogs.LoadingTask(() => manager.ForceRun())
                manager.ForceRun()
            );

            this.Clear = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dialogs.Confirm("Are you sure you wish to clear the data sync pending queue?");
                if (result)
                {
                    await this.manager.ClearPending();
                    await this.BindList();
                }
            });

            this.WhenAnyValue(x => x.AllowOutgoing)
                .Skip(1)
                .Subscribe(x => sdelegate.AllowOutgoing = x)
                .DisposeWith(this.DestroyWith);

            this.WhenAnyValue(x => x.IsSyncEnabled)
                .Skip(1)
                .Subscribe(x => manager.Enabled = x)
                .DisposeWith(this.DestroyWith);

            messageBus
                .Listener<SyncItem>()
                .SubOnMainThread(x => this.Remove(x.Id))
                .DisposeWith(this.DestroyWith);
        }


        public ICommand GenerateTestItem { get; }
        public ICommand ForceRun { get; }
        public ICommand Clear { get; }
        public ObservableList<CommandItem> SyncEvents { get; } = new ObservableList<CommandItem>();
        [Reactive] public bool IsSyncEnabled { get; set; }
        [Reactive] public bool AllowOutgoing { get; set; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.BindList();
        }


        async Task Remove(Guid syncId)
        {
            var e = this.SyncEvents.FirstOrDefault(y => ((SyncItem)y.Data).Id == syncId);
            if (e != null)
                this.SyncEvents.Remove(e);
        }


        async Task BindList()
        {
            var items = await this.manager.GetPendingItems();
            var commands = items
                .Select(x => new CommandItem
                {
                    Text = $"[{x.Operation}] {x.TypeName}:{x.EntityId}",
                    Detail = $"[{x.Operation}]",
                    Data = x,
                    PrimaryCommand = ReactiveCommand.CreateFromTask(() =>
                        this.navigator.ShowBigText(
                            x.SerializedEntity,
                            "Data Sync"
                        )
                    ),
                    SecondaryCommand = ReactiveCommand.CreateFromTask(() =>
                        this.manager.Remove(x.Id)
                    )
                })
                .ToList();

            this.SyncEvents.ReplaceAll(commands);
        }
    }
}
