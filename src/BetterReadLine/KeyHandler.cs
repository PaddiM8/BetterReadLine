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
    public bool IsInAutoCompleteMode => _completions != null;

    public string Text => _renderer.Text;

    public char[] WordSeparators = { ' ' };

    internal IAutoCompleteHandler? AutoCompleteHandler { get; set; }

    internal IHighlightHandler? HighlightHandler { get; set; }

    private readonly List<string> _history;
    private int _historyIndex;
    private ConsoleKeyInfo _keyInfo;
    private readonly Dictionary<KeyPress, Action> _defaultShortcuts;
    private readonly ShortcutBag? _shortcuts;
    private string[]? _completions;
    private int _completionStart;
    private readonly IRenderer _renderer;
    private readonly SelectionListing _selectionListing;

    internal KeyHandler(IRenderer renderer, List<string>? history, ShortcutBag? shortcuts)
    {
        _renderer = renderer;
        _selectionListing = new SelectionListing(renderer);

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

        if (HighlightHandler != null && _renderer.Text.Length > 0)
        {
            // This is done manually to optimise it in order to avoid flickering
            string highlighted = HighlightHandler.Highlight(_renderer.Text);
            int pos = _renderer.Caret;
            string moveStart = pos > 0 ? $"\x1b[{pos}D" : "";
            int postOffset = _renderer.Text.Length - pos;
            string reposition = postOffset > 0 ? $"\x1b[{postOffset}D" : "";

            _renderer.WriteRaw($"\x1b[?25l{moveStart}{highlighted}{reposition}\x1b[?25h");
        }
    }

    internal void HandleEnter()
    {
        ResetAutoComplete();
    }

    public void Backspace()
    {
        _renderer.RemoveLeft(1);
    }

    public void ClearLine()
    {
        _renderer.ClearLineLeft();
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
        if (IsInAutoCompleteMode)
        {
            if (_completions!.Length <= 1)
                return;

            if (_selectionListing.SelectedIndex >= _completions!.Length - 1)
            {
                _selectionListing.SelectedIndex = 0;
            }
            else
            {
                _selectionListing.SelectedIndex++;
            }

            _renderer.CaretVisible = false;
            _renderer.RemoveLeft(_renderer.Caret - _completionStart);
            _renderer.Insert(_completions[_selectionListing.SelectedIndex]);
            _renderer.CaretVisible = true;
            return;
        }

        if (AutoCompleteHandler == null)
            return;

        string text = _renderer.Text;
        _completionStart = AutoCompleteHandler.GetCompletionStart(text, _renderer.Caret);
        _completions = AutoCompleteHandler.GetSuggestions(text, _completionStart, _renderer.Caret);
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

    public void PreviousAutoComplete()
    {
        if (!IsInAutoCompleteMode)
            return;
        

        _selectionListing.SelectedIndex += _selectionListing.SelectedIndex == 0
            ? _completions!.Length - 1
            : -1;

        _renderer.CaretVisible = false;
        _renderer.RemoveLeft(_renderer.Caret - _completionStart);
        _renderer.Insert(_completions![_selectionListing.SelectedIndex]);
        _renderer.CaretVisible = true;
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
    
    public void ResetAutoComplete()
    {
        _completions = null;
        _selectionListing.Clear();
    }

    public void StartAutoComplete()
    {
        _renderer.CaretVisible = false;
        _renderer.RemoveLeft(_renderer.Caret - _completionStart);

        _renderer.Insert(_completions![_selectionListing.SelectedIndex]);
        if (_completions.Length > 1)
        {
            _selectionListing.LoadItems(_completions);
            _selectionListing.SelectedIndex = 0;
        }

        _renderer.CaretVisible = true;
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