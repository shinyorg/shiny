using Microsoft.CodeAnalysis;

namespace Shiny.AppFunctions.SourceGenerators;

static class Rules
{
    const string Category = "Shiny.AppFunctions";

    public static readonly DiagnosticDescriptor InvalidId = new(
        "SHAF001", "Invalid app function id",
        "'{0}' is not a valid id: use lowercase letters, digits and underscores, starting with a letter",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingInterface = new(
        "SHAF002", "App function must implement IAppFunction",
        "'{0}' has [AppFunction] but implements neither IAppFunction nor IAppFunction<TResult> (or implements more than one)",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        "SHAF003", "Unsupported type",
        "'{0}' of type '{1}' is not supported: {2}",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NoConstructor = new(
        "SHAF004", "Cannot construct request",
        "'{0}' needs a public constructor whose parameters match its properties, or a parameterless constructor with settable properties",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NoHandler = new(
        "SHAF005", "No handler",
        "No class implements {0} for app function '{1}'",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        "SHAF006", "More than one handler",
        "App function '{0}' has more than one handler: {1}",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EntityNeedsId = new(
        "SHAF007", "Entity needs an Id",
        "[AppEntity] '{0}' needs a public string Id property",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EntityDisplay = new(
        "SHAF008", "Entity display property not found",
        "[AppEntity] '{0}' has no public property '{1}' to display",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EntityNoQuery = new(
        "SHAF009", "Entity has no query",
        "No class implements IAppEntityQuery<{0}>",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateId = new(
        "SHAF010", "Duplicate id",
        "The id '{0}' is used more than once ({1})",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ShortcutPhrase = new(
        "SHAF011", "App Shortcut phrase must mention the app",
        "The phrase \"{0}\" must contain ${{applicationName}}",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor TooManyShortcuts = new(
        "SHAF012", "Too many App Shortcuts",
        "iOS allows at most 10 App Shortcuts; {0} functions have [AppShortcut]",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NoPublicConstructor = new(
        "SHAF013", "Class cannot be created by DI",
        "'{0}' has no public constructor, so it cannot be registered",
        Category, DiagnosticSeverity.Error, true);
}
