using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class UwpSourceGenerator : ShinyApplicationSourceGenerator
    {
        public UwpSourceGenerator() : base("Windows.UI.Xaml.Application")
        {
        }


        protected override void Process(IEnumerable<INamedTypeSymbol> osAppTypeSymbols)
        {
            var nameSpace = this.Context.Compilation.AssemblyName;

            //// TODO: generate OnActivated
            //foreach (var app in osAppTypeSymbols)
            //{
            //    var builder = new IndentedStringBuilder();

            //    using (builder.BlockInvariant("protected override void OnActivated(global::Windows.ApplicationModel.Activation.IActivatedEventArgs args)"))
            //    {
            //        // fired for foreground notifications
            //        if (args is ToastNotificationActivatedEventArgs not)
            //        {
            //            var args1 = not.Argument;
            //            // TODO: Handle activation according to argument
            //        }

            //        Window.Current.Activate();
            //    }
            //    this.Context.AddSource("", builder.ToString());
            //}
        }
    }
}
