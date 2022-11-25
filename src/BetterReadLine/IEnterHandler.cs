namespace BetterReadLine;

public interface IEnterHandler
{
    public bool Handle(string promptText, out string? newPromptText);
}