using System;
using System.ComponentModel;


namespace Shiny.Net
{
    public interface IConnectivity : INotifyPropertyChanged
    {
        NetworkReach Reach { get; }
        NetworkAccess Access { get; }
    }
}