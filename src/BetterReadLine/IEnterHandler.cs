namespace BetterReadLine;

public interface IEnterHandler
{
    public EnterHandlerResponse Handle(string promptText, int caret);
}