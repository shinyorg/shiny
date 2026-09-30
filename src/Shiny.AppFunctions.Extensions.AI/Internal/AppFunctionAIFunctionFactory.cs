using Microsoft.Extensions.AI;

namespace Shiny.AppFunctions.Extensions.AI.Internal;

static class AppFunctionAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(AppFunctionDispatcher dispatcher, AppFunctionAIToolBuilder builder)
    {
        var functions = dispatcher.Registry.Functions;
        var unknown = builder.FunctionIds.Concat(builder.Excluded).Where(id => functions.All(f => f.Id != id)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"AddAppFunctionAITools: unknown app function id(s) {String.Join(", ", unknown)}. " +
                $"Known ids: {String.Join(", ", functions.Select(x => x.Id))}");

        var selected = new HashSet<string>(builder.All ? functions.Select(x => x.Id) : builder.FunctionIds);

        // a function with an entity parameter is useless to the LLM without a way to find the entity's id
        var searches = functions
            .Where(f => selected.Contains(f.Id))
            .SelectMany(f => f.Parameters)
            .Where(p => p.Type.Kind == AppValueKind.Entity && p.Type.EntityId != null)
            .Select(p => functions.FirstOrDefault(f => f.SearchesEntityId == p.Type.EntityId))
            .Where(f => f != null)
            .Select(f => f!.Id)
            .ToList();
        selected.UnionWith(searches);
        selected.ExceptWith(builder.Excluded);

        if (selected.Count == 0)
            throw new InvalidOperationException(
                "AddAppFunctionAITools selected no app functions - every one was excluded or the app declares none. " +
                "An empty registration would expose no tools to the LLM.");

        // registry order: declared functions, then the search functions
        return functions
            .Where(f => selected.Contains(f.Id))
            .Select(f => (AITool)new AppFunctionAIFunction(dispatcher, f))
            .ToList();
    }
}
