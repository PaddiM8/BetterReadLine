# BetterReadLine

BetterReadLine is an implementation of a readline prompt in C# that supports configurable shortcuts, history and syntax highlighting. This repository is a fork of [tonerdo/readline](https://github.com/tonerdo/readline) but most parts have been rewritten.

## Shortcut Guide

| Shortcut                       | Comment                           |
| ------------------------------ | --------------------------------- |
| `Ctrl`+`A` / `HOME`            | Beginning of line                 |
| `Ctrl`+`B` / `←`               | Backward one character            |
| `Ctrl`+`C`                     | Send EOF                          |
| `Ctrl`+`E` / `END`             | End of line                       |
| `Ctrl`+`F` / `→`               | Forward one character             |
| `Ctrl`+`H` / `Backspace`       | Delete previous character         |
| `Tab`                          | Command line completion           |
| `Shift`+`Tab`                  | Backwards command line completion |
| `Ctrl`+`J` / `Enter`           | Line feed                         |
| `Ctrl`+`K`                     | Cut text to the end of line       |
| `Ctrl`+`L` / `Esc`             | Clear line                        |
| `Ctrl`+`M`                     | Same as Enter key                 |
| `Ctrl`+`N` / `↓`               | Forward in history                |
| `Ctrl`+`P` / `↑`               | Backward in history               |
| `Ctrl`+`U`                     | Cut text to the start of line     |
| `Ctrl`+`W`                     | Cut previous word                 |
| `Backspace`                    | Delete previous character         |
| `Ctrl` + `D` / `Delete`        | Delete succeeding character       |


## Usage

```csharp
var readLine = new ReadLine();
string input = readLine.Read(">> ")
{
    AutoCompletionHandler = new AutoCompleteHandler(),
    HighlightHandler = new HighlightHandler(),
    HistoryEnabled = true,
    WordSeparators = new[] { ' ', '/' },
};

readLine.GetHistory();
readLine.AddHistory("echo hello");
readLine.ClearHistory();

...
class AutoCompleteHandler : IAutoCompleteHandler { ... }

class HighlightHandler : IHighlightHandler { ... }
```

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
