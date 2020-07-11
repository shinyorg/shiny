using System;
using System.Linq;
using Uno.SourceGeneration;
using Uno.RoslynHelpers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Generators
{
    public static class BleClientSourceGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            var log = context.GetLogger();
            log.Info("RUNNING BLE CLIENT SOURCE GENERATOR");

            var bleService = context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.CharacteristicAttribute");
            if (bleService == null)
                return;

            var types = context
                .GetAllInterfaceTypes()
                .Where(x => x
                    .GetMethods()
                    .Any(y => y.FindAttributeFlattened(bleService) != null)
                );

            foreach (var type in types)
            {
                var className = type.Name.TrimStart('I');
                var builder = new IndentedStringBuilder();
                builder.AppendLineInvariant("using System;");

                using (builder.BlockInvariant($"namespace {type.ContainingNamespace}"))
                {
                    using (builder.BlockInvariant($"public class {className} : {type.Name}"))
                    {
                        var methods = type.GetMethods();
                        foreach (var method in methods)
                        {
                            var returnTypeName = method.ReturnType?.GetFullName() ?? "void";
                            using (builder.BlockInvariant($"public {returnTypeName} {method.Name}()"))
                            {
                                // TODO: beyond void & with args
                            }
                            // TODO: if the method is not marked, it is an error since the class can't compile

                            var attributeData = method.FindAttributeFlattened(bleService);
                            if (attributeData != null)
                            {
                                var serviceUuid = (string)attributeData.ConstructorArguments[0].Value;
                                var characteristicUuid = (string)attributeData.ConstructorArguments[1].Value;

                                // TODO: pass in IPeripheral once found
                                if (method.ReturnType == typeof(Task<>))
                                {
                                    // write
                                }
                                else if (method.ReturnType == typeof(Task))
                                {
                                    // read
                                }
                                else if (method.ReturnType  == typeof(IObservable<>))
                                {
                                    // notify - what about indicate?
                                    
                                }
                            }
                        }
                    }
                }
                context.AddCompilationUnit(className, builder.ToString());
            }
        }
    }
}