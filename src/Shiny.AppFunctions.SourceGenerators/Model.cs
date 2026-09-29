using System.Collections.Generic;

namespace Shiny.AppFunctions.SourceGenerators;

// Mirrors Shiny.AppFunctions.AppValueKind
enum ValueKind
{
    Void,
    String,
    Int32,
    Int64,
    Double,
    Boolean,
    DateTimeOffset,
    Enum,
    Entity,
    Object,
    Array
}

sealed class TypeModel
{
    public ValueKind Kind;
    public bool IsNullable;
    /// <summary>Fully qualified C# type (global::...), including ? when nullable and a value type.</summary>
    public string CSharpType = "";
    /// <summary>Non-nullable fully qualified C# type.</summary>
    public string CSharpBaseType = "";
    /// <summary>Wire / Android name for objects, enums and entities (namespace-qualified, no global::).</summary>
    public string TypeName = "";
    /// <summary>Simple C# name, used to derive Swift names.</summary>
    public string SimpleName = "";
    public string? EntityId;
    public List<string> EnumValues = new();
    public List<PropertyModel> Properties = new();
    public TypeModel? ItemType;
    public bool IsValueType;
}

sealed class PropertyModel
{
    public string CSharpName = "";
    public string WireName = "";
    public TypeModel Type = null!;
}

sealed class ParameterModel
{
    public string CSharpName = "";
    public string WireName = "";
    public string Title = "";
    public string? Description;
    public TypeModel Type = null!;
    public bool IsRequired;
    /// <summary>Index into the chosen constructor, or -1 for an initializer.</summary>
    public int ConstructorIndex = -1;
}

sealed class FunctionModel
{
    public string Id = "";
    public string Title = "";
    public string Description = "";
    public bool OpensApp;
    public string RequestType = "";
    public string RequestSimpleName = "";
    public List<ParameterModel> Parameters = new();
    public TypeModel Result = null!;
    public string HandlerInterface = "";
    public string? HandlerType;
    public List<string> ShortcutPhrases = new();
    public string? ShortcutTitle;
    public string? ShortcutImage;
    /// <summary>Set on the generated search_{entity} functions.</summary>
    public string? SearchesEntityId;
}

sealed class EntityModel
{
    public string Id = "";
    public string Title = "";
    public string CSharpType = "";
    public string SimpleName = "";
    public string TypeName = "";
    public string DisplayProperty = "";
    public string? QueryType;
}

sealed class GenerationModel
{
    public List<FunctionModel> Functions = new();
    public List<EntityModel> Entities = new();
    public List<string> Delegates = new();
    public List<TypeModel> Enums = new();
}
