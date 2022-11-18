using System;
using System.Collections.Generic;

namespace BetterReadLine.Render;

class CompletionState
{
    public bool IsActive => _completions.Count > 0;

    private readonly IRenderer _renderer;
    private readonly SelectionListing _listing;
    private IList<string> _completions = Array.Empty<string>();
    private int _completionStart;

    public CompletionState(IRenderer renderer)
    {
        _renderer = renderer;
        _listing = new SelectionListing(renderer);
    }

    public void StartNew(IList<string> completions, int completionStart)
    {
        _completions = completions;
        _completionStart = completionStart;
        _listing.Clear();
        _listing.LoadItems(completions);
        _listing.SelectedIndex = 0;
        InsertCompletion();
    }

    public void Reset()
    {
        _completions = Array.Empty<string>();
        _listing.Clear();
    }

    public void Next()
    {
        if (_listing.SelectedIndex >= _completions.Count - 1)
        {
            _listing.SelectedIndex = 0;
        }
        else
        {
            _listing.SelectedIndex++;
        }

        InsertCompletion();
    }

    public void Previous()
    {
        if (_listing.SelectedIndex == 0)
        {
            _listing.SelectedIndex = _completions.Count - 1;
        }
        else
        {
            _listing.SelectedIndex--;
        }

        InsertCompletion();
    }

    private void InsertCompletion()
    {
        _renderer.CaretVisible = false;
        _renderer.RemoveLeft(_renderer.Caret - _completionStart);
        _renderer.Insert(_completions[_listing.SelectedIndex]);
        _renderer.CaretVisible = true;
    }
}