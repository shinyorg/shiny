using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shiny.AppFunctions.SourceGenerators;

/// <summary>
/// Finds [AppFunction] / [AppEntity] records, handlers, entity queries and delegates in the project, and generates
/// AddAppFunctions(), the reflection-free registry, and (as constants the build task extracts) the Swift App Intents
/// and the Android AppFunctions schema.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AppFunctionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The model depends on symbols across the whole project (handlers, queries and delegates can live anywhere),
        // so it is rebuilt from the compilation. Output only changes when the declarations change.
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            var analyzer = new Analyzer(compilation);
            var model = analyzer.Analyze();
            foreach (var d in analyzer.Diagnostics)
                spc.ReportDiagnostic(d);

            if (model == null || analyzer.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            AddEntitySearches(model);
            var swift = SwiftEmitter.Emit(model);
            var androidV1 = AndroidEmitter.EmitV1(model);
            var androidV2 = AndroidEmitter.EmitV2(model);
            spc.AddSource("ShinyAppFunctions.g.cs", CSharpEmitter.Emit(model, swift, androidV1, androidV2));
        });
    }

    /// <summary>Android has no entity queries, so each entity gets a search_{id} function agents can use to find ids.</summary>
    internal static void AddEntitySearches(GenerationModel model)
    {
        foreach (var e in model.Entities)
        {
            model.Functions.Add(new FunctionModel
            {
                Id = "search_" + e.Id,
                Title = "Find " + e.Title,
                Description = $"Finds {e.Title} items by text. Returns their ids (for other functions' {e.Id} parameters) and titles.",
                RequestSimpleName = "Search" + e.SimpleName,
                SearchesEntityId = e.Id,
                Parameters = new List<ParameterModel>
                {
                    new()
                    {
                        CSharpName = "Query",
                        WireName = "query",
                        Title = "Query",
                        Description = "Text to search for",
                        IsRequired = true,
                        Type = new TypeModel { Kind = ValueKind.String, CSharpType = "string", CSharpBaseType = "string" }
                    }
                },
                Result = new TypeModel
                {
                    Kind = ValueKind.Array,
                    ItemType = new TypeModel
                    {
                        Kind = ValueKind.Object,
                        TypeName = "Shiny.AppFunctions.AppEntityItem",
                        SimpleName = "AppEntityItem",
                        Properties = new List<PropertyModel>
                        {
                            new() { CSharpName = "Id", WireName = "id", Type = new TypeModel { Kind = ValueKind.String } },
                            new() { CSharpName = "Title", WireName = "title", Type = new TypeModel { Kind = ValueKind.String } }
                        }
                    }
                }
            });
        }
    }
}
