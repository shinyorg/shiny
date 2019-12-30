using System;
using System.Threading.Tasks;
using Shiny.Nfc;


namespace Shiny.Testing.Nfc
{
    public class TestNfcManager : INfcManager
    {
        public bool IsListening => throw new NotImplementedException();

        public Task<AccessState> RequestAccess(bool forWrite = false)
        {
            throw new NotImplementedException();
        }

        public void StartListener()
        {
            throw new NotImplementedException();
        }

        public void StopListening()
        {
            throw new NotImplementedException();
        }
    }
}
