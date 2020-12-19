using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class CentralExtensionsViewModel : ViewModel
    {
        public CentralExtensionsViewModel(IBleManager centralManager,
                                          IDialogs dialogs)
        {
            this.Tasks = new List<TaskViewModel>
            {
                new TaskViewModel(
                    "Scan Find Peripheral",
                    ct => centralManager
                        .ScanUntilPeripheralFound(this.PeripheralName)
                        .ToTask(ct),

                    this.WhenAny(
                        x => x.PeripheralName,
                        x => !x.GetValue().IsEmpty()
                    )
                ),
                new TaskViewModel(
                    "Scan For Unique Peripherals",
                    ct => centralManager
                        .ScanForUniquePeripherals()
                        .ToTask(ct)
                ),
                new TaskViewModel(
                    "Scan Interval",
                    ct => centralManager
                        .ScanInterval(
                            TimeSpan.FromSeconds(10),
                            TimeSpan.FromSeconds(10)
                        )
                        .ToTask(ct)
                )
            };
        }


        public List<TaskViewModel> Tasks { get; }
        [Reactive] public string? PeripheralName { get; set; }
    }
}

