using System;

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

        input = readLine.ReadPassword("Enter Password> ");
        Console.WriteLine(input);
    }
}

class AutoCompletionHandler : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = { ' ', '.', '/', '\\', ':' };
        
    public string[] GetSuggestions(string text, int index)
    {
        return text.StartsWith("git ") ? new[] { "init", "clone", "pull", "push" } : null;
    }
}