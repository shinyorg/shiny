using System.Threading.Tasks;

namespace Shiny.Push;


public interface IPushProvider
{

#if ANDROID
    Task<string> Register(string nativeToken);
    Task UnRegister();
#elif APPLE
    Task<string> Register(Foundation.NSData nativeToken);
    Task UnRegister();
#endif
}

