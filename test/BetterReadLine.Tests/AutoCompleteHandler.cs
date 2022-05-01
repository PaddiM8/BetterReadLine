using System;

namespace BetterReadLine.Tests
{
    class AutoCompleteHandler : IAutoCompleteHandler
    {
        public char[] Separators { get; set; } = new char[] { ' ', '.', '/', '\\', ':' };
        public string[] GetSuggestions(string text, int index) => new string[] { "World", "Angel", "Love" };
    }
}