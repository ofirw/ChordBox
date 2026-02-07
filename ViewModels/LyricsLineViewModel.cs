namespace ChordBox.ViewModels;

public class LyricsLineViewModel : ViewModelBase
{
    public BarViewModel ParentBar { get; }
    private readonly int _lineIndex;
    private string _text;

    public LyricsLineViewModel(BarViewModel parent, int lineIndex, string text, int totalCount)
    {
        ParentBar = parent;
        _lineIndex = lineIndex;
        _text = text;
        Label = totalCount > 1 ? $"#{lineIndex + 1}" : "";
    }

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
                ParentBar.SetLyricsLine(_lineIndex, value);
        }
    }

    public string Label { get; }
}
