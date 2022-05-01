using System;
using System.Collections.Generic;
using System.Text;
using ReadLine.Abstractions;

namespace ReadLine;

public readonly struct KeyPress
{
    public ConsoleModifiers Modifiers { get;  }
    
    public ConsoleKey Key { get;  }
    
    public KeyPress(ConsoleKey key)
    {
        Modifiers = 0;
        Key = key;
    }

    public KeyPress(ConsoleModifiers modifiers, ConsoleKey key)
    {
        Modifiers = modifiers;
        Key = key;
    }
}

internal class KeyHandler
{

    private bool IsEndOfBuffer => _console2.CursorLeft == _console2.BufferWidth - 1;
    
    private bool IsEndOfLine => _cursorPos == _cursorLimit;
    
    private bool IsInAutoCompleteMode => _completions != null;

    private bool IsStartOfBuffer => _console2.CursorLeft == 0;

    private bool IsStartOfLine => _cursorPos == 0;

    public string Text => _text.ToString();
    
    private int _cursorPos;
    private int _cursorLimit;
    private readonly StringBuilder _text;
    private readonly List<string> _history;
    private int _historyIndex;
    private ConsoleKeyInfo _keyInfo;
    private readonly Dictionary<KeyPress, Action> _keyActions;
    private readonly IAutoCompleteHandler? _autoCompleteHandler;
    private string[]? _completions;
    private int _completionStart;
    private int _completionsIndex;
    private readonly IConsole _console2;

    public KeyHandler(IConsole console, List<string>? history, IAutoCompleteHandler? autoCompleteHandler)
    {
        _console2 = console;

        _history = history ?? new List<string>();
        _historyIndex = _history.Count;
        _autoCompleteHandler = autoCompleteHandler;
        _text = new StringBuilder();
        _keyActions = new Dictionary<KeyPress, Action>
        {
            [new(ConsoleKey.LeftArrow)] = MoveCursorLeft,
            [new(ConsoleKey.RightArrow)] = MoveCursorRight,
            [new(ConsoleModifiers.Control, ConsoleKey.LeftArrow)] = MoveCursorWordLeft,
            [new(ConsoleModifiers.Control, ConsoleKey.RightArrow)] = MoveCursorWordRight,
            [new(ConsoleKey.UpArrow)] = PrevHistory,
            [new(ConsoleKey.DownArrow)] = NextHistory,
            [new(ConsoleKey.Home)] = MoveCursorHome,
            [new(ConsoleKey.End)] = MoveCursorEnd,
            [new(ConsoleKey.Backspace)] = Backspace,
            [new(ConsoleKey.Delete)] = Delete,
            [new(ConsoleModifiers.Control, ConsoleKey.A)] = MoveCursorHome,
            [new(ConsoleModifiers.Control, ConsoleKey.B)] = MoveCursorLeft,
            [new(ConsoleModifiers.Control, ConsoleKey.D)] = Delete,
            [new(ConsoleModifiers.Control, ConsoleKey.E)] = MoveCursorEnd,
            [new(ConsoleModifiers.Control, ConsoleKey.F)] = MoveCursorRight,
            [new(ConsoleModifiers.Control, ConsoleKey.H)] = Backspace,
            [new(ConsoleModifiers.Control, ConsoleKey.K)] = RemoveToEnd,
            [new(ConsoleModifiers.Control, ConsoleKey.L)] = ClearLine,
            [new(ConsoleModifiers.Control, ConsoleKey.N)] = NextHistory,
            [new(ConsoleModifiers.Control, ConsoleKey.P)] = PrevHistory,
            [new(ConsoleModifiers.Control, ConsoleKey.T)] = TransposeChars,
            [new(ConsoleModifiers.Control, ConsoleKey.U)] = RemoveToHome,
            [new(ConsoleModifiers.Control, ConsoleKey.W)] = RemoveWordLeft,
            [new(ConsoleKey.Tab)] = NextAutoComplete,
            [new(ConsoleModifiers.Shift, ConsoleKey.Tab)] = PreviousAutoComplete,
        };
    }

    public void Handle(ConsoleKeyInfo keyInfo)
    {
        _keyInfo = keyInfo;

        // If in auto complete mode and Tab wasn't pressed
        if (IsInAutoCompleteMode && _keyInfo.Key != ConsoleKey.Tab)
            ResetAutoComplete();

        _keyActions.TryGetValue(new(keyInfo.Modifiers, keyInfo.Key), out var action);
        action ??= WriteChar;
        action.Invoke();
    }

    private void Backspace()
    {
        if (IsStartOfLine)
            return;

        MoveCursorLeft();
        int index = _cursorPos;
        _text.Remove(index, 1);
        string replacement = _text.ToString().Substring(index);
        int left = _console2.CursorLeft;
        int top = _console2.CursorTop;
        _console2.Write($"{replacement} ");
        _console2.SetCursorPosition(left, top);
        _cursorLimit--;
    }

    private void ClearLine()
    {
        MoveCursorEnd();
        while (!IsStartOfLine)
            Backspace();
    }

    private void Delete()
    {
        if (IsEndOfLine)
            return;

        int index = _cursorPos;
        _text.Remove(index, 1);
        string replacement = _text.ToString().Substring(index);
        int left = _console2.CursorLeft;
        int top = _console2.CursorTop;
        _console2.Write($"{replacement} ");
        _console2.SetCursorPosition(left, top);
        _cursorLimit--;
    }

    private void MoveCursorLeft()
    {
        if (IsStartOfLine)
            return;

        if (IsStartOfBuffer)
            _console2.SetCursorPosition(_console2.BufferWidth - 1, _console2.CursorTop - 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft - 1, _console2.CursorTop);

        _cursorPos--;
    }

    private void MoveCursorHome()
    {
        while (!IsStartOfLine)
            MoveCursorLeft();
    }

    private void MoveCursorRight()
    {
        if (IsEndOfLine)
            return;

        if (IsEndOfBuffer)
            _console2.SetCursorPosition(0, _console2.CursorTop + 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft + 1, _console2.CursorTop);

        _cursorPos++;
    }

    private void MoveCursorEnd()
    {
        while (!IsEndOfLine)
            MoveCursorRight();
    }

    private void MoveCursorWordLeft()
    {
        while (!IsStartOfLine && _text[_cursorPos - 1] == ' ')
            MoveCursorLeft();
        while (!IsStartOfLine && _text[_cursorPos - 1] != ' ')
            MoveCursorLeft();
    }
    
    private void MoveCursorWordRight()
    {
        while (_cursorPos + 1 < _text.Length && _text[_cursorPos + 1] == ' ')
            MoveCursorRight();
        while (_cursorPos + 1 < _text.Length && _text[_cursorPos + 1] != ' ')
            MoveCursorRight();
        
        MoveCursorRight();
    }

    private void NextAutoComplete()
    {
        if (IsInAutoCompleteMode)
        {
            while (_cursorPos > _completionStart)
                Backspace();

            _completionsIndex++;

            if (_completionsIndex == _completions!.Length)
                _completionsIndex = 0;

            WriteString(_completions[_completionsIndex]);
            return;
        }

        if (_autoCompleteHandler == null || !IsEndOfLine)
            return;

        string text = _text.ToString();

        _completionStart = text.LastIndexOfAny(_autoCompleteHandler.Separators);
        _completionStart = _completionStart == -1 ? 0 : _completionStart + 1;

        _completions = _autoCompleteHandler.GetSuggestions(text, _completionStart);
        _completions = _completions?.Length == 0 ? null : _completions;

        if (_completions == null)
            return;

        StartAutoComplete();
    }

    private void NextHistory()
    {
        if (_historyIndex < _history.Count)
        {
            _historyIndex++;
            if (_historyIndex == _history.Count)
                ClearLine();
            else
                WriteNewString(_history[_historyIndex]);
        }
    }

    private void PreviousAutoComplete()
    {
        if (!IsInAutoCompleteMode)
            return;
        
        while (_cursorPos > _completionStart)
            Backspace();

        _completionsIndex--;

        if (_completionsIndex == -1)
            _completionsIndex = _completions!.Length - 1;

        WriteString(_completions![_completionsIndex]);
    }

    private void PrevHistory()
    {
        if (_historyIndex > 0)
        {
            _historyIndex--;
            WriteNewString(_history[_historyIndex]);
        }
    }

    private void RemoveToEnd()
    {
        int pos = _cursorPos;
        MoveCursorEnd();
        while (_cursorPos > pos)
            Backspace();
    }
    
    private void RemoveToHome()
    {
        while (!IsStartOfLine)
            Backspace();
    }
    
    private void RemoveWordLeft()
    {
        while (!IsStartOfLine && _text[_cursorPos - 1] == ' ')
            Backspace();
        while (!IsStartOfLine && _text[_cursorPos - 1] != ' ')
            Backspace();
    }
    
    private void ResetAutoComplete()
    {
        _completions = null;
        _completionsIndex = 0;
    }

    private void StartAutoComplete()
    {
        while (_cursorPos > _completionStart)
            Backspace();

        _completionsIndex = 0;

        WriteString(_completions![_completionsIndex]);
    }

    private void TransposeChars()
    {
        // local helper functions
        bool AlmostEndOfLine() => (_cursorLimit - _cursorPos) == 1;
        int IncrementIf(Func<bool> expression, int index) =>  expression() ? index + 1 : index;
        int DecrementIf(Func<bool> expression, int index) => expression() ? index - 1 : index;

        if (IsStartOfLine) { return; }

        var firstIdx = DecrementIf(() => IsEndOfLine, _cursorPos - 1);
        var secondIdx = DecrementIf(() => IsEndOfLine, _cursorPos);

        (_text[secondIdx], _text[firstIdx]) = (_text[firstIdx], _text[secondIdx]);

        var left = IncrementIf(AlmostEndOfLine, _console2.CursorLeft);
        var cursorPosition = IncrementIf(AlmostEndOfLine, _cursorPos);

        WriteNewString(_text.ToString());

        _console2.SetCursorPosition(left, _console2.CursorTop);
        _cursorPos = cursorPosition;

        MoveCursorRight();
    }

    private void WriteNewString(string str)
    {
        ClearLine();
        foreach (char character in str)
            WriteChar(character);
    }

    private void WriteString(string str)
    {
        foreach (char character in str)
            WriteChar(character);
    }

    private void WriteChar() => WriteChar(_keyInfo.KeyChar);

    private void WriteChar(char c)
    {
        if (IsEndOfLine)
        {
            _text.Append(c);
            _console2.Write(c.ToString());
            _cursorPos++;
        }
        else
        {
            int left = _console2.CursorLeft;
            int top = _console2.CursorTop;
            string str = _text.ToString().Substring(_cursorPos);
            _text.Insert(_cursorPos, c);
            _console2.Write(c.ToString() + str);
            _console2.SetCursorPosition(left, top);
            MoveCursorRight();
        }

        _cursorLimit++;
    }
}