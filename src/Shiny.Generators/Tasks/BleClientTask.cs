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
            var bleClientInterface = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.IBleClient");
            if (bleClientInterface == null)
                return;

            var bleAttribute = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.CharacteristicAttribute");
            var types = this.Context
                .GetAllInterfaceTypes()
                .Where(x => x.AllInterfaces.Any(y => y.Equals(bleClientInterface)))
                .ToList();


            this.Log.Info("RUNNING BLE CLIENT SOURCE GENERATOR");
            foreach (var type in types)
            {
                var className = type.Name.TrimStart('I');
                var builder = new IndentedStringBuilder();
                builder.AppendNamespaces("using System.Reactive.Async;");

                using (builder.BlockInvariant($"namespace {type.ContainingNamespace}"))
                {
                    using (builder.BlockInvariant($"public class {className} : global::Shiny.BluetoothLE.RefitClient.Infrastructure.BleClient, {type.Name}"))
                    {
                        var methods = type.GetMethods();

                        foreach (var method in methods)
                        {
                            if (method.ReturnType == null)
                                throw new ArgumentException("BLE Clients must always have a return type of Task, Task<T>, or IObservable<T>");

                            var attributeData = method.FindAttributeFlattened(bleAttribute);
                            if (attributeData == null)
                                throw new ArgumentException("BLE Client methods must be marked with the [Shiny.BluetoothLE.RefitClient.CharacteristicAttribute]");

                            if (method.Parameters.Length > 1)
                                throw new ArgumentException("BLE Client methods can have a maximum of 1 argument");

                            var genMethodSignature = $"public {method.ReturnType.ToDisplayString()} {method.Name}(";
                            if (method.Parameters.Length > 1)
                            {
                                var p = method.Parameters[0];
                                genMethodSignature += $"{p.Type.ToDisplayString()} {p.Name}";
                            }
                            genMethodSignature += ")";

                            using (builder.BlockInvariant(genMethodSignature))
                            {
                                var serviceUuid = (string)attributeData.ConstructorArguments[0].Value;
                                var characteristicUuid = (string)attributeData.ConstructorArguments[1].Value;

                                // TODO: connect, find service, find characteristic, do operation
                                // TODO: CancellationToken, Timeouts?
                                // TODO: method signatures with Task<> need an async?
                                if (method.ReturnType.Equals(typeof(Task<>).FullName))
                                {
                                    if (method.Parameters.Length > 0)
                                    {
                                        builder.AppendLineInvariant($"var data = this.Serializer.Serialize({method.Parameters[0].Name});");
                                        builder.AppendLineInvariant("await this.Peripheral.Write(data);");
                                        // write/read - tx/rx
                                    }
                                    builder.AppendLineInvariant("var result = await this.Peripheral.Read();");

                                    builder.AppendLineInvariant($"var obj = this.Serializer.Deserialize(null, result);");
                                    builder.AppendLineInvariant("return obj");
                                    // read
                                }
                                else if (method.ReturnType == typeof(Task))
                                {
                                    if (method.Parameters.Length == 0)
                                        throw new ArgumentException("Write methods must have a single argument");

                                    // write
                                    builder.AppendLineInvariant($"var data = this.Serializer.Serialize({method.Parameters[0].Name});");
                                    builder.AppendLineInvariant("await this.Peripheral.Write(data);");
                                }
                                //else if (method.ReturnType  == typeof(IObservable<>))
                                //{
                                //    // notify - what about indicate?
                                //    if (method.Parameters.Length > 0)
                                //    {
                                //        // write/read - tx/rx
                                //    }

                                //}
                                else
                                    throw new ArgumentException("BLE Clients must always have a return type of Task, Task<T>, or IObservable<T>");
                            }
                        }
                    }
                }
                this.Context.AddCompilationUnit(className, builder.ToString());
            }
        }
    }
}