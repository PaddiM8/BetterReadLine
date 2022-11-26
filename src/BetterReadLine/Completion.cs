using BetterReadLine.Render;

namespace BetterReadLine;

public class Completion
{
    public string CompletionText { get; }

    public string DisplayText { get; }

    public Completion(string completionText, string? displayText = null)
    {
        const int maxLength = 20;
        CompletionText = completionText;
        DisplayText = (displayText ?? completionText).WcTruncate(maxLength);
    }
}