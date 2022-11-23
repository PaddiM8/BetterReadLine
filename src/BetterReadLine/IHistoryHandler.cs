namespace BetterReadLine;

public interface IHistoryHandler
{
    // TODO: Need to separate two sessions somehow. If one session goes up in history while the other session gets
    //       new entries, the new entries should not be in the other session's history. You should only get a combination
    //       when starting a new session. So maybe store a list locally as well.
    string? GetNext(string promptText, int caret, bool wasEdited);
    
    string? GetPrevious(string promptText, int caret);
}