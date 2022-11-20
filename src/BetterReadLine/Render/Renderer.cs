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
            WriteRaw(IndexToMovement(value, out int newTop, out int newLeft));

            _top = newTop;
            _left = newLeft;
            _caret = Math.Max(Math.Min(_text.Length, value), 0);
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

    public string Text
    {
        get => _text.ToString();
        set
        {
            _text.Clear();
            _text.Append(value);
            RenderText();
        }
    }

    private bool IsEndOfLine => Caret >= _text.Length;

    private int LineStartIndex => Math.Max(
        0,
        Text.LastIndexOf('\n', Math.Min(_text.Length - 1, Caret))
    );

    private int LineEndIndex
    {
        get
        {
            int index = Text.IndexOf('\n', Caret);

            return index == -1
                ? _text.Length
                : index;
        }
    }

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

    public void CaretUp()
    {
        int lineStart = LineStartIndex;
        if (lineStart == 0)
        {
            Caret = 0;
            return;
        }

        int newLineStart = Text.LastIndexOf('\n', lineStart - 1);
        int newLineEnd = lineStart - 1;
        Caret = Math.Min(newLineEnd, newLineStart + (Caret - lineStart));
    }
    
    public void CaretDown()
    {
        int lineEnd = LineEndIndex;
        if (lineEnd == _text.Length)
        {
            Caret = _text.Length;
            return;
        }

        int lineStart = Text.LastIndexOf('\n', Math.Max(0, Caret - 1));
        int newLineEnd = Text.IndexOf('\n', lineEnd + 1);
        if (newLineEnd == -1)
            newLineEnd = _text.Length;
        
        Caret = Math.Min(newLineEnd, lineEnd + (Caret - lineStart));
    }

    public void Clear()
    {
        Console.Clear();
    }

    public void ClearLineLeft(int? fromIndex = null)
    {
        int start = LineStartIndex;
        
        // Don't include the new line character
        if (start != 0)
            start++;
        
        _text.Remove(start, Caret - (fromIndex ?? start));
        RenderText();
        Caret = start;
    }

    public void ClearLineRight(int? fromIndex = null)
    {
        if (fromIndex != null)
            Caret = fromIndex.Value;

        int end = LineEndIndex;
        int pos = Caret;
        _text.Remove(Caret, end - Caret);
        RenderText();
        
        // Don't bother putting it at the end since
        // it's already there by default. Moving the
        // caret isn't free and low latency is crucial.
        // In most cases it's already going to be at
        // the end.
        if (pos != _text.Length)
            Caret = pos;
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
        string movementToStart = IndexToMovement(0);
        var (top, left) = IndexToTopLeft(Text.Length);
        string newLine = top > 0 && left == 0 && _text[^1] != '\n'
            ? "\n"
            : "";
        string formattedText = Highlight(Text)
            .Replace("\n", $"\x1b[K\n{new string(' ', InputStart)}");
        WriteRaw($"\x1b[?25l{movementToStart}{formattedText}{newLine}\x1b[?25h\x1b[K");
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
        int top = 0;
        int left = InputStart;
        for (int i = 0; i < index; i++)
        {
            if (_text[i] == '\n')
            {
                top++;
                left = InputStart;
            }
            else if (left == BufferWidth - 1)
            {
                top++;
                left = 0;
            }
            else
            {
                left++;
            }
        }

        return (top, left);
    }

    private string IndexToMovement(int index)
        => IndexToMovement(index, out int _, out int _);

    private string IndexToMovement(int index, out int newTop, out int newLeft)
    {
        index = Math.Max(Math.Min(_text.Length, index), 0);
        (newTop, newLeft) = IndexToTopLeft(index);
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

        return movement.ToString();
    }
}
