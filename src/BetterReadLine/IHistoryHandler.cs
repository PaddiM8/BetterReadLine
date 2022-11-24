namespace BetterReadLine;

public interface IHistoryHandler
{
    string? GetNext(string promptText, int caret, bool wasEdited);
    
    string? GetPrevious(string promptText, int caret);
}