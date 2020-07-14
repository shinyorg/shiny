using System;
using System.Threading.Tasks;


namespace Shiny.Printers
{
    public interface IPrinterManager
    {
        IObservable<IPrinter> Scan();
        Task<AccessState> RequestAccess();
    }
}
