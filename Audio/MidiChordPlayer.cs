using System.Diagnostics;
using NAudio.Midi;
using ChordBox.Models;

namespace ChordBox.Audio;

public class MidiChordPlayer : IDisposable
{
    private MidiOut? _midiOut;
    private CancellationTokenSource? _cts;
    private Task? _playbackTask;
    private readonly List<int> _activeNotes = new();
    private const int MidiChannel = 1;
    private const int PercussionChannel = 10;
    private const int MetronomeNote = 76; // Hi wood block
    private const int MetronomeAccentNote = 77; // Low wood block (accent on beat 1)

    private SoundFontPlayer? _sfPlayer;
    private bool _useSoundFont;

    public event Action<int, int>? BeatChanged;
    public event Action? PlaybackStopped;

    public bool IsPlaying { get; private set; }
    public bool UseSoundFont => _useSoundFont;
    public string? LoadedSoundFontPath => _sfPlayer?.LoadedPath;

    public bool LoadSoundFont(string sf2Path)
    {
        _sfPlayer ??= new SoundFontPlayer();
        bool ok = _sfPlayer.Load(sf2Path);
        _useSoundFont = ok;
        return ok;
    }

    public void UnloadSoundFont()
    {
        _sfPlayer?.Unload();
        _useSoundFont = false;
    }

    public List<(int Bank, int Program, string Name)> GetSoundFontPresets()
    {
        return _sfPlayer?.GetPresets() ?? new();
    }

    // Volatile shared state for live parameter changes
    private volatile int _liveTempo;
    private volatile PlayStyle _liveStyle = PlayStyle.Pop;
    private volatile StrumPattern _liveStrumPattern = StrumPattern.EighthDownUp;
    private volatile Instrument _liveInstrument = Instrument.AcousticPiano;
    private volatile int _liveGlobalBeatsPerBar = 4;
    private volatile bool _liveGlobalLoop = true;
    private int _currentInstrumentProgram = -1;
    private const double StrumSpreadMs = 20.0; // total ms to spread notes across a strum

    public void UpdateTempo(int tempo) => _liveTempo = tempo;
    public void UpdateStyle(PlayStyle style) => _liveStyle = style;
    public void UpdateStrumPattern(StrumPattern pattern) => _liveStrumPattern = pattern;
    public void UpdateGlobalLoop(bool loop) => _liveGlobalLoop = loop;

    public void UpdateInstrument(Instrument instrument)
    {
        _liveInstrument = instrument;
        // Send program change immediately if playing
        if (_midiOut != null && instrument.MidiProgram != _currentInstrumentProgram)
        {
            try
            {
                _midiOut.Send(MidiMessage.ChangePatch(instrument.MidiProgram, MidiChannel).RawData);
                _currentInstrumentProgram = instrument.MidiProgram;
            }
            catch { }
        }
    }

    public void UpdateGlobalBeatsPerBar(int beats) => _liveGlobalBeatsPerBar = beats;

    public void Play(List<Bar> bars, int tempo, bool globalLoop, PlayStyle style,
                     List<LoopRegion> loopRegions, Instrument? instrument, int globalBeatsPerBar,
                     StrumPattern? strumPattern = null, int startBarIndex = 0, bool countIn = false)
    {
        Stop();

        var inst = instrument ?? Instrument.AcousticPiano;
        _liveTempo = tempo;
        _liveStyle = style;
        _liveStrumPattern = strumPattern ?? StrumPattern.EighthDownUp;
        _liveInstrument = inst;
        _liveGlobalBeatsPerBar = globalBeatsPerBar;
        _liveGlobalLoop = globalLoop;

        if (_useSoundFont && _sfPlayer != null)
        {
            _sfPlayer.StartAudio();
            _sfPlayer.SetProgram(MidiChannel - 1, inst.MidiProgram);
            _currentInstrumentProgram = inst.MidiProgram;
        }
        else
        {
            try
            {
                _midiOut = new MidiOut(0);
                _currentInstrumentProgram = inst.MidiProgram;
                _midiOut.Send(MidiMessage.ChangePatch(inst.MidiProgram, MidiChannel).RawData);
            }
            catch (Exception)
            {
                PlaybackStopped?.Invoke();
                return;
            }
        }

        _cts = new CancellationTokenSource();
        IsPlaying = true;

        var token = _cts.Token;
        var sequence = BuildPlaySequence(bars.Count, loopRegions);

        // Find starting position in sequence
        int startSeqIndex = 0;
        if (startBarIndex > 0)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                if (sequence[i] >= startBarIndex) { startSeqIndex = i; break; }
            }
        }

        _playbackTask = Task.Run(() =>
        {
            try
            {
                // Count-in metronome
                if (countIn)
                {
                    int countInBeats = _liveGlobalBeatsPerBar;
                    double countInInterval = 60000.0 / _liveTempo;
                    for (int ci = 0; ci < countInBeats; ci++)
                    {
                        if (token.IsCancellationRequested) break;
                        BeatChanged?.Invoke(-1, ci);
                        int clickNote = ci == 0 ? MetronomeAccentNote : MetronomeNote;
                        int clickVel = ci == 0 ? 110 : 80;
                        SendNoteOn(clickNote, clickVel, PercussionChannel);
                        WaitBeat(countInInterval * 0.3, token);
                        SendNoteOff(clickNote, PercussionChannel);
                        WaitBeat(countInInterval * 0.7, token);
                    }
                }

                Chord? previousChord = null;
                int seqIndex = startSeqIndex;

                while (!token.IsCancellationRequested)
                {
                    if (seqIndex >= sequence.Count)
                    {
                        if (_liveGlobalLoop)
                            seqIndex = 0;
                        else
                            break;
                    }

                    // Read live state each bar
                    var curStyle = _liveStyle;
                    var curStrumPattern = _liveStrumPattern;
                    var curInst = _liveInstrument;
                    int curGlobalBeats = _liveGlobalBeatsPerBar;

                    int barIndex = sequence[seqIndex];
                    var bar = bars[barIndex];
                    int beatsInBar = bar.GetEffectiveBeatsPerBar(curGlobalBeats);
                    bool usePower = curInst.UsePowerChords;

                    if (curInst.IsStrummed)
                    {
                        // ── Guitar strum playback ──
                        PlayBarStrum(bar, barIndex, beatsInBar, curStrumPattern, usePower, token);
                    }
                    else
                    {
                        // ── Piano / keyboard playback ──
                        int patternLen = curStyle.SubBeatActions.Length;

                        for (int beat = 0; beat < beatsInBar; beat++)
                        {
                            if (token.IsCancellationRequested) break;

                            double beatIntervalMs = 60000.0 / _liveTempo;
                            double subBeatMs = beatIntervalMs / 2.0;

                            var chord = bar.GetChordAtBeat(beat);

                            // Check for TripletArp on the on-beat
                            int onBeatIdx = beat * 2;
                            var onBeatAction = curStyle.SubBeatActions[onBeatIdx % patternLen];
                            if (chord != null && onBeatAction == BeatAction.TripletArp)
                            {
                                BeatChanged?.Invoke(barIndex, beat);
                                int tripVel = curStyle.Velocities[onBeatIdx % patternLen];
                                AllNotesOff();
                                PlayTriplet(chord, tripVel, usePower, beatIntervalMs, token);
                                previousChord = chord;
                                continue;
                            }

                            for (int half = 0; half < 2; half++)
                            {
                                if (token.IsCancellationRequested) break;

                                int subIdx = beat * 2 + half;
                                var action = curStyle.SubBeatActions[subIdx % patternLen];
                                int velocity = curStyle.Velocities[subIdx % patternLen];
                                double gate = curStyle.GateFractions[subIdx % patternLen];

                                if (half == 0) BeatChanged?.Invoke(barIndex, beat);

                                if (chord != null && action != BeatAction.Rest && velocity > 0)
                                {
                                    bool isArp = action == BeatAction.ArpUp || action == BeatAction.ArpDown;
                                    AllNotesOff();
                                    if (isArp)
                                        PlayArpNote(chord, action == BeatAction.ArpUp, velocity, usePower, subIdx);
                                    else
                                        PlayAction(chord, action, velocity, usePower);
                                    previousChord = chord;
                                }
                                else if (action == BeatAction.Rest)
                                {
                                    // Rest: let previous notes sustain
                                }
                                else
                                {
                                    if (previousChord != null)
                                    {
                                        AllNotesOff();
                                        previousChord = null;
                                    }
                                }

                                if (gate < 0.99 && chord != null && action != BeatAction.Rest)
                                {
                                    double noteOnMs = subBeatMs * gate;
                                    double restMs = subBeatMs - noteOnMs;
                                    WaitBeat(noteOnMs, token);
                                    AllNotesOff();
                                    previousChord = null;
                                    WaitBeat(restMs, token);
                                }
                                else
                                {
                                    WaitBeat(subBeatMs, token);
                                }
                            }
                        }
                    }

                    seqIndex++;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                AllNotesOff();
                IsPlaying = false;
                PlaybackStopped?.Invoke();
            }
        }, token);
    }

    private static List<int> BuildPlaySequence(int barCount, List<LoopRegion> loopRegions)
    {
        var validLoops = loopRegions
            .Where(l => l.StartBarIndex < barCount && l.EndBarIndex < barCount)
            .ToList();

        return ExpandRange(0, barCount - 1, validLoops);
    }

    private static List<int> ExpandRange(int startBar, int endBar, List<LoopRegion> loops)
    {
        var result = new List<int>();
        int bar = startBar;

        while (bar <= endBar)
        {
            // Find the outermost loop starting at this bar position
            var loop = loops
                .Where(l => l.StartBarIndex == bar && l.EndBarIndex <= endBar)
                .OrderByDescending(l => l.EndBarIndex - l.StartBarIndex)
                .FirstOrDefault();

            if (loop != null)
            {
                // Collect inner loops (fully contained within this loop, but not the loop itself)
                var innerLoops = loops
                    .Where(l => l != loop && l.StartBarIndex >= loop.StartBarIndex && l.EndBarIndex <= loop.EndBarIndex)
                    .ToList();

                // Expand this loop's range N times, recursively handling inner loops
                for (int rep = 0; rep < loop.RepeatCount; rep++)
                {
                    result.AddRange(ExpandRange(loop.StartBarIndex, loop.EndBarIndex, innerLoops));
                }

                bar = loop.EndBarIndex + 1;
            }
            else
            {
                result.Add(bar);
                bar++;
            }
        }

        return result;
    }

    private void PlayAction(Chord chord, BeatAction action, int velocity, bool usePower)
    {
        switch (action)
        {
            case BeatAction.FullChord:
                PlayChord(chord, velocity, usePower);
                break;
            case BeatAction.ChordTones:
                PlayChordTones(chord, velocity);
                break;
            case BeatAction.BassOnly:
                PlayBassNote(chord, velocity);
                break;
            case BeatAction.BassAndRoot:
                PlayBassAndRoot(chord, velocity);
                break;
        }
    }

    private void PlayTriplet(Chord chord, int velocity, bool usePower, double beatMs, CancellationToken token)
    {
        var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
        double tripletMs = beatMs / 3.0;

        // 1st: bass note
        PlayBassNote(chord, velocity);
        WaitBeat(tripletMs, token);

        // 2nd: first chord tone
        AllNotesOff();
        if (notes.Length > 1)
        {
            int note = notes[1];
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, (int)(velocity * 0.85), MidiChannel);
                _activeNotes.Add(note);
            }
        }
        WaitBeat(tripletMs, token);

        // 3rd: second chord tone
        AllNotesOff();
        if (notes.Length > 2)
        {
            int note = notes[2];
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, (int)(velocity * 0.80), MidiChannel);
                _activeNotes.Add(note);
            }
        }
        WaitBeat(tripletMs, token);
    }

    private void PlayBarStrum(Bar bar, int barIndex, int beatsInBar, StrumPattern pattern, bool usePower, CancellationToken token)
    {
        var events = pattern.Events;
        if (events.Count == 0) return;

        double patternBeats = pattern.TotalBeats;
        if (patternBeats <= 0) return;

        int eventIdx = 0;
        double beatPos = 0;
        int lastReportedBeat = -1;

        while (beatPos < beatsInBar && !token.IsCancellationRequested)
        {
            var ev = events[eventIdx % events.Count];
            double beatIntervalMs = 60000.0 / _liveTempo;
            double eventMs = ev.DurationInBeats * beatIntervalMs;

            int currentBeat = (int)beatPos;
            if (currentBeat != lastReportedBeat)
            {
                BeatChanged?.Invoke(barIndex, Math.Min(currentBeat, beatsInBar - 1));
                lastReportedBeat = currentBeat;
            }

            var chord = bar.GetChordAtBeat(currentBeat);

            if (chord != null && ev.Type != StrumType.Rest)
            {
                if (ev.Articulation == StrumArticulation.Mute)
                    AllNotesOff();
                PlayStrum(chord, ev.Type == StrumType.Down, 90, usePower);

                if (ev.Articulation == StrumArticulation.Mute)
                {
                    // Muted: play for ~70% of duration then choke
                    WaitBeat(eventMs * 0.7, token);
                    AllNotesOff();
                    WaitBeat(eventMs * 0.3, token);
                }
                else
                {
                    // Ringing: sustain through the full duration
                    WaitBeat(eventMs, token);
                }
            }
            else
            {
                // Rest or no chord — just wait
                WaitBeat(eventMs, token);
            }

            beatPos += ev.DurationInBeats;
            eventIdx++;

            // If we've exhausted the pattern, wrap around
            if (eventIdx >= events.Count)
                eventIdx = 0;
        }
    }

    private void PlayStrum(Chord chord, bool downStrum, int velocity, bool usePower)
    {
        var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
        if (notes.Length == 0) return;
        if (!downStrum) notes = notes.Reverse().ToArray();

        double perNoteMs = StrumSpreadMs / Math.Max(1, notes.Length - 1);

        for (int i = 0; i < notes.Length; i++)
        {
            int note = notes[i];
            // Slight velocity ramp: first note strongest, trailing notes slightly softer
            int vel = Math.Clamp(velocity - i * 3, 40, 127);
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, vel, MidiChannel);
                _activeNotes.Add(note);
            }
            if (i < notes.Length - 1 && perNoteMs > 0.5)
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalMilliseconds < perNoteMs)
                    Thread.SpinWait(100);
            }
        }
    }

    private void PlayArpNote(Chord chord, bool ascending, int velocity, bool usePower, int subBeatIndex)
    {
        var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
        if (notes.Length == 0) return;
        if (!ascending) notes = notes.Reverse().ToArray();
        int noteIdx = subBeatIndex % notes.Length;
        int note = notes[noteIdx];
        if (note is >= 0 and <= 127)
        {
            SendNoteOn(note, velocity, MidiChannel);
            _activeNotes.Add(note);
        }
    }

    private static void WaitBeat(double milliseconds, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalMilliseconds < milliseconds)
        {
            if (token.IsCancellationRequested) return;
            double remaining = milliseconds - sw.Elapsed.TotalMilliseconds;
            if (remaining > 15)
                Thread.Sleep(8);
            else if (remaining > 1)
                Thread.SpinWait(1000);
            else
                break;
        }
    }

    private void PlayChord(Chord chord, int velocity, bool usePower)
    {
        var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
        foreach (var note in notes)
        {
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, velocity, MidiChannel);
                _activeNotes.Add(note);
            }
        }
    }

    private void PlayChordTones(Chord chord, int velocity)
    {
        int baseNote = chord.Root.ToMidiBase();
        int[] intervals = chord.Quality.GetIntervals();
        foreach (var interval in intervals)
        {
            int note = baseNote + interval;
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, velocity, MidiChannel);
                _activeNotes.Add(note);
            }
        }
    }

    private void PlayBassNote(Chord chord, int velocity)
    {
        int bassNote = (chord.BassNote ?? chord.Root).ToMidiBase() - 12;
        if (bassNote is >= 0 and <= 127)
        {
            SendNoteOn(bassNote, velocity, MidiChannel);
            _activeNotes.Add(bassNote);
        }
    }

    private void PlayBassAndRoot(Chord chord, int velocity)
    {
        int bassNote = (chord.BassNote ?? chord.Root).ToMidiBase() - 12;
        int rootNote = chord.Root.ToMidiBase();
        foreach (var note in new[] { bassNote, rootNote })
        {
            if (note is >= 0 and <= 127)
            {
                SendNoteOn(note, velocity, MidiChannel);
                _activeNotes.Add(note);
            }
        }
    }

    private void SendNoteOn(int note, int velocity, int channel)
    {
        if (_useSoundFont && _sfPlayer != null)
            _sfPlayer.NoteOn(channel - 1, note, velocity);
        else
            _midiOut?.Send(MidiMessage.StartNote(note, velocity, channel).RawData);
    }

    private void SendNoteOff(int note, int channel)
    {
        if (_useSoundFont && _sfPlayer != null)
            _sfPlayer.NoteOff(channel - 1, note);
        else
        {
            try { _midiOut?.Send(MidiMessage.StopNote(note, 0, channel).RawData); }
            catch { }
        }
    }

    private void AllNotesOff()
    {
        foreach (var note in _activeNotes)
            SendNoteOff(note, MidiChannel);
        _activeNotes.Clear();
        _sfPlayer?.NoteOffAll();
    }

    public void PlayChordPreview(Chord chord, Instrument? instrument)
    {
        var inst = instrument ?? Instrument.AcousticPiano;
        bool usePower = inst.UsePowerChords;

        if (_useSoundFont && _sfPlayer != null)
        {
            Task.Run(() =>
            {
                try
                {
                    _sfPlayer.StartAudio();
                    _sfPlayer.SetProgram(0, inst.MidiProgram);
                    var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
                    foreach (var note in notes)
                    {
                        if (note is >= 0 and <= 127)
                            _sfPlayer.NoteOn(0, note, 90);
                    }
                    Thread.Sleep(800);
                    foreach (var note in notes)
                    {
                        if (note is >= 0 and <= 127)
                            _sfPlayer.NoteOff(0, note);
                    }
                }
                catch { }
            });
        }
        else
        {
            Task.Run(() =>
            {
                MidiOut? previewOut = null;
                var previewNotes = new List<int>();
                try
                {
                    previewOut = new MidiOut(0);
                    previewOut.Send(MidiMessage.ChangePatch(inst.MidiProgram, MidiChannel).RawData);

                    var notes = usePower ? chord.GetPowerChordNotes() : chord.GetMidiNotes();
                    foreach (var note in notes)
                    {
                        if (note is >= 0 and <= 127)
                        {
                            previewOut.Send(MidiMessage.StartNote(note, 90, MidiChannel).RawData);
                            previewNotes.Add(note);
                        }
                    }
                    Thread.Sleep(800);
                    foreach (var note in previewNotes)
                    {
                        try { previewOut.Send(MidiMessage.StopNote(note, 0, MidiChannel).RawData); }
                        catch { }
                    }
                }
                catch { }
                finally
                {
                    try { previewOut?.Dispose(); } catch { }
                }
            });
        }
    }

    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            try { _playbackTask?.Wait(500); } catch { }
            _cts.Dispose();
            _cts = null;
        }

        AllNotesOff();

        if (_midiOut != null)
        {
            try { _midiOut.Dispose(); } catch { }
            _midiOut = null;
        }

        if (_useSoundFont && _sfPlayer != null)
            _sfPlayer.StopAudio();

        IsPlaying = false;
    }

    public void Dispose()
    {
        Stop();
        _sfPlayer?.Dispose();
    }
}
