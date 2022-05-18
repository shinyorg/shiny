using System.ComponentModel;
namespace Shiny.Jobs.Net;


public interface IConnectivity : INotifyPropertyChanged
{
    NetworkReach Reach { get; }
    NetworkAccess Access { get; }
    string? CellularCarrier { get; }
}