using System;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public interface IBleAdapterDelegate
    {
        Task OnBleAdapterStateChanged(AccessState state);
    }
}
