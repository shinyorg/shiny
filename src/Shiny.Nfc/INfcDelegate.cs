using System;
using System.Threading.Tasks;


namespace Shiny.Nfc
{
    public interface INfcDelegate
    {
        Task OnReceived(INDefRecord[] records);
    }
}