namespace ChordBox.Models;

public class Bar
{
    public int BeatsPerBarOverride { get; set; } // 0 = use global default
    public string Lyrics { get; set; } = "";
    public List<ChordEvent> ChordEvents { get; set; } = new();

    public int GetEffectiveBeatsPerBar(int globalBeats) =>
        BeatsPerBarOverride > 0 ? BeatsPerBarOverride : globalBeats;

    public Chord? GetChordAtBeat(int beat)
    {
        return ChordEvents
            .FirstOrDefault(e => beat >= e.StartBeat && beat < e.StartBeat + e.DurationBeats)
            ?.Chord;
    }

    public void SetSingleChord(Chord? chord)
    {
        ChordEvents.Clear();
        if (chord != null)
        {
            ChordEvents.Add(new ChordEvent(chord, 0, 4));
        }
    }

    public void SetChordAtBeat(int beat, Chord? chord)
    {
        ExpandToPerBeat();
        ChordEvents.RemoveAll(e => e.StartBeat == beat);
        if (chord != null)
        {
            ChordEvents.Add(new ChordEvent(chord, beat, 1));
        }
        ChordEvents.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
    }

    public void ClearBeat(int beat)
    {
        ExpandToPerBeat();
        ChordEvents.RemoveAll(e => e.StartBeat == beat);
        ChordEvents.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
    }

    private void ExpandToPerBeat()
    {
        if (ChordEvents.Count == 1 && ChordEvents[0].DurationBeats > 1)
        {
            var original = ChordEvents[0];
            ChordEvents.Clear();
            for (int b = original.StartBeat; b < original.StartBeat + original.DurationBeats && b < 7; b++)
            {
                ChordEvents.Add(new ChordEvent(original.Chord, b, 1));
            }
        }
    }

    public Chord? SingleChord => ChordEvents.Count > 0 ? ChordEvents[0].Chord : null;

    public bool HasMultipleChords
    {
        get
        {
            var distinct = ChordEvents.Select(e => e.Chord).Distinct().ToList();
            return distinct.Count > 1;
        }
    }

    public bool IsEmpty => ChordEvents.Count == 0;
}
