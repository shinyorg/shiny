using System;
using System.Threading.Tasks;

namespace Shiny.Push;


public interface IPushProvider
{
    /// <summary>
    /// 
    /// </summary>
    IPushTagSupport? Tags { get; }

#if ANDROID
    Task Register(string nativeToken);
    Task UnRegister();
#elif APPLE
    Task Register(Foundation.NSData nativeToken);
    Task UnRegister();
#endif
}

