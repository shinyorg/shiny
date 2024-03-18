using Microsoft.CodeAnalysis;

namespace Shiny.SourceGenerators;


[Generator]
public class AutoRegistrationGenerator : IIncrementalGenerator
{
    public GlobalOptions? GlobalOptions { get; set; }

    //https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
    //https://github.com/andrewlock/StronglyTypedId
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context
            .AnalyzerConfigOptionsProvider
            .Select((x, ct) =>
            {
                if (this.GlobalOptions != null)
                    return this.GlobalOptions;

                return GlobalOptions.Select(x, ct);
            });

        //var types = context.CompilationProvider.Select(static (compilation, ct) =>
        //{
        //});
        // JobAttribute
        // StoreGenerated Attribute
        // ShinyServiceAttribute
    }
}




//    var pages = context.CompilationProvider.Select(static (compilation, ct) =>
//    {
//        // this process could really slow things down - need to watch
//        var pageType = compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.Page");
//        var pageTypes = compilation
//            .GetAllAssemblies()
//            .Where(x => IsNonSystemAssembly(x))
//            .SelectMany(x => x
//                .GetAllTypeSymbols()
//                .Where(y => y.Is(pageType))
//            )
//            .ToList();

//        var process = new ProcessContext(compilation, pageTypes, ct);
//        return process;
//    });

//    var all = pages.Combine(globalOptions);


//    context.RegisterSourceOutput(all, (ctx, src) =>
//    {
//        var opts = src.Right;
//        var process = src.Left;

//        // we didn't need any of this, keeping it for now though
//        //var ns = opts.RootNamespace ?? process.Compilation.AssemblyName ?? throw new InvalidOperationException("No root namespace to work with");
//        //var proj = opts.ProjectName;

//        //if (proj == null)
//        //{
//        //    if (opts.ProjectFullPath == null)
//        //        throw new InvalidOperationException("Unable to get project name");

//        //    proj = Path.GetFileNameWithoutExtension(opts.ProjectFullPath);
//        //}

//        var sb = new StringBuilder();
//        // TODO: Moultrie want the solution name here, but I can't get at that as far as I know
//        sb.AppendLine($"namespace MoultrieMobile.Maui.Navigation;");
//        sb.AppendLine("public partial class NavigationKeys");
//        sb.AppendLine("{");

//        foreach (var page in process.PageTypes)
//        {
//            var assemblyName = page.ContainingAssembly.Name.Replace(".", "_");
//            var pageType = $"{assemblyName}_{page.Name}";

//            sb.AppendLine($"\tpublic const string {pageType} = \"{pageType}\";");
//        }
//        sb.AppendLine("}");

//        var text = sb.ToString();
//        var source = SourceText.From(text, Encoding.UTF8);
//        ctx.AddSource("Navigation.g.cs", source);
//    });
//}
