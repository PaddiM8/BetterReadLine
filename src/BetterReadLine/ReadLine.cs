using System;
using System.Collections.Generic;
using BetterReadLine.Render;

namespace BetterReadLine;

public class ReadLine
{

    public void AddHistory(params string[] text) => _history.AddRange(text);
        
    public List<string> GetHistory() => _history;
        
    public void ClearHistory() => _history = new List<string>();
        
    public bool HistoryEnabled { get; set; }
        
    public IAutoCompleteHandler? AutoCompletionHandler { private get; set; }

    public IHighlightHandler? HighlightHandler { private get; set; }

    public char[]? WordSeparators { get; set; }
    
    private List<string> _history;

    private KeyHandler? _keyHandler;

    private readonly ShortcutBag _shortcuts = new();

    public ReadLine()
    {
        _history = new List<string>();
    }

    public string Read(string prompt = "", string @default = "")
    {
        Console.Write(prompt);
        bool enterPressed = false;
        _keyHandler = new KeyHandler(new Renderer(), _history, _shortcuts)
        {
            AutoCompleteHandler = AutoCompletionHandler,
            HighlightHandler = HighlightHandler,
            OnEnter = () => enterPressed = true,
        };

        if (WordSeparators != null)
            _keyHandler.WordSeparators = WordSeparators;

        //string text = GetText();
        while (!enterPressed)
        {
            _keyHandler.Handle(Console.ReadKey(true));
        }

        string text = _keyHandler.Text;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(@default))
        {
            text = @default;
        }
        else
        {
            if (HistoryEnabled)
                _history.Add(text);
        }

        return text;
    }

    public void RegisterShortcut(KeyPress keyPress, Action<KeyHandler> action)
    {
        _shortcuts.Add(keyPress, action);
    }

    /*private string GetText()
    {
        var keyInfo = Console.ReadKey(true);
        while (keyInfo.Key != ConsoleKey.Enter)
        {
            _keyHandler!.Handle(keyInfo);
            keyInfo = Console.ReadKey(true);
        }

        _keyHandler!.HandleEnter();
        Console.WriteLine();

        return _keyHandler!.Text;
    }*/
}
