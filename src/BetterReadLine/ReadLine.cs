using System;
using System.Collections.Generic;
using BetterReadLine.Abstractions;

namespace BetterReadLine;

public class ReadLine
{

    public void AddHistory(params string[] text) => _history.AddRange(text);
        
    public List<string> GetHistory() => _history;
        
    public void ClearHistory() => _history = new List<string>();
        
    public bool HistoryEnabled { get; set; }
        
    public IAutoCompleteHandler? AutoCompletionHandler { private get; set; }
    
    private List<string> _history;

    public ReadLine()
    {
        _history = new List<string>();
    }

    public string Read(string prompt = "", string @default = "")
    {
        Console.Write(prompt);
        var keyHandler = new KeyHandler(new Console2(), _history, AutoCompletionHandler);
        string text = GetText(keyHandler);

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
        var keyHandler = new KeyHandler(new Console2() { PasswordMode = true }, null, null);
        return GetText(keyHandler);
    }

    private string GetText(KeyHandler keyHandler)
    {
        var keyInfo = Console.ReadKey(true);
        while (keyInfo.Key != ConsoleKey.Enter)
        {
            keyHandler.Handle(keyInfo);
            keyInfo = Console.ReadKey(true);
        }

        Console.WriteLine();
        return keyHandler.Text;
    }
}