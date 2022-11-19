namespace BetterReadLine;

public class Completion
{
    public string CompletionText { get; }

    public string DisplayText { get; }

    public Completion(string completionText, string? displayText = null)
    {
        CompletionText = completionText;
        DisplayText = displayText ?? completionText;

        const int maxLength = 20;
        if (DisplayText.Length > maxLength)
        {
            DisplayText = DisplayText[..(maxLength - 3)];
            DisplayText += DisplayText.EndsWith(".")
                ? ".."
                : "...";
        }
    }
}