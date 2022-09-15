namespace Shiny;


/// <summary>
/// Registering this in your Shiny.Startup (using RegisterStartupTask) will run these tasks immediately after the
/// service container has been built
/// </summary>
public interface IShinyStartupTask
{
    /// <summary>
    /// This method is immediately executed after the container is built (so the class implementing this interface supports constructor DI)
    /// </summary>
    void Start();
}
