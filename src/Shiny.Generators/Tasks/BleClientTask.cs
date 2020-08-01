using System;
using System.Linq;
using Uno.RoslynHelpers;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tasks
{
    public class BleClientTask : ShinySourceGeneratorTask
    {
        static INamedTypeSymbol bleAttribute;


        // TODO: https://stackoverflow.com/questions/28240167/correct-way-to-check-the-type-of-an-expression-in-roslyn-analyzer
        public override void Execute()
        {
            //System.Diagnostics.Debugger.Launch();
            bleAttribute = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.CharacteristicAttribute");
            if (bleAttribute == null)
                return;

            var bleClientInterface = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.IBleClient");
            var types = this.Context
                .GetAllInterfaceTypes()
                .Where(x => x.AllInterfaces.Any(y => y.Equals(bleClientInterface)))
                .ToList();


            this.Log.Info("RUNNING BLE CLIENT SOURCE GENERATOR");
            foreach (var type in types)
                this.GenerateClient(type);
        }


        void GenerateClient(INamedTypeSymbol type)
        {
            var className = type.Name.TrimStart('I');
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("System.Reactive.Threading");

            using (builder.BlockInvariant($"namespace {type.ContainingNamespace}"))
            {
                using (builder.BlockInvariant($"public class {className} : global::Shiny.BluetoothLE.RefitClient.Infrastructure.BleClient, {type.Name}"))
                {
                    var methods = type.GetMethods();

                    foreach (var method in methods)
                    {
                        this.ValidateMethod(method);
                        var isReadMethod = this.Context.IsGenericAsyncTask(method.ReturnType);

                        var genMethodSignature = $"public ";
                        if (isReadMethod)
                            genMethodSignature += "async ";

                        genMethodSignature += $"{method.ReturnType.ToDisplayString()} {method.Name}(";
                        if (method.Parameters.Length == 1)
                        {
                            var p = method.Parameters[0];
                            genMethodSignature += $"{p.Type.ToDisplayString()} {p.Name}";
                        }
                        genMethodSignature += ")";

                        using (builder.BlockInvariant(genMethodSignature))
                        {
                            var attributeData = method.FindAttributeFlattened(bleAttribute);
                            var serviceUuid = (string)attributeData.ConstructorArguments[0].Value;
                            var characteristicUuid = (string)attributeData.ConstructorArguments[1].Value;

                            // TODO: connect, find service, find characteristic, do operation
                            // TODO: CancellationToken, Timeouts?
                            if (isReadMethod)
                            {
                                if (method.Parameters.Length == 1)
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
                            else if (this.Context.IsAsyncTask(method.ReturnType))
                            {
                                if (method.Parameters.Length == 0)
                                    throw new ArgumentException("Write methods must have a single argument");

                                // write
                                builder.AppendLineInvariant($"var data = this.Serializer.Serialize({method.Parameters[0].Name});");
                                builder.AppendLineInvariant("return this.Peripheral.Write(data);");
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


        void ValidateMethod(IMethodSymbol method)
        {
            if (method.ReturnType == null)
                throw new ArgumentException("BLE Clients must always have a return type of Task, Task<T>, or IObservable<T>");

            var attributeData = method.FindAttributeFlattened(bleAttribute);
            if (attributeData == null)
                throw new ArgumentException("BLE Client methods must be marked with the [Shiny.BluetoothLE.RefitClient.CharacteristicAttribute]");

            //if (!Guid.TryParse((string)attributeData.ConstructorArguments[0].Value, out _))
            //    throw new ArgumentException("Service UUID is not a valid GUID");

            //if (!Guid.TryParse((string)attributeData.ConstructorArguments[1].Value, out _))
            //    throw new ArgumentException("Characteristic UUID is not a valid GUID");

            // TODO: allow tasks with secondary arg as cancellation token
            if (method.Parameters.Length > 1)
                throw new ArgumentException("BLE Client methods can have a maximum of 1 argument");
        }
    }
}