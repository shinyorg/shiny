using System;
using System.Threading.Tasks;


namespace Shiny.Printers.Zebra
{
    public class ZebraPrinterManager : IPrinterManager
    {
        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public IObservable<IPrinter> Scan()
        {
            throw new NotImplementedException();
        }
    }
}
