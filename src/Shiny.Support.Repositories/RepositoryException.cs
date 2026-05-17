namespace Shiny.Support.Repositories;

/// <summary>
/// Exception thrown when a repository operation cannot be completed
/// (e.g. inserting a duplicate identifier or updating a missing record).
/// </summary>
public class RepositoryException(string message) : System.Exception(message);

