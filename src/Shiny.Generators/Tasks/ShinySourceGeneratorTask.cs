using System;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tasks
{
    public abstract class ShinySourceGeneratorTask
    {
        protected IShinyContext ShinyContext { get; private set; }
        protected ILogger Log => this.ShinyContext.Log;
        protected GeneratorExecutionContext Context => this.ShinyContext.Context;


        public void Init(IShinyContext context) => this.ShinyContext = context;

        public abstract void Execute();
    }
}
