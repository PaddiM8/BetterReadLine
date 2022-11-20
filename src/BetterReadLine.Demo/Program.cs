using System;
using System.Collections.Generic;

namespace BetterReadLine.Demo;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("BetterReadline Library Demo");
        Console.WriteLine("---------------------");
        Console.WriteLine();

        var readLine = new ReadLine();
        string[] history = { "ls -a", "dotnet run", "git init" };
        readLine.AddHistory(history);

        readLine.AutoCompletionHandler = new AutoCompletionHandler();

        string input = readLine.Read("(prompt)> ");
        Console.WriteLine(input);
    }
}

class AutoCompletionHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = { ' ', '.', '/', '\\', ':' };
        
    public IList<Completion> GetSuggestions(string text, int start, int end)
    {
        return text.StartsWith("git ")
            ? new Completion[] { new("init"), new("clone"), new("pull"), new("push") }
            : null;
    }
}