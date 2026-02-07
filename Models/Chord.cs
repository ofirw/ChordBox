namespace ChordBox.Models;

public class Chord
{
    public NoteName Root { get; }
    public ChordQuality Quality { get; }
    public NoteName? BassNote { get; }

    public Chord(NoteName root, ChordQuality quality, NoteName? bassNote = null)
    {
        Root = root;
        Quality = quality;
        BassNote = bassNote;
    }

    public string DisplayName =>
        BassNote.HasValue
            ? Root.ToDisplayString() + Quality.ToSuffix() + "/" + BassNote.Value.ToDisplayString()
            : Root.ToDisplayString() + Quality.ToSuffix();

    public int[] GetMidiNotes()
    {
        int bassNote = BassNote.HasValue
            ? BassNote.Value.ToMidiBase() - 12
            : Root.ToMidiBase() - 12;
        int baseNote = Root.ToMidiBase();       // Chord tones in octave 3
        int[] intervals = Quality.GetIntervals();

        var notes = new List<int> { bassNote };
        foreach (var interval in intervals)
        {
            notes.Add(baseNote + interval);
        }
        notes.Add(baseNote + 12); // Root doubled an octave up
        return notes.ToArray();
    }

    public int[] GetPowerChordNotes()
    {
        int root = BassNote.HasValue
            ? BassNote.Value.ToMidiBase() - 12
            : Root.ToMidiBase() - 12;
        int fifth = root + 7;
        int octave = root + 12;
        return [root, fifth, octave];
    }

    public override bool Equals(object? obj) =>
        obj is Chord other && Root == other.Root && Quality == other.Quality && BassNote == other.BassNote;

    public override int GetHashCode() => HashCode.Combine(Root, Quality, BassNote);
}
