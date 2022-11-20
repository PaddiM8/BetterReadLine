using System;
using System.Linq;
using System.Text;

namespace BetterReadLine.Render;

internal class Renderer : IRenderer
{
    public int CursorLeft => Console.CursorLeft;

    public int CursorTop => Console.CursorTop;

    public int BufferWidth => Console.BufferWidth;

    public int BufferHeight => Console.BufferHeight;

    public int InputStart { get; }

    public int Caret
    {
        get
        {
            return _caret;
        }

        set
        {
            int index = Math.Max(Math.Min(_text.Length, value), 0);
            (int newTop, int newLeft) = IndexToTopLeft(index);
            var movement = new StringBuilder();

            int topDiff = newTop - _top;
            if (topDiff > 0)
            {
                movement.Append($"\x1b[{topDiff}B");
            }
            else if (topDiff < 0)
            {
                movement.Append($"\x1b[{Math.Abs(topDiff)}A");
            }

            int leftDiff = newLeft - _left;
            if (leftDiff > 0)
            {
                movement.Append($"\x1b[{leftDiff}C");
            }
            else if (leftDiff < 0)
            {
                movement.Append($"\x1b[{Math.Abs(leftDiff)}D");
            }

            WriteRaw(movement.ToString());

            _top = newTop;
            _left = newLeft;
            _caret = index;
        }
    }

    public bool CaretVisible
    {
        get
        {
            return _caretVisible;
        }

        set
        {
            if (_caretVisible && !value)
                WriteRaw("\x1b[?25l");
            if (!_caretVisible && value)
                WriteRaw("\x1b[?25h");

            _caretVisible = value;
        }
    }

    public string Text => _text.ToString();

    private bool IsEndOfLine => Caret >= _text.Length;

    private int _top;
    private int _left = Console.CursorLeft;
    private int _caret;
    private bool _caretVisible = true;
    private int _previousRenderTop;
    private readonly StringBuilder _text = new();
    private Func<string, string>? _highlighter;

    public Renderer()
    {
        InputStart = Console.CursorLeft;
    }

    public void OnHighlight(Func<string, string>? callback)
        => _highlighter = callback;

    private string Highlight(string input)
    {
        if (input.Length == 0 || _highlighter == null)
            return input;

        return _highlighter(input);
    }

    public void SetBufferSize(int width, int height)
        => Console.SetBufferSize(width, height);

    public void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    public void Clear()
    {
        Console.Clear();
    }

    public void ClearLineLeft(int? fromIndex = null)
    {
        _text.Remove(0, fromIndex ?? Caret);
        RenderText();
        Caret = 0;
    }

    public void ClearLineRight(int? fromIndex = null)
    {
        if (fromIndex != null)
            Caret = fromIndex.Value;

        _text.Remove(Caret, _text.Length - Caret);
        RenderText();
    }

    public void Insert(string input)
    {
        if (IsEndOfLine)
        {
            _text.Append(input);
            RenderText();
        }
        else
        {
            _text.Insert(Caret, input);
            int newPos = Caret + input.Length;
            RenderText();
            Caret = newPos;
        }
    }

    public void RemoveLeft(int count)
    {
        if (Caret - count < 0)
            count = Caret;
        if (count == 0)
            return;

        int newPos = Caret - count;
        _text.Remove(newPos, count);
        RenderText();
        Caret = newPos;
    }

    public void RemoveRight(int count)
    {
        if (Caret + count >= _text.Length)
            count = _text.Length - Caret;
        if (count == 0 || Caret == _text.Length)
            return;

        int newPos = Caret;
        _text.Remove(Caret, count);
        RenderText();
        Caret = newPos;
    }

    private void RenderText()
    {
        CaretVisible = false;
        Caret = 0;
        string newLine = (InputStart + Text.Length) % BufferWidth == 0
            ? newLine = "\n"
            : "";
        WriteRaw($"{Highlight(Text)}{newLine}\x1b[K");
        SetPositionWithoutMoving(Text.Length);

        // If there are leftover lines under, clear them.
        if (_previousRenderTop > _top)
        {
            int diff = _previousRenderTop - _top;
            string clearLines = string.Join(
                "",
                Enumerable.Repeat("\x1b[B\x1b[G\x1b[K", diff)
            );
            WriteRaw($"{clearLines}\x1b[{diff}A\x1b[{_left}C");
        }

        CaretVisible = true;
        _previousRenderTop = _top;
    }

    public void WriteLinesOutside(string value, int rowCount, int lastLineLength)
    {
        int offset = lastLineLength - _left;
        string horizontalMovement = "";
        if (offset < 0)
            horizontalMovement = $"{Math.Abs(offset)}C";
        else if (offset > 0)
            horizontalMovement = $"{offset}D";

        CaretVisible = false;
        WriteRaw($"\n\x1b[K{value}\x1b[{horizontalMovement}\x1b[{rowCount}A");
        CaretVisible = true;
    }

    public void WriteRaw(string value)
    {
        Console.Write(value);
    }

    private void SetPositionWithoutMoving(int index)
    {
        (int top, int left) = IndexToTopLeft(index);
        _top = top;
        _left = left;
        _caret = index;
    }

    private (int, int) IndexToTopLeft(int index)
    {
        int top = index + InputStart < BufferWidth
            ? 0
            : (index + InputStart) / BufferWidth;

        int left = index + InputStart;
        if (top > 0)
            left = index + InputStart - top * BufferWidth;

        return (top, left);
    }
}
