using System;
using System.Linq;
using Uno.RoslynHelpers;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tasks
{
    public class BleClientTask : ShinySourceGeneratorTask
    {
        INamedTypeSymbol? bleMethodAttribute;
        INamedTypeSymbol? bleNotificationAttribute;
        INamedTypeSymbol? bleClientInterface;

        // TODO: https://stackoverflow.com/questions/28240167/correct-way-to-check-the-type-of-an-expression-in-roslyn-analyzer
        public override void Execute()
        {
            //System.Diagnostics.Debugger.Launch();
            this.bleMethodAttribute = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.BleMethodAttribute");
            this.bleNotificationAttribute = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.BleNotificationAttribute");
            this.bleClientInterface = this.Context.Compilation.GetTypeByMetadataName("Shiny.BluetoothLE.RefitClient.IBleClient");

            if (this.bleMethodAttribute == null)
                return;

            
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
            builder.AppendNamespaces("System.Reactive.Threading", "System.Reactive.Threading.Tasks");

            using (builder.BlockInvariant($"namespace {type.ContainingNamespace}"))
            {
                using (builder.BlockInvariant($"public class {className} : global::Shiny.BluetoothLE.RefitClient.Infrastructure.BleClient, {type.Name}"))
                {
                    var methods = type.GetMethods();

                    foreach (var method in methods)
                    {
                        this.ValidateMethod(method);
                        var isReadMethod = this.Context.IsGenericAsyncTask(method.ReturnType);

                        //var genMethodSignature = $"public ";
                        //if (isReadMethod)
                        //    genMethodSignature += "async ";
                        var genMethodSignature = $"public async ";


                        genMethodSignature += $"{method.ReturnType.ToDisplayString()} {method.Name}(";
                        if (method.Parameters.Length == 1)
                        {
                            var p = method.Parameters[0];
                            genMethodSignature += $"{p.Type.ToDisplayString()} {p.Name}";
                        }
                        genMethodSignature += ")";

                        using (builder.BlockInvariant(genMethodSignature))
                        {
                            var attributeData = method.FindAttributeFlattened(this.bleMethodAttribute);
                            var serviceUuid = $"\"{(string)attributeData.ConstructorArguments[0].Value}\"";
                            var characteristicUuid = $"\"{(string)attributeData.ConstructorArguments[1].Value}\"";

                            // TODO: connect, find service, find characteristic, do operation
                            // TODO: CancellationToken, Timeouts?
                            if (isReadMethod)
                            {
                                builder.AppendLineInvariant($"var ch = await this.Char({serviceUuid}, {characteristicUuid}).ToTask();");

                                if (method.Parameters.Length == 1)
                                {
                                    builder.AppendLineInvariant($"var __write = this.Serializer.Serialize({method.Parameters[0].Name});");
                                    builder.AppendLineInvariant("await ch.Write(__write).ToTask();");
                                    // write/read - tx/rx
                                }
                                builder.AppendLineInvariant("var __read = await ch.Read().ToTask();");

                                builder.AppendLineInvariant($"var obj = this.Serializer.Deserialize(typeof({method.ReturnType.ToDisplayString()}), __read.Data);");
                                builder.AppendLineInvariant("return obj");
                                // read
                            }
                            else if (this.Context.IsAsyncTask(method.ReturnType))
                            {
                                if (method.Parameters.Length == 0)
                                    throw new ArgumentException("Write methods must have a single argument");

                                // write
                                builder.AppendLineInvariant($"var ch = await this.Char({serviceUuid}, {characteristicUuid})).ToTask();");
                                builder.AppendLineInvariant($"var __chdata = this.Serializer.Serialize({method.Parameters[0].Name});");
                                builder.AppendLineInvariant("return ch.Write(__chdata).ToTask();");
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


        BleClientMethodType DetectMethodType(IMethodSymbol method)
        {
            var methodType = BleClientMethodType.None;

            var param = method.Parameters.FirstOrDefault(); // has a write op
            var isAsyncTask = this.Context.IsGenericAsyncTask(method.ReturnType);
            var isObservable = this.Context.IsObservable(method.ReturnType);

            if (param == null)
            {
                var notifyAttr = method.FindAttributeFlattened(this.bleNotificationAttribute);
                if (notifyAttr == null)
                {
                    // read
                }
                else
                {
                    // notify
                    var useIndicationIfAvailable = notifyAttr.ConstructorArguments[2].Value.ToString().ToLower();
                }
            }
            else
            {
                // write
                var isStream = this.Context.IsStream(param.Type);

                // TODO: if stream, must be observable with CharacteristicGattResult as generic out
                if (!isStream)
                {
                    return BleClientMethodType.WriteAsync;
                }

                return BleClientMethodType.WriteStream;
            }


            return methodType;
        }


        void ValidateMethod(IMethodSymbol method)
        {
            if (method.ReturnType == null || (!this.Context.IsObservable(method.ReturnType) && !this.Context.IsAsyncTask(method.ReturnType)))
                throw new ArgumentException("BLE Clients must always have a return type of Task, Task<T>, or IObservable<T>");

            var attributeData = method.FindAttributeFlattened(this.bleMethodAttribute);
            if (attributeData == null)
                throw new ArgumentException("BLE Client methods must be marked with the [Shiny.BluetoothLE.RefitClient.BleMethodAttribute] or [Shiny.BluetoothLE.RefitClient.BleNotificationAttribute]");

            if (!Guid.TryParse((string)attributeData.ConstructorArguments[0].Value, out _))
                throw new ArgumentException("Service UUID is not a valid GUID");

            if (!Guid.TryParse((string)attributeData.ConstructorArguments[1].Value, out _))
                throw new ArgumentException("Characteristic UUID is not a valid GUID");

            // TODO: allow tasks with secondary arg as cancellation token
            if (method.Parameters.Length > 1)
                throw new ArgumentException("BLE Client methods can have a maximum of 1 argument");
        }
    }
}