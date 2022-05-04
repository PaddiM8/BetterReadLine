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
        _keyHandler = new KeyHandler(new Renderer(), _history, _shortcuts)
        {
            AutoCompleteHandler = AutoCompletionHandler,
            HighlightHandler = HighlightHandler,
        };

        if (WordSeparators != null)
            _keyHandler.WordSeparators = WordSeparators;

        string text = GetText();

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

    public string ReadPassword(string prompt = "")
    {
        Console.Write(prompt);
        _keyHandler = new KeyHandler(new Renderer { PasswordMode = true }, null, _shortcuts);
        
        return GetText();
    }

    public void RegisterShortcut(KeyPress keyPress, Action<KeyHandler> action)
    {
        _shortcuts.Add(keyPress, action);
    }

    private string GetText()
    {
        var keyInfo = Console.ReadKey(true);
        while (keyInfo.Key != ConsoleKey.Enter)
        {
            _keyHandler!.Handle(keyInfo);
            keyInfo = Console.ReadKey(true);
        }

        Console.WriteLine();
        return _keyHandler!.Text;
    }
}