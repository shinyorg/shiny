using System;


namespace Shiny.Integrations.Prism
{
    public interface IShinyCommandExceptionHandler
    {
        void OnException(Exception exception);
    }
}
