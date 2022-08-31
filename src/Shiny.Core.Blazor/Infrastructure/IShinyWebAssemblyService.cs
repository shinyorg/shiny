using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Shiny.Infrastructure;


public interface IShinyWebAssemblyService
{
    Task OnStart(IJSInProcessRuntime jsRuntime);
}

