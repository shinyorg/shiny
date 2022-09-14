using ReactiveUI;
using Shiny;
using Shiny.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Sample
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(IBleManager bleManager)
        {
            this.Functions = new List<CommandItem>
            {
                Create(
                    "GetAllCharacteristicAsync",
                    cmd => DoPeripheral(bleManager, cmd, async peripheral =>
                    {
                        var chs = await peripheral.GetAllCharacteristicsAsync();
                        cmd.Detail = $"Found {chs.Count} Characteristics";

                        chs.FirstOrDefault(x => x.Uuid.Equals("FFF1", StringComparison.InvariantCultureIgnoreCase)).Notify().Subscribe(x =>
                        {
                            cmd.Detail = Convert.ToBase64String(x.Data);
                        });
                        await chs.FirstOrDefault(x => x.Uuid.Equals("FFF2", StringComparison.InvariantCultureIgnoreCase)).WriteAsync(Encoding.ASCII.GetBytes("ATZ\n"), true);
                    })
                ),
                Create(
                    "ReadRssi",
                    cmd => DoPeripheral(bleManager, cmd, async peripheral =>
                    {
                        var rssi = await peripheral.ReadRssi();
                        cmd.Detail = "RSSI is " + rssi;
                    })
                )
            };

            this.Test = this.Functions[0].PrimaryCommand;
        }


        public List<CommandItem> Functions { get; }


        public ICommand Test { get; }

        async Task DoPeripheral(IBleManager bleManager, CommandItem cmd, Func<IPeripheral, Task> doWork)
        {
            IPeripheral? peripheral = null;
            try
            {
                cmd.Detail = "Searching for Peripheral";
                peripheral = await bleManager
                    .ScanUntilPeripheralFound(Constants.ScanPeripheralName)
                    .Timeout(TimeSpan.FromSeconds(20))
                    .ToTask();

                await peripheral.ConnectAsync();
                await doWork(peripheral).ConfigureAwait(false);
            }
            catch
            {
                cmd.Detail = String.Empty;
                throw;
            }
            finally
            {
                peripheral?.CancelConnection();
            }
        }



        CommandItem Create(string title, Func<CommandItem, Task> action)
        {
            var command = new CommandItem { Text = title };
            command.PrimaryCommand = ReactiveCommand.CreateFromTask(
                async () => await action(command).ConfigureAwait(false)
            );
            return command;
        }
    }
}
