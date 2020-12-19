using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Nfc;


namespace Samples.Nfc
{
    public class NfcReadViewModel : ViewModel
    {
        readonly IDialogs dialogs;


        public NfcReadViewModel(IDialogs dialogs, INfcManager? nfcManager = null)
        {
            this.dialogs = dialogs;

            this.CheckPermission = ReactiveCommand.CreateFromTask(
                () => this.DoCheckPermission(nfcManager)
            );

            this.Clear = ReactiveCommand.Create(() =>
                this.NDefRecords.Clear()
            );

            this.Read = ReactiveCommand.CreateFromTask(
                async () =>
                { 
                    await this.DoCheckPermission(nfcManager);
                    if (this.Access == AccessState.Available)
                        this.ManageObservable(nfcManager.SingleRead());
                },
                this.WhenAny(
                    x => x.IsListening,
                    x => !x.GetValue()
                )
            );

            this.Continuous = ReactiveCommand.CreateFromTask(async () =>
            {
                await this.DoCheckPermission(nfcManager);
                if (this.Access == AccessState.Available)
                {
                    if (this.IsListening)
                    {
                        this.IsListening = false;
                        this.Deactivate();
                    }
                    else
                    {
                        this.ManageObservable(nfcManager.ContinuousRead());
                    }
                }
            });
        }



        public ICommand Clear { get; }
        public ICommand Continuous { get; }
        public ICommand Read { get; }
        public ICommand CheckPermission { get; }
        public ObservableList<NDefItemViewModel> NDefRecords { get; } = new ObservableList<NDefItemViewModel>();
        [Reactive] public AccessState Access { get; private set; } = AccessState.Unknown;
        [Reactive] public bool IsListening { get; private set; }


        void ManageObservable(IObservable<NDefRecord[]> obs)
        {
            obs
                .SelectMany(x => x.Select(y => new NDefItemViewModel(y)))
                .SubOnMainThread(
                    x => this.NDefRecords.Add(x),
                    async ex =>
                    {
                        this.Deactivate();
                        this.IsListening = false;
                        await this.dialogs.Alert(ex.ToString());
                    },
                    () =>
                    {
                        this.IsListening = false;
                        this.Deactivate();
                    }
                )
                .DisposeWith(this.DeactivateWith);

            this.IsListening = true;
        }

        async Task DoCheckPermission(INfcManager? manager = null)
        {
            if (manager == null)
                this.Access = AccessState.NotSupported;
            else
                this.Access = await manager.RequestAccess().ToTask();
        }
    }
}
