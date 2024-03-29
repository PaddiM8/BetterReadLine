using System.Collections.Generic;

namespace BetterReadLine;

public interface IAutoCompleteHandler
{
    char[] Separators { get; set; }

    public int GetCompletionStart(string text, int cursorPos)
    {
        int start = text.LastIndexOfAny(Separators);

        return start == -1
            ? 0
            : start + 1;
    }

    IList<Completion> GetSuggestions(string text, int completionStart, int completionEnd);
}