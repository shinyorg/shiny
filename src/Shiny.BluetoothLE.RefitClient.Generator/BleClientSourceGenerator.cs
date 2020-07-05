using System;
using System.Linq;

using Uno.SourceGeneration;


namespace Shiny.BluetoothLE.RefitClient.Generator
{
    public class BleClientSourceGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            var bleService = context.Compilation.GetTypeByMetadataName($"Shiny.BluetoothLE.RefitClient.ServiceAttribute");

            var types = context.Compilation.SourceModule.GlobalNamespace.GetTypeMembers().Where(x => x.IsType);
            foreach (var type in types)
            {
                var members = type.GetMembers();
                foreach (var member in members)
                {
                    // must be marked with an attribute

                }
            }
        }


        //public bool HasMagicAttribute(INamedTypeSymbol typeSymbol, string attribute)
        //{
        //    var attributeSymbol = GetMagicianSymbol(attribute);
        //    return typeSymbol == attributeSymbol || typeSymbol.GetAttributes().Any(a => a.AttributeClass == attributeSymbol);
        //}


        //private INamedTypeSymbol GetMagicianSymbol(string attribute)
        //{
        //    if (!_magicianSymbols.ContainsKey(attribute))
        //        _magicianSymbols[attribute] = SourceContext.Compilation.GetTypeByMetadataName($"Prism.Magician.{attribute}Attribute");

        //    return _magicianSymbols[attribute];
        //}
    }
}
