using System.Collections.ObjectModel;
using System.Windows.Media;
using ChordBox.Models;

namespace ChordBox.ViewModels;

public class BarViewModel : ViewModelBase
{
    private static readonly SolidColorBrush[] LoopColors =
    [
        new(Color.FromRgb(0x4e, 0xc9, 0xb0)),  // teal
        new(Color.FromRgb(0xdc, 0xdc, 0x6e)),  // yellow
        new(Color.FromRgb(0xc5, 0x86, 0xc0)),  // purple
        new(Color.FromRgb(0x56, 0x9c, 0xd6)),  // blue
    ];

    private readonly Bar _bar;
    private bool _isPlaying;
    private int _currentBeat = -1;
    private bool _isSelected;
    private bool _isLoopStart;
    private bool _isLoopEnd;
    private bool _isInLoop;
    private int _loopRepeatCount;
    private int _loopColorIndex = -1;
    private string _loopName = "";
    private int _effectiveBeats = 4;
    private string _lyrics = "";
    private string _inlineText = "";
    private int _editBeatIndex = -1; // -1 = all beats, 0+ = specific beat
    private bool _isEditing;
    private int _totalPlayCount = 1;

    public BarViewModel(Bar bar, int index)
    {
        _bar = bar;
        Index = index;
    }

    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BarNumber));
            }
        }
    }
    public int BarNumber => Index + 1;
    public Bar Model => _bar;

    public int EffectiveBeats
    {
        get => _effectiveBeats;
        set
        {
            if (SetProperty(ref _effectiveBeats, value))
            {
                OnPropertyChanged(nameof(TimeSigDisplay));
                OnPropertyChanged(nameof(ShowBeat3));
                OnPropertyChanged(nameof(ShowBeat4));
                OnPropertyChanged(nameof(ShowBeat5));
                OnPropertyChanged(nameof(HasOverride));
            }
        }
    }

    public bool HasOverride => _bar.BeatsPerBarOverride > 0;
    public string TimeSigDisplay => $"{_effectiveBeats}/4";
    public bool ShowBeat3 => _effectiveBeats >= 3;
    public bool ShowBeat4 => _effectiveBeats >= 4;
    public bool ShowBeat5 => _effectiveBeats >= 5;

    public bool HasMultipleChords => _bar.HasMultipleChords;

    public string ChordDisplay
    {
        get
        {
            if (_bar.IsEmpty) return "—";
            if (_bar.HasMultipleChords) return "";
            return _bar.SingleChord?.DisplayName ?? "—";
        }
    }

    public bool ShowPerBeatRow => _bar.HasMultipleChords;

    public string Beat1Chord => _bar.GetChordAtBeat(0)?.DisplayName ?? "·";
    public string Beat2Chord => CollapsedBeatChord(1);
    public string Beat3Chord => CollapsedBeatChord(2);
    public string Beat4Chord => CollapsedBeatChord(3);
    public string Beat5Chord => CollapsedBeatChord(4);

    private string CollapsedBeatChord(int beat)
    {
        var cur = _bar.GetChordAtBeat(beat);
        var prev = _bar.GetChordAtBeat(beat - 1);
        if (cur == null) return "·";
        if (prev != null && cur.Equals(prev)) return "";
        return cur.DisplayName;
    }

    public bool HasChord => !_bar.IsEmpty;

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(CardBackground));
                OnPropertyChanged(nameof(CardBorder));
                if (!value) CurrentBeat = -1;
            }
        }
    }

    public int CurrentBeat
    {
        get => _currentBeat;
        set
        {
            if (SetProperty(ref _currentBeat, value))
            {
                OnPropertyChanged(nameof(Beat1Brush));
                OnPropertyChanged(nameof(Beat2Brush));
                OnPropertyChanged(nameof(Beat3Brush));
                OnPropertyChanged(nameof(Beat4Brush));
                OnPropertyChanged(nameof(Beat5Brush));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                OnPropertyChanged(nameof(CardBorder));
        }
    }

    // Loop properties
    public bool IsLoopStart
    {
        get => _isLoopStart;
        set { if (SetProperty(ref _isLoopStart, value)) OnPropertyChanged(nameof(LoopStartVisibility)); }
    }

    public bool IsLoopEnd
    {
        get => _isLoopEnd;
        set { if (SetProperty(ref _isLoopEnd, value)) OnPropertyChanged(nameof(LoopEndVisibility)); }
    }

    public bool IsInLoop
    {
        get => _isInLoop;
        set { if (SetProperty(ref _isInLoop, value)) OnPropertyChanged(nameof(LoopTopBrush)); }
    }

    public int LoopRepeatCount
    {
        get => _loopRepeatCount;
        set { if (SetProperty(ref _loopRepeatCount, value)) OnPropertyChanged(nameof(LoopRepeatText)); }
    }

    public int LoopColorIndex
    {
        get => _loopColorIndex;
        set
        {
            if (SetProperty(ref _loopColorIndex, value))
            {
                OnPropertyChanged(nameof(LoopTopBrush));
                OnPropertyChanged(nameof(LoopStartVisibility));
                OnPropertyChanged(nameof(LoopEndVisibility));
            }
        }
    }

    public string LoopName
    {
        get => _loopName;
        set { if (SetProperty(ref _loopName, value)) OnPropertyChanged(nameof(LoopNameDisplay)); }
    }

    public string LoopNameDisplay => !string.IsNullOrWhiteSpace(_loopName) ? _loopName : "";
    public string LoopRepeatText => _loopRepeatCount > 0 ? $"×{_loopRepeatCount}" : "";
    public string LoopStartVisibility => _isLoopStart ? "Visible" : "Collapsed";
    public string LoopEndVisibility => _isLoopEnd ? "Visible" : "Collapsed";

    public SolidColorBrush LoopTopBrush =>
        _isInLoop && _loopColorIndex >= 0
            ? LoopColors[_loopColorIndex % LoopColors.Length]
            : Brushes.Transparent;

    public void SetChord(Chord? chord)
    {
        _bar.SetSingleChord(chord);
        RefreshChordDisplay();
    }

    public void SetChordAtBeat(int beat, Chord? chord)
    {
        _bar.SetChordAtBeat(beat, chord);
        RefreshChordDisplay();
    }

    public void ClearAllChords()
    {
        _bar.ChordEvents.Clear();
        RefreshChordDisplay();
    }

    public void RefreshChordDisplay()
    {
        OnPropertyChanged(nameof(ChordDisplay));
        OnPropertyChanged(nameof(HasChord));
        OnPropertyChanged(nameof(HasMultipleChords));
        OnPropertyChanged(nameof(ShowPerBeatRow));
        OnPropertyChanged(nameof(Beat1Chord));
        OnPropertyChanged(nameof(Beat2Chord));
        OnPropertyChanged(nameof(Beat3Chord));
        OnPropertyChanged(nameof(Beat4Chord));
        OnPropertyChanged(nameof(Beat5Chord));
    }

    public ObservableCollection<LoopLayerInfo> LoopLayers { get; } = new();

    public string Lyrics
    {
        get => _lyrics;
        set
        {
            if (SetProperty(ref _lyrics, value))
                OnPropertyChanged(nameof(LyricsFontSize));
        }
    }

    public bool HasLyrics => !string.IsNullOrWhiteSpace(_lyrics);

    public double LyricsFontSize
    {
        get
        {
            const double baseFontSize = 11.0;
            const double minFontSize = 7.0;
            const int maxCharsAtBase = 22;
            // Use the longest line for sizing
            int maxLen = LyricsLines.Max(l => l.Text.Length);
            if (maxLen <= maxCharsAtBase)
                return baseFontSize;
            return Math.Max(minFontSize, baseFontSize * maxCharsAtBase / maxLen);
        }
    }

    public int TotalPlayCount
    {
        get => _totalPlayCount;
        set
        {
            if (SetProperty(ref _totalPlayCount, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(LyricsLines));
                OnPropertyChanged(nameof(ShowMultipleLyrics));
                OnPropertyChanged(nameof(LyricsFontSize));
            }
        }
    }

    public bool ShowMultipleLyrics => _totalPlayCount > 1;

    public List<LyricsLineViewModel> LyricsLines
    {
        get
        {
            var lines = (_lyrics ?? "").Split('\n');
            var result = new List<LyricsLineViewModel>();
            for (int i = 0; i < _totalPlayCount; i++)
            {
                string text = i < lines.Length ? lines[i] : "";
                result.Add(new LyricsLineViewModel(this, i, text, _totalPlayCount));
            }
            return result;
        }
    }

    public void SetLyricsLine(int index, string value)
    {
        var lines = (_lyrics ?? "").Split('\n').ToList();
        while (lines.Count <= index) lines.Add("");
        lines[index] = value;
        // Trim trailing empty lines
        while (lines.Count > 1 && string.IsNullOrEmpty(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        _lyrics = string.Join("\n", lines);
        OnPropertyChanged(nameof(LyricsFontSize));
    }

    // ─── Inline editing ───
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(CardBackground));
                OnPropertyChanged(nameof(EditingIndicator));
                RefreshBeatHighlights();
            }
        }
    }

    public int EditBeatIndex
    {
        get => _editBeatIndex;
        set
        {
            if (SetProperty(ref _editBeatIndex, value))
            {
                OnPropertyChanged(nameof(EditingIndicator));
                RefreshBeatHighlights();
            }
        }
    }

    public string InlineText
    {
        get => _inlineText;
        set
        {
            if (SetProperty(ref _inlineText, value))
                OnPropertyChanged(nameof(EditingIndicator));
        }
    }

    public string EditingIndicator =>
        _isEditing
            ? (_inlineText.Length > 0
                ? _inlineText
                : (_editBeatIndex < 0 ? "All beats: type chord..." : $"Beat {_editBeatIndex + 1}: type chord..."))
            : "";

    public SolidColorBrush EditBeat1Brush => EditBeatBrush(0);
    public SolidColorBrush EditBeat2Brush => EditBeatBrush(1);
    public SolidColorBrush EditBeat3Brush => EditBeatBrush(2);
    public SolidColorBrush EditBeat4Brush => EditBeatBrush(3);
    public SolidColorBrush EditBeat5Brush => EditBeatBrush(4);

    private SolidColorBrush EditBeatBrush(int beat) =>
        _isEditing && _editBeatIndex >= 0 && _editBeatIndex == beat
            ? new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xff))
            : Brushes.Transparent;

    private void RefreshBeatHighlights()
    {
        OnPropertyChanged(nameof(EditBeat1Brush));
        OnPropertyChanged(nameof(EditBeat2Brush));
        OnPropertyChanged(nameof(EditBeat3Brush));
        OnPropertyChanged(nameof(EditBeat4Brush));
        OnPropertyChanged(nameof(EditBeat5Brush));
    }

    public void SyncLyricsToModel()
    {
        _bar.Lyrics = _lyrics;
    }

    public void ClearLoopMarkers()
    {
        IsLoopStart = false;
        IsLoopEnd = false;
        IsInLoop = false;
        LoopRepeatCount = 0;
        LoopColorIndex = -1;
        LoopName = "";
        LoopLayers.Clear();
    }

    // Visual properties
    public SolidColorBrush CardBackground => IsPlaying
        ? new SolidColorBrush(Color.FromRgb(0x1a, 0x3a, 0x5c))
        : _isEditing
            ? new SolidColorBrush(Color.FromRgb(0x1a, 0x2a, 0x4e))
            : new SolidColorBrush(Color.FromRgb(0x16, 0x21, 0x3e));

    public SolidColorBrush CardBorder => IsSelected
        ? new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xff))
        : IsPlaying
            ? new SolidColorBrush(Color.FromRgb(0xe9, 0x45, 0x60))
            : new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x4a));

    public SolidColorBrush Beat1Brush => BeatBrush(0);
    public SolidColorBrush Beat2Brush => BeatBrush(1);
    public SolidColorBrush Beat3Brush => BeatBrush(2);
    public SolidColorBrush Beat4Brush => BeatBrush(3);
    public SolidColorBrush Beat5Brush => BeatBrush(4);

    private SolidColorBrush BeatBrush(int beat) =>
        _currentBeat == beat
            ? new SolidColorBrush(Color.FromRgb(0xe9, 0x45, 0x60))
            : new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x4a));
}
