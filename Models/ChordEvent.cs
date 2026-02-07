namespace ChordBox.Models;

public class ChordEvent
{
    public Chord Chord { get; set; }
    public int StartBeat { get; set; }
    public int DurationBeats { get; set; }

    public ChordEvent(Chord chord, int startBeat = 0, int durationBeats = 4)
    {
        Chord = chord;
        StartBeat = startBeat;
        DurationBeats = durationBeats;
    }
}
