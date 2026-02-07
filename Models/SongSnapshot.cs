namespace ChordBox.Models;

/// <summary>
/// Lightweight snapshot of the song state for undo/redo.
/// </summary>
public class SongSnapshot
{
    public List<BarSnapshot> Bars { get; set; } = new();
    public List<LoopSnapshot> Loops { get; set; } = new();

    public static SongSnapshot Capture(IEnumerable<BarSnapshot> bars, IEnumerable<LoopSnapshot> loops)
    {
        return new SongSnapshot
        {
            Bars = bars.ToList(),
            Loops = loops.ToList(),
        };
    }
}

public class BarSnapshot
{
    public int BeatsPerBarOverride { get; set; }
    public string Lyrics { get; set; } = "";
    public List<ChordEventSnapshot> ChordEvents { get; set; } = new();
}

public class ChordEventSnapshot
{
    public NoteName Root { get; set; }
    public ChordQuality Quality { get; set; }
    public NoteName? BassNote { get; set; }
    public int StartBeat { get; set; }
    public int DurationBeats { get; set; }
}

public class LoopSnapshot
{
    public string Name { get; set; } = "";
    public int StartBarIndex { get; set; }
    public int EndBarIndex { get; set; }
    public int RepeatCount { get; set; }
    public int ColorIndex { get; set; }
    public SectionType SectionType { get; set; }
}
