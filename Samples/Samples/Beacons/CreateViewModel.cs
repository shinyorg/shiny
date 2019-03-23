using System;
using System.Windows.Input;
using Shiny.Beacons;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Beacons
{
    public class CreateViewModel : ViewModel
    {
        public CreateViewModel(INavigationService navigationService)
        {
            this.Title = "Create Region";

            this.EstimoteDefaults = ReactiveCommand.Create(() =>
            {
                this.Identifier = "Estimote";
                this.Uuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
            });

            this.Create = ReactiveCommand.CreateFromTask(
                _ =>
                {
                    ushort? major = null;
                    ushort? minor = null;
                    if (this.Major > 0)
                        major = this.Major;

                    if (this.Minor > 0)
                        minor = this.Minor;

                    return navigationService.GoBackAsync(new NavigationParameters
                    {
                        {
                            nameof(BeaconRegion),
                            new BeaconRegion(
                                this.Identifier,
                                Guid.Parse(this.Uuid),
                                major,
                                minor
                            )
                        }
                    });
                },
                this.WhenAny(
                    x => x.Identifier,
                    x => x.Uuid,
                    x => x.Major,
                    x => x.Minor,
                    (id, uuid, M, m) =>
                    {
                        if (String.IsNullOrWhiteSpace(id.GetValue()))
                            return false;

                        if (!Guid.TryParse(uuid.GetValue(), out _))
                            return false;

                        var Mv = M.GetValue();
                        if (Mv < 0 || Mv > ushort.MaxValue)
                            return false;

                        var mv = m.GetValue();
                        if (mv < 0 || mv > ushort.MaxValue)
                            return false;

                        // if using minor, must have major
                        //if (Mv == 0 || mv > 0)
                        //    return false;

                        return true;
                    }
                )
            );
        }


        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            var forMonitoring = parameters.GetValue<bool>("Monitoring");
            this.Title = forMonitoring
                ? "Create Monitoring Region"
                : "Create Ranging Region";

            var current = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (current != null)
            {
                this.Identifier = current.Identifier;
                this.Uuid = current.Uuid.ToString();
                this.Major = current.Major ?? 0;
                this.Minor = current.Minor ?? 0;
            }
        }


        public ICommand Create { get; }
        public ICommand EstimoteDefaults { get; }
        [Reactive] public string Identifier { get; set; }
        [Reactive] public string Uuid { get; set; }
        [Reactive] public ushort Major { get; set; }
        [Reactive] public ushort Minor { get; set; }
    }
}
