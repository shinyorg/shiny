using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks
{
    public class BleHostingHubTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            var hubMarker = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.Hosting.Hubs.IShinyBleHub");
            if (hubMarker == null)
                return;

            var types = this.Context
                .GetAllInterfaceTypes()
                .Where(x => x.AllInterfaces.Any(y => y.Equals(hubMarker)))
                .ToList();

            foreach (var type in types)
                this.GenerateHub(type);
        }


        void GenerateHub(INamedTypeSymbol type)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Shiny.BluetoothLE.Hosting");

            using (builder.BlockInvariant("namespace " + type.ContainingNamespace))
            {
                using (builder.BlockInvariant($"public class HubRegistration : global::Shiny.IShinyStartup"))
                {
                    builder.AppendLineInvariant("readonly IBleHostingManager manager");
                    using (builder.BlockInvariant($"public HubRegistration(IBleHostingManager manager)"))
                    {
                        builder.AppendLine("this.manager = manager");
                    }

                    using (builder.BlockInvariant("public void Start()"))
                    {
                        using (builder.BlockInvariant(""))
                        {
                            var methods = type.GetMethods();
                            foreach (var method in methods)
                            {

                            }
                        }
                    }
                }
            }
        }
    }
}
