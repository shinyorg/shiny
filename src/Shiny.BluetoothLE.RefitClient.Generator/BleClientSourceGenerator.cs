using System;
using System.Linq;
using Uno.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using Uno.RoslynHelpers;


namespace Shiny.BluetoothLE.RefitClient.Generator
{
    public class BleClientSourceGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            var log = context.GetLogger();
            log.Info("RUNNING BLE CLIENT SOURCE GENERATOR");

            var bleService = context.Compilation.GetTypeByMetadataName($"Shiny.BluetoothLE.RefitClient.CharacteristicAttribute");
            if (bleService == null)
                return;

            var types = context
                .Compilation
                .SyntaxTrees
                .Select(x => context.Compilation.GetSemanticModel(x))
                .SelectMany(x => x
                    .SyntaxTree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Select(y => y.GetDeclaredSymbol(x))
                )
                .OfType<INamedTypeSymbol>();

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

                            //var attributeData = method.FindAttributeFlattened(bleService);
                            //if (attributeData != null)
                            //{
                            //    var serviceUuid = (string)attributeData.ConstructorArguments[0].Value;
                            //    var characteristicUuid = (string)attributeData.ConstructorArguments[1].Value;

                            //    if (method.ReturnType is Task)
                            //    {
                            //        // write
                            //    }
                            //    else if (method.ReturnType is Task<>)
                            //    {
                            //        // read
                            //    }
                            //    else if (method.ReturnType is IObservable<>)
                            //    {
                            //        // notify - what about indicate?
                            //    }
                            //}
                        }
                    }
                }
                context.AddCompilationUnit(className, builder.ToString());
            }
        }
    }
}