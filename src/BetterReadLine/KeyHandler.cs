using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BetterReadLine.Abstractions;

namespace BetterReadLine;

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

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class KeyHandler
{

    public bool IsEndOfBuffer => _console2.CursorLeft == _console2.BufferWidth - 1;
    
    public bool IsEndOfLine => _cursorPos == _cursorLimit;
    
    public bool IsInAutoCompleteMode => _completions != null;

    public bool IsStartOfBuffer => _console2.CursorLeft == 0;

    public bool IsStartOfLine => _cursorPos == 0;

    public int CursorPos => _cursorPos;
    
    public string Text => _text.ToString();
    
    private int _cursorPos;
    private int _cursorLimit;
    private readonly StringBuilder _text;
    private readonly List<string> _history;
    private int _historyIndex;
    private ConsoleKeyInfo _keyInfo;
    private readonly Dictionary<KeyPress, Action> _defaultShortcuts;
    private readonly IAutoCompleteHandler? _autoCompleteHandler;
    private readonly ShortcutBag? _shortcuts;
    private string[]? _completions;
    private int _completionStart;
    private int _completionsIndex;
    private readonly IConsole _console2;

    internal KeyHandler(IConsole console, List<string>? history, IAutoCompleteHandler? autoCompleteHandler, ShortcutBag? shortcuts)
    {
        _console2 = console;

        _history = history ?? new List<string>();
        _historyIndex = _history.Count;
        _autoCompleteHandler = autoCompleteHandler;
        _shortcuts = shortcuts;
        _text = new StringBuilder();
        _defaultShortcuts = new Dictionary<KeyPress, Action>
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

    internal void Handle(ConsoleKeyInfo keyInfo)
    {
        _keyInfo = keyInfo;

        // If in auto complete mode and Tab wasn't pressed
        if (IsInAutoCompleteMode && _keyInfo.Key != ConsoleKey.Tab)
            ResetAutoComplete();

        if (_shortcuts?.TryGetValue(new(keyInfo.Modifiers, keyInfo.Key), out var action1) ?? false)
        {
            action1?.Invoke(this);
            return;
        }
        
        _defaultShortcuts.TryGetValue(new(keyInfo.Modifiers, keyInfo.Key), out var action2);
        action2 ??= WriteChar;
        action2.Invoke();
    }

    public void Backspace()
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

    public void ClearLine()
    {
        MoveCursorEnd();
        while (!IsStartOfLine)
            Backspace();
    }

    public void Delete()
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

    public void MoveCursorLeft()
    {
        if (IsStartOfLine)
            return;

        if (IsStartOfBuffer)
            _console2.SetCursorPosition(_console2.BufferWidth - 1, _console2.CursorTop - 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft - 1, _console2.CursorTop);

        _cursorPos--;
    }

    public void MoveCursorHome()
    {
        while (!IsStartOfLine)
            MoveCursorLeft();
    }

    public void MoveCursorRight()
    {
        if (IsEndOfLine)
            return;

        if (IsEndOfBuffer)
            _console2.SetCursorPosition(0, _console2.CursorTop + 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft + 1, _console2.CursorTop);

        _cursorPos++;
    }

    public void MoveCursorEnd()
    {
        while (!IsEndOfLine)
            MoveCursorRight();
    }

    public void MoveCursorWordLeft()
    {
        while (!IsStartOfLine && _text[_cursorPos - 1] == ' ')
            MoveCursorLeft();
        while (!IsStartOfLine && _text[_cursorPos - 1] != ' ')
            MoveCursorLeft();
    }
    
    public void MoveCursorWordRight()
    {
        while (_cursorPos + 1 < _text.Length && _text[_cursorPos + 1] == ' ')
            MoveCursorRight();
        while (_cursorPos + 1 < _text.Length && _text[_cursorPos + 1] != ' ')
            MoveCursorRight();
        
        MoveCursorRight();
    }

    public void NextAutoComplete()
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

        if (_autoCompleteHandler == null)
            return;

        string text = _text.ToString();
        _completionStart = _autoCompleteHandler.GetCompletionStart(text, _cursorPos);
        _completions = _autoCompleteHandler.GetSuggestions(text, _completionStart, _cursorPos);
        _completions = _completions?.Length == 0 ? null : _completions;

        if (_completions == null)
            return;

        StartAutoComplete();
    }

    public void NextHistory()
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

    public void PreviousAutoComplete()
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

    public void PrevHistory()
    {
        if (_historyIndex > 0)
        {
            _historyIndex--;
            WriteNewString(_history[_historyIndex]);
        }
    }

    public void RemoveToEnd()
    {
        int pos = _cursorPos;
        MoveCursorEnd();
        while (_cursorPos > pos)
            Backspace();
    }
    
    public void RemoveToHome()
    {
        while (!IsStartOfLine)
            Backspace();
    }
    
    public void RemoveWordLeft()
    {
        while (!IsStartOfLine && _text[_cursorPos - 1] == ' ')
            Backspace();
        while (!IsStartOfLine && _text[_cursorPos - 1] != ' ')
            Backspace();
    }
    
    public void ResetAutoComplete()
    {
        _completions = null;
        _completionsIndex = 0;
    }

    public void StartAutoComplete()
    {
        while (_cursorPos > _completionStart)
            Backspace();

        _completionsIndex = 0;

        WriteString(_completions![_completionsIndex]);
    }

    public void TransposeChars()
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

    public void WriteNewString(string str)
    {
        ClearLine();
        foreach (char character in str)
            WriteChar(character);
    }

    public void WriteString(string str)
    {
        foreach (char character in str)
            WriteChar(character);
    }

    public void WriteChar() => WriteChar(_keyInfo.KeyChar);

    public void WriteChar(char c)
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