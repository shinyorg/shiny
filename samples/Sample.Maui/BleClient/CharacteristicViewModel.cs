using System;
using System.Text;
using System.Windows.Input;
using Shiny.BluetoothLE;
using Xamarin.Forms;


namespace Sample
{
    public class CharacteristicViewModel : SampleViewModel
    {
        IDisposable? dispose;


        public CharacteristicViewModel(IGattCharacteristic characteristic)
        {
            this.ServiceUUID = characteristic.Service.Uuid;
            this.UUID = characteristic.Uuid;

            this.CanNotify = characteristic.CanNotify();
            this.CanRead = characteristic.CanRead();
            this.CanWrite = characteristic.CanWrite();

            this.Read = this.LoadingCommand(async () =>
            {
                var result = await characteristic.ReadAsync();
                this.SetRead(result.Data);
            });


            this.ToggleNotify = new Command(async () =>
            {
                try
                {
                    if (this.dispose == null)
                    {
                        this.dispose = characteristic
                            .Notify()
                            .SubOnMainThread(x => this.SetRead(x.Data));
                    }
                    else
                    {
                        this.Stop();
                    }

                }
                catch (Exception ex)
                {
                    await this.Alert(ex.ToString());
                }
            });
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.Stop();
        }


        void Stop()
        {
            this.dispose?.Dispose();
            this.dispose = null;
            this.IsNotifying = false;
        }


        void SetRead(byte[] data)
        {
            this.ReadValue = Encoding.UTF8.GetString(data);
            this.LastValueTime = DateTime.Now.ToString();
        }


        public bool CanRead { get; }
        public bool CanWrite { get; }
        public bool CanNotify { get; }
        public string ServiceUUID { get; }
        public string UUID { get; }

        public ICommand Read { get; }
        public ICommand Write { get; }
        public ICommand ToggleNotify { get; }


        bool notifying;
        public bool IsNotifying
        {
            get => this.notifying;
            private set => this.Set(ref this.notifying, value);
        }


        string readValue;
        public string ReadValue
        {
            get => this.readValue;
            private set => this.Set(ref this.readValue, value);
        }


        string writeValue;
        public string WriteValue
        {
            get => this.writeValue;
            set => this.Set(ref this.writeValue, value);
        }


        string lastValueTime;
        public string LastValueTime
        {
            get => this.lastValueTime;
            private set => this.Set(ref this.lastValueTime, value);
        }
    }
}
