namespace Sample.SpeechRecognition;

public record ListItemViewModel(
    bool IsBot,
    string? From,
    string? Text,
    ICommand? Command = null
);