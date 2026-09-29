namespace Shiny.AppFunctions;

public enum AppFunctionErrorCode
{
    /// <summary>A parameter was missing or invalid.</summary>
    InvalidArgument,
    /// <summary>The function, or an entity it refers to, does not exist.</summary>
    NotFound,
    /// <summary>A delegate refused the call, or the user is not allowed to do this.</summary>
    Denied,
    /// <summary>The caller cancelled or the OS time budget ran out.</summary>
    Cancelled,
    /// <summary>Anything else that went wrong in the app.</summary>
    AppError
}

/// <summary>
/// Throw from a handler to fail the call with a specific code. The message is shown or spoken to the user,
/// so write it for them. Any other exception becomes <see cref="AppFunctionErrorCode.AppError"/>.
/// </summary>
public class AppFunctionException(AppFunctionErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AppFunctionErrorCode Code { get; } = code;
}
