using Microsoft.CodeAnalysis;

namespace Shiny.Auto.Generators;


//https://github.com/shinyorg/shiny/tree/dev/src/Shiny.Core/Stores
[Generator]
public class StoreGeneratedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

    }
}

//[SecureStore] public interface IMySecureStore { string MySecureToken { get; set; } }
//container.RegisterStore<IMySecureStore>();
