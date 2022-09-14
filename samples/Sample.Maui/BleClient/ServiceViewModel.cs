using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny;
using Shiny.BluetoothLE;


namespace Sample
{
    public class ServiceViewModel : SampleViewModel
    {
        public ServiceViewModel(IGattService service)
        {
            this.Title = service.Uuid;

            this.Load = this.LoadingCommand(async () =>
            {
                this.Characteristics = (await service.GetCharacteristicsAsync())
                    .Select(x => new CharacteristicViewModel(x))
                    .ToList();

                this.RaisePropertyChanged(nameof(this.Characteristics));
            });

            this.WhenAnyProperty(x => x.SelectedCharacteristic)
                .Where(x => x != null)
                .SubOnMainThread(async x =>
                {
                    this.SelectedCharacteristic = null;
                    await this.Navigation.PushAsync(new CharacteristicPage
                    {
                        BindingContext = x
                    });
                });
        }


        public string Title { get; }
        public ICommand Load { get; }
        public List<CharacteristicViewModel> Characteristics { get; private set; }

        CharacteristicViewModel selected;
        public CharacteristicViewModel SelectedCharacteristic
        {
            get => this.selected;
            set
            {
                this.selected = value;
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
