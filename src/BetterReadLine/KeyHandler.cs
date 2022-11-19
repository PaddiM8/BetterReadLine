using System;
using System.Collections.Generic;
using System.Linq;
using BetterReadLine.Render;

namespace BetterReadLine;

public readonly struct KeyPress
{
    public ConsoleModifiers Modifiers { get; }
    
    public ConsoleKey Key { get; }
    
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

public class KeyHandler
{
    public string Text => _renderer.Text;

    public char[] WordSeparators = { ' ' };

    internal IAutoCompleteHandler? AutoCompleteHandler { get; set; }

    internal IHighlightHandler? HighlightHandler
    {
        get => _highlightHandler;
        set
        {
            _highlightHandler = value;
            if (value == null)
            {
                _renderer.OnHighlight(null);
                return;
            }

            _renderer.OnHighlight(value.Highlight);
        }
    }

    internal Action? OnEnter { get; set; }

    private IHighlightHandler? _highlightHandler;
    private readonly List<string> _history;
    private int _historyIndex;
    private ConsoleKeyInfo _keyInfo;
    private readonly Dictionary<KeyPress, Action> _defaultShortcuts;
    private readonly ShortcutBag? _shortcuts;
    private readonly IRenderer _renderer;
    private readonly CompletionState _completionState;

    internal KeyHandler(IRenderer renderer, List<string>? history, ShortcutBag? shortcuts)
    {
        _renderer = renderer;
        _completionState = new CompletionState(renderer);

        _history = history ?? new List<string>();
        _historyIndex = _history.Count;
        _shortcuts = shortcuts;
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
            [new(ConsoleModifiers.Alt, ConsoleKey.Backspace)] = RemoveWordLeft,
            [new(ConsoleModifiers.Control, ConsoleKey.Backspace)] = RemoveWordLeft,
            [new(ConsoleModifiers.Control, ConsoleKey.A)] = MoveCursorHome,
            [new(ConsoleModifiers.Control, ConsoleKey.B)] = MoveCursorLeft,
            [new(ConsoleModifiers.Control, ConsoleKey.D)] = Delete,
            [new(ConsoleModifiers.Control, ConsoleKey.E)] = MoveCursorEnd,
            [new(ConsoleModifiers.Control, ConsoleKey.F)] = MoveCursorRight,
            [new(ConsoleModifiers.Control, ConsoleKey.H)] = Backspace,
            [new(ConsoleModifiers.Control, ConsoleKey.K)] = RemoveToEnd,
            [new(ConsoleModifiers.Control, ConsoleKey.L)] = ClearConsole,
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
        if (_completionState.IsActive && _keyInfo.Key != ConsoleKey.Tab)
            _completionState.Reset();

        if (OnEnter != null && keyInfo.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            OnEnter();
            return;
        }

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
        _renderer.RemoveLeft(1);
    }

    public void ClearConsole()
    {
        _renderer.Clear();
        OnEnter?.Invoke();
    }

    public void Delete()
    {
        _renderer.RemoveRight(1);
    }

    public void MoveCursorLeft()
    {
        _renderer.Caret--;
    }

    public void MoveCursorHome()
    {
        _renderer.Caret = 0;
    }

    public void MoveCursorRight()
    {
        _renderer.Caret++;
    }

    public void MoveCursorEnd()
    {
        _renderer.Caret = _renderer.Text.Length;
    }

    public void MoveCursorWordLeft()
    {
        string text = _renderer.Text;
        int i = _renderer.Caret;
        while (i > 0 && WordSeparators.Contains(text[i - 1]))
            i--;
        while (i > 0 && !WordSeparators.Contains(text[i - 1]))
            i--;

        _renderer.Caret = i;
    }
    
    public void MoveCursorWordRight()
    {
        string text = _renderer.Text;
        int i = _renderer.Caret;
        while (i + 1 < text.Length && WordSeparators.Contains(text[i + 1]))
            i++;
        while (i + 1 < text.Length && !WordSeparators.Contains(text[i + 1]))
            i++;

        _renderer.Caret = i + 1;
    }

    public void NextAutoComplete()
    {
        if (AutoCompleteHandler == null)
            return;

        if (_completionState.IsActive)
        {
            _completionState.Next();
            return;
        }

        int start = AutoCompleteHandler.GetCompletionStart(_renderer.Text, _renderer.Caret);
        var completions = AutoCompleteHandler.GetSuggestions(_renderer.Text, start, _renderer.Caret);
        if (completions.Count > 0)
        {
            _completionState.StartNew(completions, start);
        }
    }

    public void PreviousAutoComplete()
    {
        if (!_completionState.IsActive || AutoCompleteHandler == null)
            return;

        _completionState.Previous();
    }

    public void NextHistory()
    {
        if (_historyIndex < _history.Count)
        {
            _historyIndex++;
            if (_historyIndex == _history.Count)
            {
                _renderer.ClearLineRight(0);
            }
            else
            {
                _renderer.ClearLineRight(0);
                _renderer.Insert(_history[_historyIndex]);
            }
        }
    }

    public void PrevHistory()
    {
        if (_historyIndex > 0)
        {
            _historyIndex--;
            _renderer.ClearLineRight(0);
            _renderer.Insert(_history[_historyIndex]);
        }
    }

    public void RemoveToEnd()
    {
        _renderer.ClearLineRight();
    }
    
    public void RemoveToHome()
    {
        _renderer.ClearLineLeft();
    }
    
    public void RemoveWordLeft()
    {
        string text = _renderer.Text;
        int i = _renderer.Caret;
        while (i > 0 && WordSeparators.Contains(text[i - 1]))
            i--;
        while (i > 0 && !WordSeparators.Contains(text[i - 1]))
            i--;

        _renderer.RemoveLeft(_renderer.Caret - i);
    }
    
    public void TransposeChars()
    {
        // TODO: Implement TransposeChars
    }

    public void WriteChar() => WriteChar(_keyInfo.KeyChar);

    public void WriteChar(char c)
    {
        _renderer.Insert(c.ToString());
    }
}
