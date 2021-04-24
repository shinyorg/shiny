
# MODULES

Modules are handy little things that allows your library to inject other services it may need into the container so that they can be used.  This way, you are able to keep loose coupling on your libraries

```cs
using Shiny;
using Microsoft.Extensions.DependencyInjection;

public class YourModule : ShinyModule
{
    public override void Register(IServiceCollection services)
    {

    }
}
```