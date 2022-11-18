namespace BetterReadLine;

public class Completion
{
    public string CompletionText { get; }

    public string DisplayText { get; }

    public Completion(string completionText, string? displayText = null)
    {
        CompletionText = completionText;
        DisplayText = displayText ?? completionText;
    }
}