using System;
using System.Linq;
using Uno.RoslynHelpers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tasks
{
    public class BleClientTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            //System.Diagnostics.Debugger.Launch();
            var bleService = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.CharacteristicAttribute");
            if (bleService == null)
                return;

            var types = this
                .Context
                .GetAllInterfaceTypes()
                .Where(x => x
                    .GetMethods()
                    .Any(y => y.FindAttributeFlattened(bleService) != null)
                );

            this.Log.Info("RUNNING BLE CLIENT SOURCE GENERATOR");
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
                this.Context.AddCompilationUnit(className, builder.ToString());
            }
        }
    }
}