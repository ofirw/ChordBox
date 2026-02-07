using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ChordBox.Audio;
using ChordBox.Models;

namespace ChordBox.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly MidiChordPlayer _player = new();
    private int _tempo = 120;
    private bool _isPlaying;
    private bool _isLooping = true;
    private bool _isChordPickerOpen;
    private BarViewModel? _selectedBar;
    private NoteName _pickerRoot = NoteName.C;
    private string _statusText = "Ready — click a bar to set a chord, then press Play";
    private int _currentBarIndex = -1;
    private PlayStyle _selectedStyle;
    private Instrument _selectedInstrument;
    private int _pickerTargetBeat = -1;
    private int? _pendingLoopStart;
    private bool _isSettingLoopStart;
    private bool _isSettingLoopEnd;
    private string _songTitle = "Untitled";
    private bool _isLoopEditorOpen;
    private LoopRegion? _editingLoop;
    private bool _isEditingExistingLoop;
    private string _editLoopName = "";
    private int _editLoopRepeat = 2;
    private int _editLoopStartBar = 1;
    private int _editLoopEndBar = 1;
    private string? _currentFilePath;
    private int _globalBeatsPerBar = 4;
    private bool _showLyrics;
    private int _pickerBarTimeSig;
    private bool _isComposingHelpOpen;
    private NoteName _composingKey = NoteName.C;
    private ScaleType _composingScale = ScaleType.Major;
    private readonly UndoManager<SongSnapshot> _undoManager = new();
    private string _soundFontStatus = "Windows MIDI (default)";
    private bool _isSoundFontLoaded;
    private BarSnapshot? _clipboardBar;
    private bool _composingKeyManuallySet;
    private List<string> _recentSoundFonts = new();
    private DateTime _lastTapTime = DateTime.MinValue;
    private readonly List<double> _tapIntervals = new();
    private StrumPattern _selectedStrumPattern = StrumPattern.EighthDownUp;
    private bool _isStrumPatternEditorOpen;

    public MainViewModel()
    {
        _selectedStyle = PlayStyle.Pop;
        _selectedInstrument = Instrument.AcousticPiano;
        _selectedStrumPattern = StrumPattern.EighthDownUp;

        for (int i = 0; i < 8; i++)
            Bars.Add(new BarViewModel(new Bar(), i));

        Bars[0].SetChord(new Chord(NoteName.C, ChordQuality.Major));
        Bars[1].SetChord(new Chord(NoteName.A, ChordQuality.Minor));
        Bars[2].SetChord(new Chord(NoteName.F, ChordQuality.Major));
        Bars[3].SetChord(new Chord(NoteName.G, ChordQuality.Dominant7));
        Bars[4].SetChord(new Chord(NoteName.C, ChordQuality.Major));
        Bars[5].SetChord(new Chord(NoteName.E, ChordQuality.Minor));
        Bars[6].SetChord(new Chord(NoteName.A, ChordQuality.Minor));
        Bars[7].SetChord(new Chord(NoteName.G, ChordQuality.Major));

        PlayCommand = new RelayCommand(DoPlay, () => !IsPlaying);
        StopCommand = new RelayCommand(DoStop, () => IsPlaying);
        AddBarCommand = new RelayCommand(DoAddBar);
        RemoveBarCommand = new RelayCommand(DoRemoveBar, () => Bars.Count > 1);
        DeleteBarCommand = new RelayCommand(DoDeleteBar, _ => Bars.Count > 1);
        CopyBarCommand = new RelayCommand(DoCopyBar, () => SelectedBar != null);
        PasteBarCommand = new RelayCommand(DoPasteBar, () => SelectedBar != null && _clipboardBar != null);
        SelectBarCommand = new RelayCommand(DoSelectBar);
        SelectRootCommand = new RelayCommand(DoSelectRoot);
        SelectQualityCommand = new RelayCommand(DoSelectQuality);
        ClearChordCommand = new RelayCommand(DoClearChord);
        ClosePickerCommand = new RelayCommand(DoClosePicker);
        SetPickerBeatCommand = new RelayCommand(DoSetPickerBeat);
        SetLoopStartCommand = new RelayCommand(DoSetLoopStart, () => !IsPlaying);
        SetLoopEndCommand = new RelayCommand(DoSetLoopEnd, () => !IsPlaying && _pendingLoopStart.HasValue);
        RemoveLoopCommand = new RelayCommand(DoRemoveLoop, _ => !IsPlaying);
        RemoveLoopByIndexCommand = new RelayCommand(DoRemoveLoopByIndex);
        EditLoopByIndexCommand = new RelayCommand(DoEditLoopByIndex);
        IncreaseLoopRepeatCommand = new RelayCommand(DoIncreaseLoopRepeat);
        DecreaseLoopRepeatCommand = new RelayCommand(DoDecreaseLoopRepeat);
        SaveCommand = new RelayCommand(DoSave);
        SaveAsCommand = new RelayCommand(DoSaveAs);
        OpenCommand = new RelayCommand(DoOpen);
        NewCommand = new RelayCommand(DoNew);
        ConfirmLoopEditCommand = new RelayCommand(DoConfirmLoopEdit);
        CancelLoopEditCommand = new RelayCommand(DoCancelLoopEdit);
        EditLoopCommand = new RelayCommand(DoEditLoop);
        PlayFromBarCommand = new RelayCommand(DoPlayFromBar);
        ToggleLyricsCommand = new RelayCommand(() => ShowLyrics = !ShowLyrics);
        OpenBarSettingsCommand = new RelayCommand(DoOpenBarSettings);
        ToggleComposingHelpCommand = new RelayCommand(() => {
            IsComposingHelpOpen = !IsComposingHelpOpen;
            if (IsComposingHelpOpen && !_composingKeyManuallySet) AutoDetectKey();
        });
        PlaySingleChordCommand = new RelayCommand(DoPlaySingleChord);
        UndoCommand = new RelayCommand(DoUndo, () => _undoManager.CanUndo);
        RedoCommand = new RelayCommand(DoRedo, () => _undoManager.CanRedo);
        LoadSoundFontCommand = new RelayCommand(DoLoadSoundFont);
        UnloadSoundFontCommand = new RelayCommand(DoUnloadSoundFont, () => _isSoundFontLoaded);
        LoadRecentSoundFontCommand = new RelayCommand(DoLoadRecentSoundFont);
        TapTempoCommand = new RelayCommand(DoTapTempo);
        OpenStrumEditorCommand = new RelayCommand(() => IsStrumPatternEditorOpen = !IsStrumPatternEditorOpen);
        AddStrumEventCommand = new RelayCommand(DoAddStrumEvent);
        RemoveStrumEventCommand = new RelayCommand(DoRemoveStrumEvent);

        _player.BeatChanged += OnBeatChanged;
        _player.PlaybackStopped += OnPlaybackStopped;

        RootNotes = Enum.GetValues<NoteName>();
        ChordQualities = Enum.GetValues<ChordQuality>();
        Styles = new ObservableCollection<PlayStyle>(PlayStyle.AllStyles);
        Instruments = new ObservableCollection<Instrument>(Instrument.AllInstruments);

        LoadAppConfig();
        RefreshAllBarBeats();
    }

    // Collections
    public ObservableCollection<BarViewModel> Bars { get; } = new();
    public ObservableCollection<PlayStyle> Styles { get; }
    public ObservableCollection<Instrument> Instruments { get; }
    public ObservableCollection<StrumPattern> StrumPatterns { get; } = new(StrumPattern.AllPatterns);
    public List<LoopRegion> LoopRegions { get; } = new();
    public ObservableCollection<LoopRegion> LoopRegionsView { get; } = new();

    // Song title
    public string SongTitle
    {
        get => _songTitle;
        set => SetProperty(ref _songTitle, value);
    }

    public PlayStyle SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (SetProperty(ref _selectedStyle, value) && value != null)
            {
                StatusText = $"Style: {value.Name}";
                if (_isPlaying) _player.UpdateStyle(value);
            }
        }
    }

    public Instrument SelectedInstrument
    {
        get => _selectedInstrument;
        set
        {
            if (SetProperty(ref _selectedInstrument, value) && value != null)
            {
                StatusText = $"Instrument: {value.Name}";
                if (_isPlaying) _player.UpdateInstrument(value);
                OnPropertyChanged(nameof(PowerChordIndicator));
                OnPropertyChanged(nameof(IsStrummedInstrument));
            }
        }
    }

    public bool IsStrummedInstrument => _selectedInstrument?.IsStrummed == true;

    public StrumPattern SelectedStrumPattern
    {
        get => _selectedStrumPattern;
        set
        {
            if (SetProperty(ref _selectedStrumPattern, value) && value != null)
            {
                StatusText = $"Strum: {value.Name}";
                if (_isPlaying) _player.UpdateStrumPattern(value);
                SyncStrumPatternEvents();
                OnPropertyChanged(nameof(StrumPatternSummary));
                OnPropertyChanged(nameof(CustomStrumName));
            }
        }
    }

    public bool IsStrumPatternEditorOpen
    {
        get => _isStrumPatternEditorOpen;
        set
        {
            if (SetProperty(ref _isStrumPatternEditorOpen, value) && value)
                SyncStrumPatternEvents();
        }
    }

    public ObservableCollection<StrumEvent> StrumPatternEvents { get; } = new();

    public string StrumPatternSummary =>
        _selectedStrumPattern != null
            ? string.Join(" ", _selectedStrumPattern.Events.Select(e => e.DisplayLabel))
              + $" ({_selectedStrumPattern.TotalBeats} beats)"
            : "";

    public StrumType[] StrumTypeValues { get; } = Enum.GetValues<StrumType>();
    public StrumArticulation[] StrumArticulationValues { get; } = Enum.GetValues<StrumArticulation>();
    public double[] StrumDurationValues { get; } = [1.0, 0.5, 0.25];

    public string CustomStrumName
    {
        get => _selectedStrumPattern?.Name ?? "";
        set
        {
            if (_selectedStrumPattern != null)
            {
                EnsureCustomStrumPattern();
                _selectedStrumPattern.Name = value;
                OnPropertyChanged();
                // Update the ComboBox display
                var idx = StrumPatterns.IndexOf(_selectedStrumPattern);
                if (idx >= 0)
                {
                    StrumPatterns[idx] = _selectedStrumPattern;
                    SelectedStrumPattern = _selectedStrumPattern;
                }
            }
        }
    }

    public void EnsureCustomStrumPattern()
    {
        if (_selectedStrumPattern == null) return;
        if (!_selectedStrumPattern.IsCustom)
        {
            var custom = _selectedStrumPattern.Clone();
            custom.Name = "Custom";
            StrumPatterns.Add(custom);
            SelectedStrumPattern = custom;
        }
    }

    private void SyncStrumPatternEvents()
    {
        StrumPatternEvents.Clear();
        if (_selectedStrumPattern != null)
            foreach (var e in _selectedStrumPattern.Events)
                StrumPatternEvents.Add(e);
    }

    public void RefreshStrumSummary()
    {
        OnPropertyChanged(nameof(StrumPatternSummary));
        OnPropertyChanged(nameof(CustomStrumName));
    }

    public void RefreshStrumEditor()
    {
        SyncStrumPatternEvents();
        OnPropertyChanged(nameof(StrumPatternSummary));
        OnPropertyChanged(nameof(CustomStrumName));
    }

    public int GlobalBeatsPerBar
    {
        get => _globalBeatsPerBar;
        set
        {
            if (SetProperty(ref _globalBeatsPerBar, Math.Clamp(value, 2, 7)))
            {
                RefreshAllBarBeats();
                if (_isPlaying) _player.UpdateGlobalBeatsPerBar(_globalBeatsPerBar);
            }
        }
    }

    public int Tempo
    {
        get => _tempo;
        set
        {
            if (SetProperty(ref _tempo, Math.Clamp(value, 40, 300)))
            {
                if (_isPlaying) _player.UpdateTempo(_tempo);
            }
        }
    }

    private void DoTapTempo()
    {
        var now = DateTime.UtcNow;
        double elapsed = (now - _lastTapTime).TotalMilliseconds;
        _lastTapTime = now;

        if (elapsed > 2000) // reset if more than 2 seconds between taps
        {
            _tapIntervals.Clear();
            StatusText = "TAP — tap again...";
            return;
        }

        _tapIntervals.Add(elapsed);
        if (_tapIntervals.Count > 8)
            _tapIntervals.RemoveAt(0); // keep last 8 intervals

        double avgMs = _tapIntervals.Average();
        int bpm = (int)Math.Round(60000.0 / avgMs);
        Tempo = Math.Clamp(bpm, 40, 300);
        StatusText = $"TAP tempo: {Tempo} BPM ({_tapIntervals.Count} taps)";
    }

    private void DoAddStrumEvent(object? param)
    {
        EnsureCustomStrumPattern();
        _selectedStrumPattern.Events.Add(new StrumEvent(StrumType.Down, 0.5, StrumArticulation.Ring));
        RefreshStrumEditor();
    }

    private void DoRemoveStrumEvent(object? param)
    {
        if (_selectedStrumPattern.Events.Count <= 1) return;
        EnsureCustomStrumPattern();
        int idx = param is int i ? i : _selectedStrumPattern.Events.Count - 1;
        if (idx >= 0 && idx < _selectedStrumPattern.Events.Count)
            _selectedStrumPattern.Events.RemoveAt(idx);
        RefreshStrumEditor();
    }

    public void UpdateStrumEvent(int index, StrumType? type = null, double? duration = null, StrumArticulation? articulation = null)
    {
        if (index < 0 || index >= _selectedStrumPattern.Events.Count) return;
        EnsureCustomStrumPattern();
        var ev = _selectedStrumPattern.Events[index];
        if (type.HasValue) ev.Type = type.Value;
        if (duration.HasValue) ev.DurationInBeats = duration.Value;
        if (articulation.HasValue) ev.Articulation = articulation.Value;
        RefreshStrumEditor();
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (SetProperty(ref _isPlaying, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            if (SetProperty(ref _isLooping, value))
            {
                if (_isPlaying) _player.UpdateGlobalLoop(value);
            }
        }
    }

    public bool IsChordPickerOpen
    {
        get => _isChordPickerOpen;
        set => SetProperty(ref _isChordPickerOpen, value);
    }

    public BarViewModel? SelectedBar
    {
        get => _selectedBar;
        set
        {
            if (_selectedBar != null) _selectedBar.IsSelected = false;
            SetProperty(ref _selectedBar, value);
            if (_selectedBar != null) _selectedBar.IsSelected = true;
        }
    }

    public NoteName PickerRoot
    {
        get => _pickerRoot;
        set
        {
            if (SetProperty(ref _pickerRoot, value))
                OnPropertyChanged(nameof(PickerPreview));
        }
    }

    public int PickerTargetBeat
    {
        get => _pickerTargetBeat;
        set
        {
            if (SetProperty(ref _pickerTargetBeat, value))
            {
                OnPropertyChanged(nameof(PickerBeatLabel));
                OnPropertyChanged(nameof(IsPickerAllBeats));
                OnPropertyChanged(nameof(IsPickerBeat1));
                OnPropertyChanged(nameof(IsPickerBeat2));
                OnPropertyChanged(nameof(IsPickerBeat3));
                OnPropertyChanged(nameof(IsPickerBeat4));
                OnPropertyChanged(nameof(IsPickerBeat5));
            }
        }
    }

    public string PickerBeatLabel => _pickerTargetBeat < 0 ? "All Beats" : $"Beat {_pickerTargetBeat + 1}";
    public bool IsPickerAllBeats => _pickerTargetBeat == -1;
    public bool IsPickerBeat1 => _pickerTargetBeat == 0;
    public bool IsPickerBeat2 => _pickerTargetBeat == 1;
    public bool IsPickerBeat3 => _pickerTargetBeat == 2;
    public bool IsPickerBeat4 => _pickerTargetBeat == 3;
    public bool IsPickerBeat5 => _pickerTargetBeat == 4;

    public string PickerPreview => _pickerRoot.ToDisplayString();

    public string PowerChordIndicator =>
        _selectedInstrument?.UsePowerChords == true ? "⚡ Power Chords" : "";

    public bool ShowLyrics
    {
        get => _showLyrics;
        set => SetProperty(ref _showLyrics, value);
    }

    public int PickerBarTimeSig
    {
        get => _pickerBarTimeSig;
        set
        {
            if (SetProperty(ref _pickerBarTimeSig, value) && SelectedBar != null)
            {
                SelectedBar.Model.BeatsPerBarOverride = value;
                SelectedBar.EffectiveBeats = SelectedBar.Model.GetEffectiveBeatsPerBar(_globalBeatsPerBar);
                SelectedBar.RefreshChordDisplay();
                NotifyPickerBeats();
            }
        }
    }

    public int PickerEffectiveBeats => SelectedBar?.EffectiveBeats ?? _globalBeatsPerBar;
    public bool PickerShowBeat3 => PickerEffectiveBeats >= 3;
    public bool PickerShowBeat4 => PickerEffectiveBeats >= 4;
    public bool PickerShowBeat5 => PickerEffectiveBeats >= 5;

    private void NotifyPickerBeats()
    {
        OnPropertyChanged(nameof(PickerEffectiveBeats));
        OnPropertyChanged(nameof(PickerShowBeat3));
        OnPropertyChanged(nameof(PickerShowBeat4));
        OnPropertyChanged(nameof(PickerShowBeat5));
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsSettingLoopStart
    {
        get => _isSettingLoopStart;
        set => SetProperty(ref _isSettingLoopStart, value);
    }

    public bool IsSettingLoopEnd
    {
        get => _isSettingLoopEnd;
        set => SetProperty(ref _isSettingLoopEnd, value);
    }

    // Loop editor overlay
    public bool IsLoopEditorOpen
    {
        get => _isLoopEditorOpen;
        set => SetProperty(ref _isLoopEditorOpen, value);
    }

    public string EditLoopName
    {
        get => _editLoopName;
        set => SetProperty(ref _editLoopName, value);
    }

    public int EditLoopRepeat
    {
        get => _editLoopRepeat;
        set => SetProperty(ref _editLoopRepeat, Math.Clamp(value, 1, 99));
    }

    private SectionType _editLoopSectionType;
    public SectionType EditLoopSectionType
    {
        get => _editLoopSectionType;
        set => SetProperty(ref _editLoopSectionType, value);
    }

    public SectionType[] AllSectionTypes => Enum.GetValues<SectionType>();

    public int EditLoopStartBar
    {
        get => _editLoopStartBar;
        set => SetProperty(ref _editLoopStartBar, Math.Clamp(value, 1, Bars.Count));
    }

    public int EditLoopEndBar
    {
        get => _editLoopEndBar;
        set => SetProperty(ref _editLoopEndBar, Math.Clamp(value, 1, Bars.Count));
    }

    public NoteName[] RootNotes { get; }
    public ChordQuality[] ChordQualities { get; }

    // Commands
    public ICommand PlayCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand AddBarCommand { get; }
    public ICommand RemoveBarCommand { get; }
    public ICommand SelectBarCommand { get; }
    public ICommand SelectRootCommand { get; }
    public ICommand SelectQualityCommand { get; }
    public ICommand ClearChordCommand { get; }
    public ICommand ClosePickerCommand { get; }
    public ICommand SetPickerBeatCommand { get; }
    public ICommand SetLoopStartCommand { get; }
    public ICommand SetLoopEndCommand { get; }
    public ICommand RemoveLoopCommand { get; }
    public ICommand RemoveLoopByIndexCommand { get; }
    public ICommand EditLoopByIndexCommand { get; }
    public ICommand IncreaseLoopRepeatCommand { get; }
    public ICommand DecreaseLoopRepeatCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand ConfirmLoopEditCommand { get; }
    public ICommand CancelLoopEditCommand { get; }
    public ICommand EditLoopCommand { get; }
    public ICommand PlayFromBarCommand { get; }
    public ICommand ToggleLyricsCommand { get; }
    public ICommand OpenBarSettingsCommand { get; }
    public ICommand ToggleComposingHelpCommand { get; }
    public ICommand PlaySingleChordCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand LoadSoundFontCommand { get; }
    public ICommand UnloadSoundFontCommand { get; }
    public ICommand DeleteBarCommand { get; }
    public ICommand CopyBarCommand { get; }
    public ICommand PasteBarCommand { get; }
    public ICommand LoadRecentSoundFontCommand { get; }
    public ICommand TapTempoCommand { get; }
    public ICommand OpenStrumEditorCommand { get; }
    public ICommand AddStrumEventCommand { get; }
    public ICommand RemoveStrumEventCommand { get; }

    // ─── SoundFont ───
    public string SoundFontStatus
    {
        get => _soundFontStatus;
        set => SetProperty(ref _soundFontStatus, value);
    }

    public bool IsSoundFontLoaded
    {
        get => _isSoundFontLoaded;
        set
        {
            if (SetProperty(ref _isSoundFontLoaded, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public ObservableCollection<string> RecentSoundFonts { get; } = new();

    private void AddToRecentSoundFonts(string path)
    {
        _recentSoundFonts.Remove(path);
        _recentSoundFonts.Insert(0, path);
        if (_recentSoundFonts.Count > 5) _recentSoundFonts.RemoveAt(5);
        RecentSoundFonts.Clear();
        foreach (var p in _recentSoundFonts) RecentSoundFonts.Add(p);
        SaveAppConfig();
    }

    private void LoadSoundFontFromPath(string path)
    {
        if (_player.LoadSoundFont(path))
        {
            IsSoundFontLoaded = true;
            SoundFontStatus = $"SF2: {System.IO.Path.GetFileName(path)}";
            StatusText = $"SoundFont loaded: {System.IO.Path.GetFileName(path)}";
            AddToRecentSoundFonts(path);
        }
        else
        {
            SoundFontStatus = "Failed to load SoundFont";
            StatusText = "Error loading SoundFont file";
        }
    }

    private void DoLoadSoundFont()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "SoundFont files (*.sf2)|*.sf2|All files (*.*)|*.*",
            Title = "Load SoundFont"
        };
        if (dlg.ShowDialog() == true)
            LoadSoundFontFromPath(dlg.FileName);
    }

    private void DoLoadRecentSoundFont(object? param)
    {
        if (param is string path && File.Exists(path))
            LoadSoundFontFromPath(path);
        else
            StatusText = "SoundFont file not found";
    }

    private void DoUnloadSoundFont()
    {
        _player.UnloadSoundFont();
        IsSoundFontLoaded = false;
        SoundFontStatus = "Windows MIDI (default)";
        StatusText = "SoundFont unloaded — using Windows MIDI";
    }

    // ─── Undo / Redo ───
    public void SaveUndoState()
    {
        _undoManager.SaveState(CaptureSnapshot());
        CommandManager.InvalidateRequerySuggested();
    }

    private SongSnapshot CaptureSnapshot()
    {
        var barSnaps = Bars.Select(b =>
        {
            b.SyncLyricsToModel();
            return new BarSnapshot
            {
                BeatsPerBarOverride = b.Model.BeatsPerBarOverride,
                Lyrics = b.Model.Lyrics,
                ChordEvents = b.Model.ChordEvents.Select(e => new ChordEventSnapshot
                {
                    Root = e.Chord.Root,
                    Quality = e.Chord.Quality,
                    BassNote = e.Chord.BassNote,
                    StartBeat = e.StartBeat,
                    DurationBeats = e.DurationBeats,
                }).ToList(),
            };
        });
        var loopSnaps = LoopRegions.Select(l => new LoopSnapshot
        {
            Name = l.Name,
            StartBarIndex = l.StartBarIndex,
            EndBarIndex = l.EndBarIndex,
            RepeatCount = l.RepeatCount,
            ColorIndex = l.ColorIndex,
            SectionType = l.SectionType,
        });
        return SongSnapshot.Capture(barSnaps, loopSnaps);
    }

    private void RestoreSnapshot(SongSnapshot snap)
    {
        StopInlineEditing();
        SelectedBar = null;
        IsChordPickerOpen = false;

        Bars.Clear();
        for (int i = 0; i < snap.Bars.Count; i++)
        {
            var bs = snap.Bars[i];
            var bar = new Bar { BeatsPerBarOverride = bs.BeatsPerBarOverride, Lyrics = bs.Lyrics };
            foreach (var es in bs.ChordEvents)
                bar.ChordEvents.Add(new ChordEvent(new Chord(es.Root, es.Quality, es.BassNote), es.StartBeat, es.DurationBeats));
            var vm = new BarViewModel(bar, i) { Lyrics = bs.Lyrics };
            Bars.Add(vm);
        }

        LoopRegions.Clear();
        for (int i = 0; i < snap.Loops.Count; i++)
        {
            var ls = snap.Loops[i];
            LoopRegions.Add(new LoopRegion(ls.StartBarIndex, ls.EndBarIndex, ls.RepeatCount, ls.ColorIndex, ls.Name, ls.SectionType));
        }
        SyncLoopRegionsView();
        RefreshLoopMarkers();
        RefreshAllBarBeats();
    }

    private void DoUndo()
    {
        var snap = _undoManager.Undo(CaptureSnapshot());
        if (snap != null)
        {
            RestoreSnapshot(snap);
            StatusText = "Undo";
        }
        CommandManager.InvalidateRequerySuggested();
    }

    private void DoRedo()
    {
        var snap = _undoManager.Redo(CaptureSnapshot());
        if (snap != null)
        {
            RestoreSnapshot(snap);
            StatusText = "Redo";
        }
        CommandManager.InvalidateRequerySuggested();
    }

    // ─── Composing help ───
    public bool IsComposingHelpOpen
    {
        get => _isComposingHelpOpen;
        set => SetProperty(ref _isComposingHelpOpen, value);
    }

    public NoteName ComposingKey
    {
        get => _composingKey;
        set
        {
            if (SetProperty(ref _composingKey, value))
            {
                _composingKeyManuallySet = true;
                OnPropertyChanged(nameof(DiatonicChords));
                OnPropertyChanged(nameof(ChordGroups));
            }
        }
    }

    public ScaleType ComposingScale
    {
        get => _composingScale;
        set
        {
            if (SetProperty(ref _composingScale, value))
            {
                _composingKeyManuallySet = true;
                OnPropertyChanged(nameof(DiatonicChords));
                OnPropertyChanged(nameof(ChordGroups));
            }
        }
    }

    public NoteName[] AllNotes => Enum.GetValues<NoteName>();
    public ScaleType[] AllScales => [ScaleType.Major, ScaleType.Minor];

    public List<ScaleChordInfo> DiatonicChords =>
        ScaleHelper.GetDiatonicChords(_composingKey, _composingScale);

    public List<ScaleChordGroup> ChordGroups =>
        ScaleHelper.GetAllChordGroups(_composingKey, _composingScale);

    private void AutoDetectKey()
    {
        var chords = Bars
            .SelectMany(b => b.Model.ChordEvents.Select(e => e.Chord))
            .Where(c => c != null)
            .Distinct()
            .ToList();
        if (chords.Count > 0)
        {
            var (key, scale) = ScaleHelper.DetectKey(chords!);
            ComposingKey = key;
            ComposingScale = scale;
            StatusText = $"Detected key: {key.ToDisplayString()} {scale}";
        }
    }

    private void DoPlaySingleChord(object? param)
    {
        if (param is ScaleChordInfo sci)
        {
            _player.PlayChordPreview(sci.Chord, SelectedInstrument);
            StatusText = $"Preview: {sci.Display}";
        }
    }

    // ─── Transport ───
    private void DoPlay()
    {
        var bars = Bars.Select(b => b.Model).ToList();
        if (bars.Count == 0) return;

        IsPlaying = true;
        StatusText = $"Count-in...";
        _player.Play(bars, Tempo, IsLooping, SelectedStyle, LoopRegions, SelectedInstrument, GlobalBeatsPerBar, SelectedStrumPattern, startBarIndex: 0, countIn: true);
    }

    private void DoPlayFromBar(object? param)
    {
        if (param is BarViewModel bar)
        {
            var bars = Bars.Select(b => b.Model).ToList();
            if (bars.Count == 0) return;

            IsPlaying = true;
            StatusText = $"Playing from bar {bar.BarNumber}...";
            _player.Play(bars, Tempo, IsLooping, SelectedStyle, LoopRegions, SelectedInstrument, GlobalBeatsPerBar, SelectedStrumPattern, startBarIndex: bar.Index, countIn: false);
        }
    }

    private void DoStop()
    {
        _player.Stop();
    }

    public void SetBarTimeSig(BarViewModel bar, int beats)
    {
        bar.Model.BeatsPerBarOverride = beats;
        bar.EffectiveBeats = bar.Model.GetEffectiveBeatsPerBar(_globalBeatsPerBar);
        string label = beats == 0 ? "global default" : $"{beats}/4";
        StatusText = $"Bar {bar.BarNumber}: time signature → {label}";
    }

    // ─── Bar management ───
    private void DoAddBar()
    {
        SaveUndoState();
        Bars.Add(new BarViewModel(new Bar(), Bars.Count));
        StatusText = $"{Bars.Count} bars";
    }

    private void DoRemoveBar()
    {
        if (Bars.Count <= 1) return;
        SaveUndoState();
        int lastIndex = Bars.Count - 1;
        if (SelectedBar == Bars[lastIndex])
        {
            SelectedBar = null;
            IsChordPickerOpen = false;
        }
        LoopRegions.RemoveAll(l => l.StartBarIndex >= lastIndex || l.EndBarIndex >= lastIndex);
        Bars.RemoveAt(lastIndex);
        RefreshLoopMarkers();
        StatusText = $"{Bars.Count} bars";
    }

    private void DoDeleteBar(object? param)
    {
        var target = param as BarViewModel ?? SelectedBar;
        if (target == null || Bars.Count <= 1) return;
        SaveUndoState();
        int idx = target.Index;
        StopInlineEditing();
        IsChordPickerOpen = false;
        SelectedBar = null;

        // Remove loops that reference this bar or beyond
        LoopRegions.RemoveAll(l => l.StartBarIndex == idx || l.EndBarIndex == idx);
        // Adjust loop indices for bars after the deleted one
        foreach (var loop in LoopRegions)
        {
            if (loop.StartBarIndex > idx) loop.StartBarIndex--;
            if (loop.EndBarIndex > idx) loop.EndBarIndex--;
        }

        Bars.RemoveAt(idx);
        // Re-index all bars
        for (int i = 0; i < Bars.Count; i++)
            Bars[i].Index = i;

        SyncLoopRegionsView();
        RefreshLoopMarkers();
        RefreshAllBarBeats();
        CommandManager.InvalidateRequerySuggested();
        StatusText = $"Deleted bar {idx + 1} — {Bars.Count} bars";
    }

    private void DoCopyBar()
    {
        if (SelectedBar == null) return;
        var bar = SelectedBar;
        bar.SyncLyricsToModel();
        _clipboardBar = new BarSnapshot
        {
            BeatsPerBarOverride = bar.Model.BeatsPerBarOverride,
            Lyrics = bar.Model.Lyrics,
            ChordEvents = bar.Model.ChordEvents.Select(e => new ChordEventSnapshot
            {
                Root = e.Chord.Root,
                Quality = e.Chord.Quality,
                BassNote = e.Chord.BassNote,
                StartBeat = e.StartBeat,
                DurationBeats = e.DurationBeats,
            }).ToList(),
        };
        CommandManager.InvalidateRequerySuggested();
        StatusText = $"Copied bar {bar.BarNumber}";
    }

    private void DoPasteBar()
    {
        if (SelectedBar == null || _clipboardBar == null) return;
        SaveUndoState();
        var bar = SelectedBar;
        bar.Model.ChordEvents.Clear();
        foreach (var es in _clipboardBar.ChordEvents)
            bar.Model.ChordEvents.Add(new ChordEvent(new Chord(es.Root, es.Quality, es.BassNote), es.StartBeat, es.DurationBeats));
        bar.Model.BeatsPerBarOverride = _clipboardBar.BeatsPerBarOverride;
        bar.Model.Lyrics = _clipboardBar.Lyrics;
        bar.Lyrics = _clipboardBar.Lyrics;
        bar.EffectiveBeats = bar.Model.GetEffectiveBeatsPerBar(_globalBeatsPerBar);
        bar.RefreshChordDisplay();
        StatusText = $"Pasted to bar {bar.BarNumber}";
    }

    // ─── Bar selection & inline editing ───
    private void DoSelectBar(object? param)
    {
        if (param is BarViewModel bar)
        {
            if (_isSettingLoopStart)
            {
                _pendingLoopStart = bar.Index;
                IsSettingLoopStart = false;
                IsSettingLoopEnd = true;
                StatusText = $"Loop start: bar {bar.BarNumber}. Now click a bar for loop end.";
                CommandManager.InvalidateRequerySuggested();
                return;
            }
            if (_isSettingLoopEnd && _pendingLoopStart.HasValue)
            {
                int startIdx = _pendingLoopStart.Value;
                int endIdx = bar.Index;
                if (endIdx < startIdx) (startIdx, endIdx) = (endIdx, startIdx);

                bool partialOverlap = LoopRegions.Any(l => l.IsPartialOverlap(startIdx, endIdx));
                if (partialOverlap)
                {
                    StatusText = "Partial overlap! Loops must fully nest or be separate.";
                    IsSettingLoopEnd = false;
                    _pendingLoopStart = null;
                    return;
                }

                _editingLoop = new LoopRegion(startIdx, endIdx, 2, LoopRegions.Count);
                EditLoopName = "";
                EditLoopRepeat = 2;
                EditLoopStartBar = startIdx + 1;
                EditLoopEndBar = endIdx + 1;
                EditLoopSectionType = SectionType.None;
                IsLoopEditorOpen = true;
                IsSettingLoopEnd = false;
                _pendingLoopStart = null;
                return;
            }

            // Just select the bar and start inline editing
            if (SelectedBar == bar) return; // already selected
            StopInlineEditing();
            ClearFocusRequested?.Invoke();
            SelectedBar = bar;
            bar.IsEditing = true;
            bar.EditBeatIndex = -1;
            bar.InlineText = "";
            StatusText = $"Bar {bar.BarNumber} selected — type chord (all beats), → per-beat, Tab next bar";
        }
    }

    /// <summary>Raised when the UI should clear keyboard focus from any text field.</summary>
    public event Action? ClearFocusRequested;

    private void DoOpenBarSettings(object? param)
    {
        var bar = param as BarViewModel ?? SelectedBar;
        if (bar == null) return;

        StopInlineEditing();
        SelectedBar = bar;

        _pickerTargetBeat = -1;
        OnPropertyChanged(nameof(PickerTargetBeat));
        OnPropertyChanged(nameof(PickerBeatLabel));
        OnPropertyChanged(nameof(IsPickerAllBeats));
        OnPropertyChanged(nameof(IsPickerBeat1));
        OnPropertyChanged(nameof(IsPickerBeat2));
        OnPropertyChanged(nameof(IsPickerBeat3));
        OnPropertyChanged(nameof(IsPickerBeat4));
        OnPropertyChanged(nameof(IsPickerBeat5));

        _pickerBarTimeSig = bar.Model.BeatsPerBarOverride;
        OnPropertyChanged(nameof(PickerBarTimeSig));
        NotifyPickerBeats();

        if (bar.Model.SingleChord != null)
            PickerRoot = bar.Model.SingleChord.Root;
        else if (bar.Model.GetChordAtBeat(0) != null)
            PickerRoot = bar.Model.GetChordAtBeat(0)!.Root;
        else
            PickerRoot = NoteName.C;
        IsChordPickerOpen = true;
    }

    public void StopInlineEditing()
    {
        foreach (var b in Bars)
        {
            if (b.IsEditing)
            {
                b.IsEditing = false;
                b.InlineText = "";
            }
        }
    }

    /// <summary>
    /// Handle a text character typed while a bar is selected for inline editing.
    /// </summary>
    public void HandleInlineTextInput(string text)
    {
        if (SelectedBar == null || !SelectedBar.IsEditing) return;
        if (IsChordPickerOpen || IsLoopEditorOpen) return;

        var bar = SelectedBar;
        bar.InlineText += text.ToLowerInvariant();

        if (ChordParser.TryParse(bar.InlineText, out var chord) && chord != null)
        {
            SaveUndoState();
            if (bar.EditBeatIndex < 0)
            {
                bar.SetChord(chord);
                StatusText = $"Bar {bar.BarNumber}: {chord.DisplayName} (all beats)";
            }
            else
            {
                bar.SetChordAtBeat(bar.EditBeatIndex, chord);
                StatusText = $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: {chord.DisplayName}";
            }
        }
    }

    /// <summary>
    /// Handle special keys for inline editing navigation.
    /// Returns true if the key was consumed.
    /// </summary>
    public bool HandleInlineKeyDown(System.Windows.Input.Key key)
    {
        if (SelectedBar == null || !SelectedBar.IsEditing) return false;
        if (IsChordPickerOpen || IsLoopEditorOpen) return false;

        var bar = SelectedBar;

        switch (key)
        {
            case System.Windows.Input.Key.Right:
                if (bar.EditBeatIndex < 0)
                {
                    // All beats → beat 0
                    bar.EditBeatIndex = 0;
                    bar.InlineText = "";
                    StatusText = $"Bar {bar.BarNumber} — editing beat 1";
                }
                else if (bar.EditBeatIndex < bar.EffectiveBeats - 1)
                {
                    bar.EditBeatIndex++;
                    bar.InlineText = "";
                    StatusText = $"Bar {bar.BarNumber} — editing beat {bar.EditBeatIndex + 1}";
                }
                else
                {
                    // Last beat → back to all beats
                    bar.EditBeatIndex = -1;
                    bar.InlineText = "";
                    StatusText = $"Bar {bar.BarNumber} — editing all beats";
                }
                return true;

            case System.Windows.Input.Key.Left:
                if (bar.EditBeatIndex == 0)
                {
                    // Beat 0 → back to all beats
                    bar.EditBeatIndex = -1;
                    bar.InlineText = "";
                    StatusText = $"Bar {bar.BarNumber} — editing all beats";
                }
                else if (bar.EditBeatIndex > 0)
                {
                    bar.EditBeatIndex--;
                    bar.InlineText = "";
                    StatusText = $"Bar {bar.BarNumber} — editing beat {bar.EditBeatIndex + 1}";
                }
                return true;

            case System.Windows.Input.Key.Tab:
                bool shiftHeld = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
                if (shiftHeld)
                {
                    // Shift-Tab: move to previous bar
                    int prevIdx = bar.Index - 1;
                    if (prevIdx >= 0)
                    {
                        StopInlineEditing();
                        var prevBar = Bars[prevIdx];
                        SelectedBar = prevBar;
                        prevBar.IsEditing = true;
                        prevBar.EditBeatIndex = -1;
                        prevBar.InlineText = "";
                        StatusText = $"Bar {prevBar.BarNumber} selected — type chord (all beats)";
                    }
                }
                else
                {
                    // Tab: move to next bar
                    int nextIdx = bar.Index + 1;
                    if (nextIdx < Bars.Count)
                    {
                        StopInlineEditing();
                        var nextBar = Bars[nextIdx];
                        SelectedBar = nextBar;
                        nextBar.IsEditing = true;
                        nextBar.EditBeatIndex = -1;
                        nextBar.InlineText = "";
                        StatusText = $"Bar {nextBar.BarNumber} selected — type chord (all beats)";
                    }
                }
                return true;

            case System.Windows.Input.Key.Delete:
                if (bar.InlineText.Length > 0)
                {
                    bar.InlineText = "";
                    StatusText = bar.EditBeatIndex < 0
                        ? $"Bar {bar.BarNumber}: input cleared"
                        : $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: input cleared";
                }
                else
                {
                    SaveUndoState();
                    if (bar.EditBeatIndex < 0)
                    {
                        bar.ClearAllChords();
                        StatusText = $"Bar {bar.BarNumber}: all chords cleared";
                    }
                    else
                    {
                        bar.SetChordAtBeat(bar.EditBeatIndex, null);
                        StatusText = $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: cleared";
                    }
                }
                return true;

            case System.Windows.Input.Key.Back:
                if (bar.InlineText.Length > 0)
                {
                    bar.InlineText = "";
                    StatusText = bar.EditBeatIndex < 0
                        ? $"Bar {bar.BarNumber}: input cleared"
                        : $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: input cleared";
                }
                else if (bar.EditBeatIndex > 0)
                {
                    // Copy previous beat's chord to current beat
                    var prevChord = bar.Model.GetChordAtBeat(bar.EditBeatIndex - 1);
                    if (prevChord != null)
                    {
                        SaveUndoState();
                        bar.SetChordAtBeat(bar.EditBeatIndex, prevChord);
                        StatusText = $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: set to {prevChord.DisplayName} (from beat {bar.EditBeatIndex})";
                    }
                }
                else
                {
                    SaveUndoState();
                    if (bar.EditBeatIndex < 0)
                    {
                        bar.ClearAllChords();
                        StatusText = $"Bar {bar.BarNumber}: all chords cleared";
                    }
                    else
                    {
                        bar.SetChordAtBeat(bar.EditBeatIndex, null);
                        StatusText = $"Bar {bar.BarNumber} beat {bar.EditBeatIndex + 1}: cleared";
                    }
                }
                return true;

            case System.Windows.Input.Key.Escape:
                StopInlineEditing();
                SelectedBar = null;
                StatusText = "Ready";
                return true;
        }

        return false;
    }

    private void DoSelectRoot(object? param)
    {
        if (param is NoteName root)
            PickerRoot = root;
    }

    private void DoSetPickerBeat(object? param)
    {
        if (param is int beat)
            PickerTargetBeat = beat;
        else if (param is string s && int.TryParse(s, out int b))
            PickerTargetBeat = b;
    }

    private void DoSelectQuality(object? param)
    {
        if (param is ChordQuality quality && SelectedBar != null)
        {
            SaveUndoState();
            var chord = new Chord(PickerRoot, quality);
            if (PickerTargetBeat < 0)
            {
                SelectedBar.SetChord(chord);
                StatusText = $"Bar {SelectedBar.BarNumber}: {chord.DisplayName}";
            }
            else
            {
                SelectedBar.SetChordAtBeat(PickerTargetBeat, chord);
                StatusText = $"Bar {SelectedBar.BarNumber} beat {PickerTargetBeat + 1}: {chord.DisplayName}";
            }
            // Stay open so user can continue editing beats
        }
    }

    private void DoClearChord()
    {
        if (SelectedBar != null)
        {
            SaveUndoState();
            if (PickerTargetBeat < 0)
            {
                SelectedBar.ClearAllChords();
                StatusText = $"Cleared bar {SelectedBar.BarNumber}";
            }
            else
            {
                SelectedBar.SetChordAtBeat(PickerTargetBeat, null);
                StatusText = $"Cleared bar {SelectedBar.BarNumber} beat {PickerTargetBeat + 1}";
            }
            // Stay open like DoSelectQuality
        }
    }

    private void DoClosePicker()
    {
        IsChordPickerOpen = false;
        // Don't deselect bar — keep it selected for inline editing
    }

    // ─── Loop management ───
    private void DoSetLoopStart()
    {
        IsSettingLoopStart = true;
        IsSettingLoopEnd = false;
        _pendingLoopStart = null;
        StatusText = "Click a bar to set loop start...";
    }

    private void DoSetLoopEnd()
    {
    }

    private void DoConfirmLoopEdit()
    {
        if (_editingLoop != null)
        {
            SaveUndoState();
            int newStart = EditLoopStartBar - 1;
            int newEnd = EditLoopEndBar - 1;
            if (newEnd < newStart) (newStart, newEnd) = (newEnd, newStart);
            newStart = Math.Clamp(newStart, 0, Bars.Count - 1);
            newEnd = Math.Clamp(newEnd, 0, Bars.Count - 1);

            // Check partial overlaps against other loops (excluding the one being edited)
            bool partialOverlap = LoopRegions
                .Where(l => l != _editingLoop)
                .Any(l => l.IsPartialOverlap(newStart, newEnd));
            if (partialOverlap)
            {
                StatusText = "Partial overlap! Loops must fully nest or be separate.";
                return;
            }

            _editingLoop.StartBarIndex = newStart;
            _editingLoop.EndBarIndex = newEnd;
            _editingLoop.Name = EditLoopName;
            _editingLoop.RepeatCount = EditLoopRepeat;
            _editingLoop.SectionType = EditLoopSectionType;
            if (!_isEditingExistingLoop)
                LoopRegions.Add(_editingLoop);
            SyncLoopRegionsView();
            RefreshLoopMarkers();
            string label = string.IsNullOrWhiteSpace(_editingLoop.Name)
                ? $"bars {_editingLoop.StartBarIndex + 1}–{_editingLoop.EndBarIndex + 1}"
                : $"\"{_editingLoop.Name}\" (bars {_editingLoop.StartBarIndex + 1}–{_editingLoop.EndBarIndex + 1})";
            StatusText = $"Loop {label} ×{_editingLoop.RepeatCount}";
            _editingLoop = null;
        }
        _isEditingExistingLoop = false;
        IsLoopEditorOpen = false;
    }

    private void DoCancelLoopEdit()
    {
        _editingLoop = null;
        _isEditingExistingLoop = false;
        IsLoopEditorOpen = false;
        StatusText = "Loop cancelled.";
    }

    private void DoEditLoop(object? param)
    {
        LoopRegion? loop = null;
        if (param is BarViewModel bar)
        {
            loop = LoopRegions
                .Where(l => l.ContainsBar(bar.Index))
                .OrderBy(l => l.EndBarIndex - l.StartBarIndex)
                .FirstOrDefault();
        }
        else if (LoopRegions.Count > 0)
        {
            loop = LoopRegions[^1];
        }

        if (loop != null)
        {
            _editingLoop = loop;
            _isEditingExistingLoop = true;
            EditLoopName = loop.Name;
            EditLoopRepeat = loop.RepeatCount;
            EditLoopStartBar = loop.StartBarIndex + 1;
            EditLoopEndBar = loop.EndBarIndex + 1;
            EditLoopSectionType = loop.SectionType;
            IsLoopEditorOpen = true;
        }
    }

    private void DoEditLoopByIndex(object? param)
    {
        if (param is LoopRegion loop)
        {
            _editingLoop = loop;
            _isEditingExistingLoop = true;
            EditLoopName = loop.Name;
            EditLoopRepeat = loop.RepeatCount;
            EditLoopStartBar = loop.StartBarIndex + 1;
            EditLoopEndBar = loop.EndBarIndex + 1;
            EditLoopSectionType = loop.SectionType;
            IsLoopEditorOpen = true;
        }
    }

    private void DoRemoveLoop(object? param)
    {
        if (param is BarViewModel bar)
        {
            var loop = LoopRegions
                .Where(l => l.ContainsBar(bar.Index))
                .OrderBy(l => l.EndBarIndex - l.StartBarIndex)
                .FirstOrDefault();
            if (loop != null)
            {
                LoopRegions.Remove(loop);
                SyncLoopRegionsView();
                RefreshLoopMarkers();
                StatusText = $"Removed loop at bars {loop.StartBarIndex + 1}–{loop.EndBarIndex + 1}";
            }
        }
        else if (LoopRegions.Count > 0)
        {
            var last = LoopRegions[^1];
            LoopRegions.RemoveAt(LoopRegions.Count - 1);
            SyncLoopRegionsView();
            RefreshLoopMarkers();
            StatusText = $"Removed loop at bars {last.StartBarIndex + 1}–{last.EndBarIndex + 1}";
        }
    }

    private void DoRemoveLoopByIndex(object? param)
    {
        if (param is LoopRegion loop && LoopRegions.Contains(loop))
        {
            LoopRegions.Remove(loop);
            SyncLoopRegionsView();
            RefreshLoopMarkers();
            StatusText = $"Removed loop at bars {loop.StartBarIndex + 1}–{loop.EndBarIndex + 1}";
        }
    }

    private void DoIncreaseLoopRepeat(object? param)
    {
        if (param is BarViewModel bar)
        {
            var loop = LoopRegions.FirstOrDefault(l => l.ContainsBar(bar.Index));
            if (loop != null && loop.RepeatCount < 99)
            {
                loop.RepeatCount++;
                RefreshLoopMarkers();
                StatusText = $"Loop bars {loop.StartBarIndex + 1}–{loop.EndBarIndex + 1}: ×{loop.RepeatCount}";
            }
        }
    }

    private void DoDecreaseLoopRepeat(object? param)
    {
        if (param is BarViewModel bar)
        {
            var loop = LoopRegions.FirstOrDefault(l => l.ContainsBar(bar.Index));
            if (loop != null && loop.RepeatCount > 1)
            {
                loop.RepeatCount--;
                RefreshLoopMarkers();
                StatusText = $"Loop bars {loop.StartBarIndex + 1}–{loop.EndBarIndex + 1}: ×{loop.RepeatCount}";
            }
        }
    }

    private void SyncLoopRegionsView()
    {
        LoopRegionsView.Clear();
        foreach (var l in LoopRegions)
            LoopRegionsView.Add(l);
    }

    private void RefreshAllBarBeats()
    {
        foreach (var bar in Bars)
            bar.EffectiveBeats = bar.Model.GetEffectiveBeatsPerBar(_globalBeatsPerBar);
    }

    private void RefreshLoopMarkers()
    {
        foreach (var bar in Bars)
            bar.ClearLoopMarkers();

        // Sort loops outermost-first (largest span first) for layered display
        var sorted = LoopRegions.OrderByDescending(l => l.EndBarIndex - l.StartBarIndex).ToList();

        // Set single-loop backward-compat props (innermost wins)
        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            var loop = sorted[i];
            for (int b = loop.StartBarIndex; b <= loop.EndBarIndex && b < Bars.Count; b++)
            {
                Bars[b].IsInLoop = true;
                Bars[b].LoopColorIndex = loop.ColorIndex % 4;
                if (b == loop.StartBarIndex)
                {
                    Bars[b].IsLoopStart = true;
                    Bars[b].LoopName = loop.Name;
                }
                if (b == loop.EndBarIndex)
                {
                    Bars[b].IsLoopEnd = true;
                    Bars[b].LoopRepeatCount = loop.RepeatCount;
                }
            }
        }

        // Build LoopLayers per bar — pack non-overlapping loops into the same layer
        var loopColors = new System.Windows.Media.SolidColorBrush[]
        {
            new(System.Windows.Media.Color.FromRgb(0x4e, 0xc9, 0xb0)),
            new(System.Windows.Media.Color.FromRgb(0xdc, 0xdc, 0x6e)),
            new(System.Windows.Media.Color.FromRgb(0xc5, 0x86, 0xc0)),
            new(System.Windows.Media.Color.FromRgb(0x56, 0x9c, 0xd6)),
        };
        foreach (var bar in Bars)
            bar.LoopLayers.Clear();

        // Assign each loop to the lowest layer that has no overlap
        var layerAssignments = new List<(LoopRegion loop, int layer)>();
        var layerEnds = new List<int>(); // tracks the max EndBarIndex used per layer
        foreach (var loop in sorted)
        {
            int assignedLayer = -1;
            for (int li = 0; li < layerEnds.Count; li++)
            {
                // No overlap if this loop starts after the last loop in this layer ends
                bool overlaps = sorted
                    .Where(l => layerAssignments.Any(a => a.loop == l && a.layer == li))
                    .Any(l => loop.StartBarIndex <= l.EndBarIndex && loop.EndBarIndex >= l.StartBarIndex);
                if (!overlaps)
                {
                    assignedLayer = li;
                    if (loop.EndBarIndex > layerEnds[li]) layerEnds[li] = loop.EndBarIndex;
                    break;
                }
            }
            if (assignedLayer < 0)
            {
                assignedLayer = layerEnds.Count;
                layerEnds.Add(loop.EndBarIndex);
            }
            layerAssignments.Add((loop, assignedLayer));
        }

        int totalLayers = layerEnds.Count;
        foreach (var (loop, layer) in layerAssignments)
        {
            var brush = loopColors[loop.ColorIndex % loopColors.Length];
            for (int b = loop.StartBarIndex; b <= loop.EndBarIndex && b < Bars.Count; b++)
            {
                while (Bars[b].LoopLayers.Count < totalLayers)
                    Bars[b].LoopLayers.Add(new LoopLayerInfo { Brush = System.Windows.Media.Brushes.Transparent });
                Bars[b].LoopLayers[layer] = new LoopLayerInfo
                {
                    Brush = brush,
                    IsStart = b == loop.StartBarIndex,
                    IsEnd = b == loop.EndBarIndex,
                    Name = loop.DisplayLabel,
                    RepeatCount = loop.IsLoop ? loop.RepeatCount : 0,
                };
            }
        }
        // Ensure all bars have same layer count for alignment
        foreach (var bar in Bars)
        {
            while (bar.LoopLayers.Count < totalLayers)
                bar.LoopLayers.Add(new LoopLayerInfo { Brush = System.Windows.Media.Brushes.Transparent });
        }

        // Calculate TotalPlayCount per bar (product of all enclosing loop repeat counts)
        foreach (var bar in Bars)
        {
            int playCount = 1;
            foreach (var loop in LoopRegions)
            {
                if (loop.IsLoop && bar.Index >= loop.StartBarIndex && bar.Index <= loop.EndBarIndex)
                    playCount *= loop.RepeatCount;
            }
            bar.TotalPlayCount = playCount;
        }
    }

    // ─── Save / Load ───
    private void DoSave()
    {
        if (_currentFilePath != null)
            SaveToFile(_currentFilePath);
        else
            DoSaveAs();
    }

    private void DoSaveAs()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "ChordBox Song (*.cbs)|*.cbs|All Files (*.*)|*.*",
            DefaultExt = ".cbs",
            FileName = SongTitle,
        };
        if (dlg.ShowDialog() == true)
        {
            _currentFilePath = dlg.FileName;
            SaveToFile(_currentFilePath);
        }
    }

    private void SaveToFile(string path)
    {
        try
        {
            var song = new SongFile
            {
                Title = SongTitle,
                Tempo = Tempo,
                StyleName = SelectedStyle.Name,
                InstrumentName = SelectedInstrument.Name,
                GlobalBeatsPerBar = GlobalBeatsPerBar,
            };

            foreach (var barVm in Bars)
            {
                barVm.SyncLyricsToModel();
                var bd = new BarData
                {
                    BeatsPerBarOverride = barVm.Model.BeatsPerBarOverride,
                    Lyrics = barVm.Model.Lyrics,
                };
                foreach (var ev in barVm.Model.ChordEvents)
                {
                    bd.ChordEvents.Add(new ChordEventData
                    {
                        Root = ev.Chord.Root.ToString(),
                        Quality = ev.Chord.Quality.ToString(),
                        BassNote = ev.Chord.BassNote?.ToString(),
                        StartBeat = ev.StartBeat,
                        DurationBeats = ev.DurationBeats,
                    });
                }
                song.Bars.Add(bd);
            }

            foreach (var loop in LoopRegions)
            {
                song.Loops.Add(new LoopData
                {
                    Name = loop.Name,
                    StartBarIndex = loop.StartBarIndex,
                    EndBarIndex = loop.EndBarIndex,
                    RepeatCount = loop.RepeatCount,
                    SectionType = loop.SectionType.ToString(),
                });
            }

            // Save strum pattern
            song.StrumPatternName = SelectedStrumPattern.Name;
            if (SelectedStrumPattern.IsCustom)
            {
                song.CustomStrumPattern = new StrumPatternData
                {
                    Name = SelectedStrumPattern.Name,
                    Events = SelectedStrumPattern.Events.Select(e => new StrumEventData
                    {
                        Type = e.Type.ToString(),
                        Articulation = e.Articulation.ToString(),
                        DurationInBeats = e.DurationInBeats,
                    }).ToList(),
                };
            }

            File.WriteAllText(path, song.ToJson());
            StatusText = $"Saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    private void DoOpen()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "ChordBox Song (*.cbs)|*.cbs|All Files (*.*)|*.*",
            DefaultExt = ".cbs",
        };
        if (dlg.ShowDialog() == true)
        {
            LoadFromFile(dlg.FileName);
        }
    }

    private void LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var song = SongFile.FromJson(json);
            if (song == null)
            {
                StatusText = "Failed to parse song file.";
                return;
            }

            _player.Stop();

            SongTitle = song.Title;
            Tempo = song.Tempo;
            _globalBeatsPerBar = song.GlobalBeatsPerBar > 0 ? song.GlobalBeatsPerBar : 4;
            OnPropertyChanged(nameof(GlobalBeatsPerBar));

            var style = PlayStyle.AllStyles.FirstOrDefault(s => s.Name == song.StyleName);
            if (style != null) SelectedStyle = style;

            var instr = Instrument.AllInstruments.FirstOrDefault(i => i.Name == song.InstrumentName);
            if (instr != null) SelectedInstrument = instr;

            Bars.Clear();
            for (int i = 0; i < song.Bars.Count; i++)
            {
                var bd = song.Bars[i];
                var bar = new Bar { BeatsPerBarOverride = bd.BeatsPerBarOverride, Lyrics = bd.Lyrics ?? "" };
                foreach (var evd in bd.ChordEvents)
                {
                    if (Enum.TryParse<NoteName>(evd.Root, out var root) &&
                        Enum.TryParse<ChordQuality>(evd.Quality, out var quality))
                    {
                        NoteName? bass = null;
                        if (!string.IsNullOrEmpty(evd.BassNote) && Enum.TryParse<NoteName>(evd.BassNote, out var bn))
                            bass = bn;
                        bar.ChordEvents.Add(new ChordEvent(new Chord(root, quality, bass), evd.StartBeat, evd.DurationBeats));
                    }
                }
                var barVm = new BarViewModel(bar, i);
                barVm.Lyrics = bar.Lyrics;
                Bars.Add(barVm);
            }

            LoopRegions.Clear();
            for (int i = 0; i < song.Loops.Count; i++)
            {
                var ld = song.Loops[i];
                Enum.TryParse<SectionType>(ld.SectionType, out var secType);
                LoopRegions.Add(new LoopRegion(ld.StartBarIndex, ld.EndBarIndex, ld.RepeatCount, i, ld.Name, secType));
            }
            SyncLoopRegionsView();
            RefreshLoopMarkers();
            RefreshAllBarBeats();

            // Load strum pattern
            if (song.CustomStrumPattern != null)
            {
                var events = song.CustomStrumPattern.Events.Select(e =>
                {
                    Enum.TryParse<StrumType>(e.Type, out var t);
                    Enum.TryParse<StrumArticulation>(e.Articulation, out var a);
                    return new StrumEvent(t, e.DurationInBeats, a);
                }).ToList();
                var custom = new StrumPattern(song.CustomStrumPattern.Name, events, true);
                StrumPatterns.Add(custom);
                SelectedStrumPattern = custom;
            }
            else if (!string.IsNullOrEmpty(song.StrumPatternName))
            {
                var sp = StrumPattern.AllPatterns.FirstOrDefault(p => p.Name == song.StrumPatternName);
                if (sp != null) SelectedStrumPattern = sp;
            }

            _currentFilePath = path;
            StatusText = $"Loaded: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Load failed: {ex.Message}";
        }
    }

    private void DoNew()
    {
        _player.Stop();
        SongTitle = "Untitled";
        Bars.Clear();
        LoopRegions.Clear();
        SyncLoopRegionsView();
        _currentFilePath = null;
        _globalBeatsPerBar = 4;
        OnPropertyChanged(nameof(GlobalBeatsPerBar));

        for (int i = 0; i < 8; i++)
            Bars.Add(new BarViewModel(new Bar(), i));

        RefreshLoopMarkers();
        RefreshAllBarBeats();
        StatusText = "New song created.";
    }

    // ─── Playback events ───
    private void OnBeatChanged(int barIndex, int beat)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // Count-in: barIndex == -1
            if (barIndex < 0)
            {
                StatusText = $"Count-in: {beat + 1}...";
                return;
            }

            if (_currentBarIndex >= 0 && _currentBarIndex < Bars.Count && _currentBarIndex != barIndex)
                Bars[_currentBarIndex].IsPlaying = false;

            _currentBarIndex = barIndex;

            if (barIndex >= 0 && barIndex < Bars.Count)
            {
                Bars[barIndex].IsPlaying = true;
                Bars[barIndex].CurrentBeat = beat;
                StatusText = $"Bar {barIndex + 1}, Beat {beat + 1}  [{SelectedStyle.Name}]";
            }
        });
    }

    private void OnPlaybackStopped()
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // If a new playback already started, don't reset state
            if (_player.IsPlaying) return;

            IsPlaying = false;
            if (_currentBarIndex >= 0 && _currentBarIndex < Bars.Count)
                Bars[_currentBarIndex].IsPlaying = false;
            _currentBarIndex = -1;
            StatusText = "Stopped";
        });
    }

    // ─── App Config persistence ───
    private void LoadAppConfig()
    {
        var config = AppConfig.Load();

        // Load recent SF2s
        _recentSoundFonts = config.RecentSoundFonts.ToList();
        RecentSoundFonts.Clear();
        foreach (var p in _recentSoundFonts) RecentSoundFonts.Add(p);

        // Load custom strum patterns
        foreach (var spd in config.CustomStrumPatterns)
        {
            var events = spd.Events.Select(e =>
            {
                Enum.TryParse<StrumType>(e.Type, out var t);
                Enum.TryParse<StrumArticulation>(e.Articulation, out var a);
                return new StrumEvent(t, e.DurationInBeats, a);
            }).ToList();
            if (events.Count > 0)
                StrumPatterns.Add(new StrumPattern(spd.Name, events, true));
        }
    }

    private void SaveAppConfig()
    {
        var config = new AppConfig
        {
            RecentSoundFonts = _recentSoundFonts.ToList(),
            CustomStrumPatterns = StrumPatterns
                .Where(p => p.IsCustom)
                .Select(p => new StrumPatternData
                {
                    Name = p.Name,
                    Events = p.Events.Select(e => new StrumEventData
                    {
                        Type = e.Type.ToString(),
                        Articulation = e.Articulation.ToString(),
                        DurationInBeats = e.DurationInBeats,
                    }).ToList(),
                }).ToList(),
        };
        config.Save();
    }

    public void SaveCustomStrumPatterns()
    {
        SaveAppConfig();
    }

    public void Dispose()
    {
        _player.Dispose();
    }
}
