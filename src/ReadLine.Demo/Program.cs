using System;

namespace ReadLine.Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ReadLine Library Demo");
            Console.WriteLine("---------------------");
            Console.WriteLine();

            string[] history = { "ls -a", "dotnet run", "git init" };
            ReadLine.AddHistory(history);

            ReadLine.AutoCompletionHandler = new AutoCompletionHandler();

            string input = ReadLine.Read("(prompt)> ");
            Console.WriteLine(input);

            input = ReadLine.ReadPassword("Enter Password> ");
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
}
