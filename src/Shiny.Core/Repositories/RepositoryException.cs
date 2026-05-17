namespace Shiny.Support.Repositories;

/// <summary>
/// Thrown when a repository operation fails (e.g. inserting a duplicate identifier or updating a missing entity).
/// </summary>
/// <param name="message">A description of the failure.</param>
public class RepositoryException(string message) : System.Exception(message);

