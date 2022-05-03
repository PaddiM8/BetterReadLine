using System;

namespace BetterReadLine.Tests
{
    class AutoCompleteHandler : IAutoCompleteHandler
    {
        public char[] Separators { get; set; } = { ' ', '.', '/', '\\', ':' };

        public string[] GetSuggestions(string text, int start, int end) => new[] { "World", "Angel", "Love" };
    }
}