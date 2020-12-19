using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.BluetoothLE;
using Samples.Infrastructure;


namespace Samples.BluetoothLE
{
    public class GattCharacteristicViewModel : ViewModel
    {
        readonly IDialogs dialogs;
        IDisposable watcher;


        public GattCharacteristicViewModel(IGattCharacteristic characteristic, IDialogs dialogs)
        {
            this.Characteristic = characteristic;
            this.dialogs = dialogs;
        }


        public IGattCharacteristic Characteristic { get; }


        [Reactive] public string Value { get; private set; }
        [Reactive] public bool IsNotifying { get; private set; }
        [Reactive] public bool IsValueAvailable { get; private set; }
        [Reactive] public DateTime LastValue { get; private set; }
        public string Uuid => this.Characteristic.Uuid;
        public string ServiceUuid => this.Characteristic.Service.Uuid;
        public string Properties => this.Characteristic.Properties.ToString();


        public async void Select()
        {
            var cfg = new Dictionary<string, Action>();
            if (this.Characteristic.CanWriteWithResponse())
                cfg.Add("Write With Response", () => this.DoWrite(true));

            if (this.Characteristic.CanWriteWithoutResponse())
                cfg.Add("Write Without Response", () => this.DoWrite(false));

            if (this.Characteristic.CanWrite())
                cfg.Add("Send Test BLOB", this.SendBlob);

            if (this.Characteristic.CanRead())
                cfg.Add("Read", this.DoRead);

            if (this.Characteristic.CanNotify())
            {
                var txt = this.Characteristic.IsNotifying ? "Stop Notifying" : "Notify";
                cfg.Add(txt, this.ToggleNotify);
            }
            if (cfg.Any())
                await this.dialogs.ActionSheet($"{this.Uuid}", cfg);
        }


        async void SendBlob()
        {
            var cts = new CancellationTokenSource();
            var bytes = Encoding.UTF8.GetBytes(RandomString(5000));
            //var dlg = this.dialogs.Loading("Sending Blob", () => cts.Cancel(), "Cancel");
            var sw = new Stopwatch();
            sw.Start();

            var sub = this.Characteristic
                .BlobWrite(new MemoryStream(bytes))
                .Subscribe(
                    //s => dlg.Title = $"Sending Blob - Sent {s.Position} of {s.TotalLength} bytes",
                    ex =>
                    {
                        //dlg.Dispose();
                        this.dialogs.Snackbar("Failed writing blob - " + ex);
                        sw.Stop();
                    },
                    () =>
                    {
                        //dlg.Dispose();
                        sw.Stop();
                        this.dialogs.Snackbar($"BLOB write took " + sw.Elapsed);
                    }
                );

            cts.Token.Register(() => sub.Dispose());
        }


        async void DoWrite(bool withResponse)
        {
            try
            {
                var utf8 = await this.dialogs.Confirm("Confirm", "Write value from UTF8 or HEX?", "UTF8", "HEX");
                var result = await this.dialogs.Input(this.Uuid.ToString(), "Please enter a write value");

                if (!result.IsEmpty())
                {
                    var bytes = utf8 == true ? Encoding.UTF8.GetBytes(result) : result.FromHex();
                    var msg = withResponse ? "Write Complete" : "Write Without Response Complete";
                    this.Characteristic
                        .Write(bytes, withResponse)
                        .Timeout(TimeSpan.FromSeconds(2))
                        .Subscribe(
                            x => this.dialogs.Snackbar(msg),
                            ex => this.dialogs.Alert(ex.ToString())
                        );
                }
            }
            catch (Exception ex)
            {
                await this.dialogs.Alert(ex.ToString(), "ERROR");
            }
        }


        async void ToggleNotify()
        {
            if (this.Characteristic.IsNotifying)
            {
                this.watcher?.Dispose();
                this.IsNotifying = false;
            }
            else
            {
                this.IsNotifying = true;
                var utf8 = await this.dialogs.Confirm(
                    "Display Value as UTF8 or HEX?",
                    "Confirm",
                    "UTF8",
                    "HEX"
                );
                this.watcher = this.Characteristic
                    .Notify()
                    .Where(x => x.Type == CharacteristicResultType.Notification)
                    .SubOnMainThread(
                        x => this.SetReadValue(x, utf8),
                        ex => this.dialogs.Alert(ex.ToString())
                    );
            }
        }


        async void DoRead()
        {
            var utf8 = await this.dialogs.Confirm(
                "Confirm",
                "Display Value as UTF8 or HEX?",
                "UTF8",
                "HEX"
            );
            this.Characteristic
                .Read()
                .Timeout(TimeSpan.FromSeconds(2))
                .Subscribe(
                    x => this.SetReadValue(x, utf8),
                    ex => this.dialogs.Alert(ex.ToString())
                );
        }


        void SetReadValue(CharacteristicGattResult result, bool fromUtf8) => Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
        {
            this.IsValueAvailable = true;
            this.LastValue = DateTime.Now;

            if (result.Data == null)
                this.Value = "EMPTY";

            else
                this.Value = fromUtf8
                    ? Encoding.UTF8.GetString(result.Data, 0, result.Data.Length)
                    : BitConverter.ToString(result.Data);
        });


        static readonly Random random = new Random();
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
