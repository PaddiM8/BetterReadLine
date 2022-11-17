namespace BetterReadLine.Render;
    
internal interface IRenderer
{
    int CursorLeft { get; }
    
    int CursorTop { get; }
    
    int BufferWidth { get; }
    
    int BufferHeight { get; }

    int InputStart { get; }

    int Caret { get; set; }

    bool CaretVisible { get; set; }

    string Text { get; }

    void SetCursorPosition(int left, int top);
    
    void SetBufferSize(int width, int height);

    void ClearLineLeft(int? fromIndex = null);

    void ClearLineRight(int? fromIndex = null);

    void RemoveLeft(int count);

    void RemoveRight(int count);

    void Insert(string text);

    void WriteLinesOutside(string value, int rowCount, int lastLineLength);

    void WriteRaw(string value);
}