using System;
using System.Threading.Tasks;

namespace Shiny.Nfc
{
    public interface INfcManager
    {
        Task<AccessState> RequestAccess(bool forWrite = false);
        bool IsListening { get; }
        void StartListener();
        void StopListening();
    }
}
