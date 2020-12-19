using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using ImTools;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Samples.Models;
using Samples.ShinyDelegates;

using Shiny.Locations;
using Shiny.Locations.Sync;


namespace Samples.LocationSync
{
    public class ActionsViewModel : ViewModel
    {
        readonly SampleSqliteConnection conn;
        readonly LocationSyncDelegates syncDelegate;
        readonly ILocationSyncManager syncManager;
        readonly IDialogs dialogs;


        public ActionsViewModel(SampleSqliteConnection conn, 
                                LocationSyncDelegates syncDelegate, 
                                ILocationSyncManager syncManager,
                                IDialogs dialogs)
        {
            this.conn = conn;
            this.syncDelegate = syncDelegate;
            this.syncManager = syncManager;
            this.dialogs = dialogs;

            this.ProcessGeofences = this.ProcessCommand(LocationSyncType.Geofence);
            this.ClearGeofences = this.ClearCommand(LocationSyncType.Geofence);

            this.ProcessGps = this.ProcessCommand(LocationSyncType.GPS);
            this.ClearGps = this.ClearCommand(LocationSyncType.GPS);
        }


        public ICommand ClearGeofences { get; }
        public ICommand ProcessGeofences { get; }
        public ICommand ClearGps { get; }
        public ICommand ProcessGps { get; }

        [Reactive] public bool IsGeofenceSyncEnabled { get; set; }
        [Reactive] public bool IsGpsSyncEnabled { get; set; }
        [Reactive] public bool IsSyncDelegateCrashEnabled { get; set; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            this.IsSyncDelegateCrashEnabled = this.syncDelegate.IsSyncDelegateCrashEnabled;
            this.WhenAnyValue(x => x.IsSyncDelegateCrashEnabled)
                .Skip(1)
                .Subscribe(x => this.syncDelegate.IsSyncDelegateCrashEnabled = x)
                .DisposeWith(this.DeactivateWith);

            this.SetGps();
            this.SetGeofence();
        }


        async Task SetGeofence()
        {
            this.IsGeofenceSyncEnabled = await this.syncManager.IsMonitoring(LocationSyncType.Geofence);
            this.WhenAnyValue(x => x.IsGeofenceSyncEnabled)
                .Skip(1)
                .Subscribe(async enabled =>
                {
                    try
                    { 
                        if (enabled)
                        {
                            await this.dialogs.Alert("Make sure to go to the geofence menu and add a new geofence", "Info");
                        }
                        else
                        {
                            await this.syncManager.StopMonitoring(LocationSyncType.Geofence);
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.dialogs.Alert(ex.ToString());
                    }
                })
                .DisposeWith(this.DeactivateWith);
        }


        async Task SetGps()
        {
            this.IsGpsSyncEnabled = await this.syncManager.IsMonitoring(LocationSyncType.GPS);
            this.WhenAnyValue(x => x.IsGeofenceSyncEnabled)
                .Skip(1)
                .Subscribe(async enabled =>
                {
                    try
                    {
                        if (enabled)
                        {
                            await this.syncManager.StartGpsMonitoring(GpsRequest.Realtime(true));
                        }
                        else
                        {
                            await this.syncManager.StopMonitoring(LocationSyncType.Geofence);
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.dialogs.Alert(ex.ToString());
                    }
                })
                .DisposeWith(this.DeactivateWith);
        }

        ICommand ProcessCommand(LocationSyncType syncType) => ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await this.dialogs.Confirm("Run the sync?");
            if (!result)
                return;

            await this.dialogs.LoadingTask(() => this.syncManager.ForceRun(syncType), "Running Sync Processes");
            await this.dialogs.Snackbar("Sync Process Complete");
        });


        ICommand ClearCommand(LocationSyncType syncType) => ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await this.dialogs.Confirm("Are you sure you want to delete these events?");
            if (!result)
                return;

            await conn.DeleteAllAsync<LocationSyncEvent>();
            await syncManager.ClearEvents(syncType);
            await this.dialogs.Snackbar("Events Cleared");
        });
    }
}
