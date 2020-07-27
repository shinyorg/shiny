using System;
using Uno.SourceGeneration;


namespace Shiny.Generators.Tasks
{
    public abstract class ShinySourceGeneratorTask
    {
        protected IShinyContext ShinyContext { get; private set; }
        protected ISourceGeneratorLogger Log => this.ShinyContext.Log;
        protected SourceGeneratorContext Context => this.ShinyContext.Context;


        public void Init(IShinyContext context) => this.ShinyContext = context;

        public abstract void Execute();
    }
}
