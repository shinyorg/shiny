using Microsoft.CodeAnalysis;

namespace Shiny.SourceGenerators;


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
