using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterReadLine.Render;

class SelectionListing
{
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            Render();
        }
    }

    private readonly IRenderer _renderer;
    private IList<string> _items = Array.Empty<string>();
    private int _maxLength;
    private int _lastRowCount;
    private int _selectedIndex;

    public SelectionListing(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void LoadItems(IList<string> items)
    {
        _items = items;
        _maxLength = items.Max(x => x.Length);
    }

    public void Clear()
    {
        _items = Array.Empty<string>();
        _maxLength = 0;
        _selectedIndex = 0;
        var clearLines = string.Join("\n", Enumerable.Repeat("\x1b[K", _lastRowCount));
        _renderer.WriteLinesOutside(clearLines, _lastRowCount, 0);
    }

    // TODO: (in another class, maybe a new one?) if the input isn't
    //       empty and there are more than 1 options to choose from,
    //       start by writing out the common first characters, then
    //       let the user press tab again to show the selection
    //       if they want to.
    // TODO: if there was just one result, stop the completion thing,
    //       and if the result is a folder, add a slash to the end.
    // TODO: test with different terminal widths
    private void Render()
    {
        if (_items.Count <= 1)
            return;

        const string margin = "   ";
        int columnCount = Math.Min(
            _items.Count,
            _renderer.BufferWidth / (_maxLength + margin.Length)
        );
        columnCount = Math.Min(5, columnCount);

        const int maxRowCount = 5;
        //int rowCount = (int)Math.Ceiling((float)_items.Count / columnCount);
        int startRow = (int)((float)_selectedIndex / columnCount / maxRowCount) * maxRowCount;
        int rowCount = Math.Min(
            maxRowCount,
            (int)Math.Ceiling((float)_items.Count / columnCount - startRow)
        );
        int endRow = startRow + rowCount;

        var columnWidths = new int[columnCount];
        for (int i = startRow; i < endRow; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                int index = i * columnCount + j;
                if (index < _items.Count && _items[index].Length > columnWidths[j])
                    columnWidths[j] = _items[index].Length;
            }
        }

        var output = new StringBuilder();
        for (int i = startRow; i < endRow; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                int index = i * columnCount + j;
                if (index >= _items.Count)
                {
                    output.Append(new string(' ', columnWidths[j]));
                    break;
                }

                if (j != 0)
                    output.Append(margin);

                string content = _items[i * columnCount + j];
                if (content.Length + margin.Length > _renderer.BufferWidth)
                {
                    if (content.Length <= margin.Length + 3)
                        continue;

                    content = content[..(_renderer.BufferWidth - margin.Length - 3)] + "...";
                }

                string padding = new string(' ', columnWidths[j] - content.Length);
                if (index == _selectedIndex)
                    content = $"\x1b[107m\x1b[30m{content}\x1b[0m";

                output.Append($"{content}{padding}\x1b[K");
            }

            if (i < endRow - 1)
                output.AppendLine();
        }

        int lineLength = columnWidths.Sum() + (columnCount - 1) * margin.Length;
        System.IO.File.AppendAllText("/Users/paddi/log.txt", _lastRowCount + " | " + rowCount + "\n");
        if (_lastRowCount > rowCount)
        {
            int difference = _lastRowCount - rowCount;
            var clearLines = string.Join("\n", Enumerable.Repeat("\x1b[K", difference));
            output.Append($"\n{clearLines}");
            rowCount = _lastRowCount;
            lineLength = 0;
        }

        _renderer.WriteLinesOutside(output.ToString(), rowCount, lineLength);
        _lastRowCount = rowCount;
    }
}