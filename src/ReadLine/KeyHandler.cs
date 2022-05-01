using System;
using System.Collections.Generic;
using System.Text;
using ReadLine.Abstractions;

namespace ReadLine;

internal class KeyHandler
{
    private bool IsStartOfLine() => _cursorPos == 0;

    private bool IsEndOfLine() => _cursorPos == _cursorLimit;

    private bool IsStartOfBuffer() => _console2.CursorLeft == 0;

    private bool IsEndOfBuffer() => _console2.CursorLeft == _console2.BufferWidth - 1;
        
    private bool IsInAutoCompleteMode() => _completions != null;
    
    private int _cursorPos;
    private int _cursorLimit;
    private readonly StringBuilder _text;
    private readonly List<string> _history;
    private int _historyIndex;
    private ConsoleKeyInfo _keyInfo;
    private readonly Dictionary<string, Action> _keyActions;
    private string[]? _completions;
    private int _completionStart;
    private int _completionsIndex;
    private readonly IConsole _console2;

    public KeyHandler(IConsole console, List<string>? history, IAutoCompleteHandler? autoCompleteHandler)
    {
        _console2 = console;

        _history = history ?? new List<string>();
        _historyIndex = _history.Count;
        _text = new StringBuilder();
        _keyActions = new Dictionary<string, Action>
        {
            ["LeftArrow"] = MoveCursorLeft,
            ["Home"] = MoveCursorHome,
            ["End"] = MoveCursorEnd,
            ["ControlA"] = MoveCursorHome,
            ["ControlB"] = MoveCursorLeft,
            ["RightArrow"] = MoveCursorRight,
            ["ControlF"] = MoveCursorRight,
            ["ControlE"] = MoveCursorEnd,
            ["Backspace"] = Backspace,
            ["Delete"] = Delete,
            ["ControlD"] = Delete,
            ["ControlH"] = Backspace,
            ["ControlL"] = ClearLine,
            ["Escape"] = ClearLine,
            ["UpArrow"] = PrevHistory,
            ["ControlP"] = PrevHistory,
            ["DownArrow"] = NextHistory,
            ["ControlN"] = NextHistory,
            ["ControlU"] = () =>
            {
                while (!IsStartOfLine())
                    Backspace();
            },
            ["ControlK"] = () =>
            {
                int pos = _cursorPos;
                MoveCursorEnd();
                while (_cursorPos > pos)
                    Backspace();
            },
            ["ControlW"] = () =>
            {
                while (!IsStartOfLine() && _text[_cursorPos - 1] == ' ')
                    Backspace();
                while (!IsStartOfLine() && _text[_cursorPos - 1] != ' ')
                    Backspace();
            },
            ["ControlT"] = TransposeChars,
            ["Tab"] = () =>
            {
                if (IsInAutoCompleteMode())
                {
                    NextAutoComplete();
                }
                else
                {
                    if (autoCompleteHandler == null || !IsEndOfLine())
                        return;

                    string text = _text.ToString();

                    _completionStart = text.LastIndexOfAny(autoCompleteHandler.Separators);
                    _completionStart = _completionStart == -1 ? 0 : _completionStart + 1;

                    _completions = autoCompleteHandler.GetSuggestions(text, _completionStart);
                    _completions = _completions?.Length == 0 ? null : _completions;

                    if (_completions == null)
                        return;

                    StartAutoComplete();
                }
            },
            ["ShiftTab"] = () =>
            {
                if (IsInAutoCompleteMode())
                {
                    PreviousAutoComplete();
                }
            },
        };
    }

    private void MoveCursorLeft()
    {
        if (IsStartOfLine())
            return;

        if (IsStartOfBuffer())
            _console2.SetCursorPosition(_console2.BufferWidth - 1, _console2.CursorTop - 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft - 1, _console2.CursorTop);

        _cursorPos--;
    }

    private void MoveCursorHome()
    {
        while (!IsStartOfLine())
            MoveCursorLeft();
    }

    private string BuildKeyInput()
    {
        return (_keyInfo.Modifiers != ConsoleModifiers.Control && _keyInfo.Modifiers != ConsoleModifiers.Shift) ?
            _keyInfo.Key.ToString() : _keyInfo.Modifiers.ToString() + _keyInfo.Key.ToString();
    }

    private void MoveCursorRight()
    {
        if (IsEndOfLine())
            return;

        if (IsEndOfBuffer())
            _console2.SetCursorPosition(0, _console2.CursorTop + 1);
        else
            _console2.SetCursorPosition(_console2.CursorLeft + 1, _console2.CursorTop);

        _cursorPos++;
    }

    private void MoveCursorEnd()
    {
        while (!IsEndOfLine())
            MoveCursorRight();
    }

    private void ClearLine()
    {
        MoveCursorEnd();
        while (!IsStartOfLine())
            Backspace();
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
        if (IsEndOfLine())
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

    private void Backspace()
    {
        if (IsStartOfLine())
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

    private void Delete()
    {
        if (IsEndOfLine())
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

    private void TransposeChars()
    {
        // local helper functions
        bool AlmostEndOfLine() => (_cursorLimit - _cursorPos) == 1;
        int IncrementIf(Func<bool> expression, int index) =>  expression() ? index + 1 : index;
        int DecrementIf(Func<bool> expression, int index) => expression() ? index - 1 : index;

        if (IsStartOfLine()) { return; }

        var firstIdx = DecrementIf(IsEndOfLine, _cursorPos - 1);
        var secondIdx = DecrementIf(IsEndOfLine, _cursorPos);

        (_text[secondIdx], _text[firstIdx]) = (_text[firstIdx], _text[secondIdx]);

        var left = IncrementIf(AlmostEndOfLine, _console2.CursorLeft);
        var cursorPosition = IncrementIf(AlmostEndOfLine, _cursorPos);

        WriteNewString(_text.ToString());

        _console2.SetCursorPosition(left, _console2.CursorTop);
        _cursorPos = cursorPosition;

        MoveCursorRight();
    }

    private void StartAutoComplete()
    {
        while (_cursorPos > _completionStart)
            Backspace();

        _completionsIndex = 0;

        WriteString(_completions![_completionsIndex]);
    }

    private void NextAutoComplete()
    {
        while (_cursorPos > _completionStart)
            Backspace();

        _completionsIndex++;

        if (_completionsIndex == _completions!.Length)
            _completionsIndex = 0;

        WriteString(_completions[_completionsIndex]);
    }

    private void PreviousAutoComplete()
    {
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

    private void ResetAutoComplete()
    {
        _completions = null;
        _completionsIndex = 0;
    }

    public string Text
    {
        get
        {
            return _text.ToString();
        }
    }

    public void Handle(ConsoleKeyInfo keyInfo)
    {
        _keyInfo = keyInfo;

        // If in auto complete mode and Tab wasn't pressed
        if (IsInAutoCompleteMode() && _keyInfo.Key != ConsoleKey.Tab)
            ResetAutoComplete();

        _keyActions.TryGetValue(BuildKeyInput(), out var action);
        action ??= WriteChar;
        action.Invoke();
    }
}