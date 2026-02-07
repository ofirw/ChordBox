namespace ChordBox.ViewModels;

/// <summary>
/// Generic undo/redo manager that stores snapshots of state.
/// </summary>
public class UndoManager<T> where T : class
{
    private readonly Stack<T> _undoStack = new();
    private readonly Stack<T> _redoStack = new();
    private readonly int _maxHistory;

    public UndoManager(int maxHistory = 100)
    {
        _maxHistory = maxHistory;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Save current state before a change. Call this BEFORE modifying the model.
    /// </summary>
    public void SaveState(T state)
    {
        _undoStack.Push(state);
        _redoStack.Clear();

        // Trim oldest entries if over limit
        if (_undoStack.Count > _maxHistory)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = Math.Min(items.Length - 1, _maxHistory - 1); i >= 0; i--)
                _undoStack.Push(items[i]);
        }
    }

    /// <summary>
    /// Undo: returns the previous state and pushes current state onto redo stack.
    /// </summary>
    public T? Undo(T currentState)
    {
        if (_undoStack.Count == 0) return null;
        _redoStack.Push(currentState);
        return _undoStack.Pop();
    }

    /// <summary>
    /// Redo: returns the next state and pushes current state onto undo stack.
    /// </summary>
    public T? Redo(T currentState)
    {
        if (_redoStack.Count == 0) return null;
        _undoStack.Push(currentState);
        return _redoStack.Pop();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
