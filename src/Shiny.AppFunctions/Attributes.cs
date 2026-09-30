namespace Shiny.AppFunctions;

/// <summary>
/// Marks a record as an app function. The record's public properties are the function's parameters, and the record
/// must implement <see cref="IAppFunction"/> or <see cref="IAppFunction{TResult}"/>. A matching
/// <see cref="IAppFunctionHandler{TRequest}"/> / <see cref="IAppFunctionHandler{TRequest, TResult}"/> runs it.
/// </summary>
/// <param name="id">
/// Stable id shared by both platforms: lowercase letters, digits and underscores, starting with a letter
/// (for example <c>create_order</c>). Changing it breaks saved Shortcuts and agent references.
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class AppFunctionAttribute(string id) : Attribute
{
    public string Id { get; } = id;

    /// <summary>Short user-facing name (Shortcuts action title). Defaults to the record name split into words.</summary>
    public string? Title { get; set; }

    /// <summary>What the function does. Assistants use this to decide when to call it, so be specific.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The function needs the app on screen, as if every call passed an <see cref="AppFunctionGate.OpenApp"/> gate.
    /// iOS: the app is brought to the foreground before the handler runs. Android cannot bring an app forward from
    /// the background, so the call runs only while the app is visible and is otherwise refused (<c>Denied</c>).
    /// </summary>
    public bool OpensApp { get; set; }
}

/// <summary>Describes a function parameter (a property of an <see cref="AppFunctionAttribute"/> record).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class AppParameterAttribute : Attribute
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Marks a record as an entity that functions can take as a parameter (a customer, a playlist...). It needs a
/// public <c>string Id</c> property and an <see cref="IAppEntityQuery{TEntity}"/> implementation. iOS shows it as an
/// App Entity with search; Android receives the id and gets a generated <c>search_{id}</c> function to find one.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class AppEntityAttribute(string id) : Attribute
{
    public string Id { get; } = id;

    /// <summary>User-facing type name, for example "Customer". Defaults to the record name split into words.</summary>
    public string? Title { get; set; }

    /// <summary>The property shown to the user for an instance. Defaults to <c>Name</c>, then <c>Title</c>, then <c>Id</c>.</summary>
    public string? DisplayProperty { get; set; }
}

/// <summary>
/// Apple only (ignored on Android): exposes the function as an App Shortcut, which makes it available to Siri
/// and Spotlight without any setup by the user. Every phrase must contain <c>${applicationName}</c>.
/// Repeat the attribute for more phrases; the first one's <see cref="ShortTitle"/> and <see cref="SystemImage"/> are used.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AppShortcutAttribute(string phrase) : Attribute
{
    public string Phrase { get; } = phrase;

    /// <summary>Label on the Shortcuts tile. Defaults to the function title.</summary>
    public string? ShortTitle { get; set; }

    /// <summary>SF Symbol name for the tile, for example <c>cart</c>.</summary>
    public string? SystemImage { get; set; }
}
