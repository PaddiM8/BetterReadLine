using System;
using System.Text;

namespace BetterReadLine.Render;

internal class Renderer : IRenderer
{
    public int CursorLeft => Console.CursorLeft;

    public int CursorTop => Console.CursorTop;

    public int BufferWidth => Console.BufferWidth;

    public int BufferHeight => Console.BufferHeight;

    public int InputStart { get; }

    public bool PasswordMode { get; set; }

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

            Write(movement.ToString());

            _top = newTop;
            _left = newLeft;
            _caret = index;
        }
    }

    public string Text => _text.ToString();

    private bool IsEndOfLine => Caret >= _text.Length - 1;

    private int _top;
    private int _left = Console.CursorLeft;
    private int _caret;
    private readonly StringBuilder _text = new();

    public Renderer()
    {
        InputStart = Console.CursorLeft;
    }

    public void SetBufferSize(int width, int height)
        => Console.SetBufferSize(width, height);

    public void SetCursorPosition(int left, int top)
    {
        if (!PasswordMode)
            Console.SetCursorPosition(left, top);
    }

    public void ClearLineLeft(int? fromIndex = null)
    {
        _text.Remove(0, fromIndex ?? Caret);
        Caret = 0;
        WriteRaw("\x1b[K");
        Write(Text, false);
        Caret = 0;
    }

    public void ClearLineRight(int? fromIndex = null)
    {
        if (fromIndex != null)
            Caret = fromIndex.Value;

        _text.Remove(Caret, _text.Length - Caret);
        WriteRaw("\x1b[K");
    }

    public void Insert(char c)
    {
        if (IsEndOfLine)
        {
            _text.Append(c);
            Write(c.ToString());
        }
        else
        {
            _text.Insert(Caret, c);
            string newEndText = _text.ToString()[Caret..];
            Write($"\x1b[K{newEndText}", true, newEndText.Length);
            Caret -= newEndText.Length - 1;
        }
    }

    public void RemoveLeft(int count)
    {
        if (Caret - count < 0)
            count = Caret;
        if (count == 0)
            return;

        string leftoverText = Text[Caret..];
        Caret -= count;
        ClearLineRight();
        Write(leftoverText, false);
        _text.Append(leftoverText);
    }

    public void RemoveRight(int count)
    {
        if (Caret + count >= _text.Length)
            count = _text.Length - Caret;
        if (count == 0 || Caret == _text.Length)
            return;

        string leftoverText = Text[(Caret + count)..];
        ClearLineRight();
        Write(leftoverText, false);
        _text.Append(leftoverText);
    }

    public void Write(string value, bool moveCaret = true, int? length = null)
    {
        WriteRaw(value);
        SetPositionWithoutMoving(Caret + (length ?? value.Length));

        if (!moveCaret)
            Caret -= length ?? value.Length;
    }

    public void WriteRaw(string value)
    {
        if (PasswordMode)
            value = new string(default, value.Length);

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