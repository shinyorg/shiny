using System.Windows.Input;

namespace Sample;

public record CommandItem(
    string Text,
    string? Detail = null,
    ICommand? Command = null
);