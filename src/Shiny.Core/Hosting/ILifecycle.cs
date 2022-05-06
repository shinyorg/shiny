namespace Shiny.Hosting; 

public interface ILifecycle
{
    void Process<T>(T args);
}
