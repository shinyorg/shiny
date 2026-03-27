namespace Shiny;

/// <summary>
/// Represents a component that performs startup initialization when the Shiny host starts
/// </summary>
public interface IShinyComponentStartup
{
    /// <summary>
    /// Runs component-specific startup logic
    /// </summary>
    void ComponentStart();
}