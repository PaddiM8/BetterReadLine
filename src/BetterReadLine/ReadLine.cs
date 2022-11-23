using System;
using System.Collections.Generic;
using BetterReadLine.Render;

namespace BetterReadLine;

public class ReadLine
{

    public IHistoryHandler? HistoryHandler { private get; set; }
    
    public IAutoCompleteHandler? AutoCompletionHandler { private get; set; }

    public IHighlightHandler? HighlightHandler { private get; set; }

    public char[]? WordSeparators { get; set; }
    
    private KeyHandler? _keyHandler;

    private readonly ShortcutBag _shortcuts = new();

    public string Read(string prompt = "", string @default = "")
    {
        Console.Write(prompt);
        bool enterPressed = false;
        _keyHandler = new KeyHandler(new Renderer(), _shortcuts)
        {
            HistoryHandler = HistoryHandler,
            AutoCompleteHandler = AutoCompletionHandler,
            HighlightHandler = HighlightHandler,
            OnEnter = () => enterPressed = true,
        };

        if (WordSeparators != null)
            _keyHandler.WordSeparators = WordSeparators;

        while (!enterPressed)
        {
            _keyHandler.Handle(Console.ReadKey(true));
        }

        string text = _keyHandler.Text;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(@default))
        {
            text = @default;
        }

        return text;
    }

    public void RegisterShortcut(KeyPress keyPress, Action<KeyHandler> action)
    {
        _shortcuts.Add(keyPress, action);
    }
}
