namespace ChordBox.Models;

public enum ChordQuality
{
    Major, Minor, Dominant7, Major7, Minor7, Diminished, Augmented, Sus2, Sus4
}

public static class ChordQualityExtensions
{
    public static string ToSuffix(this ChordQuality quality) => quality switch
    {
        ChordQuality.Major => "",
        ChordQuality.Minor => "m",
        ChordQuality.Dominant7 => "7",
        ChordQuality.Major7 => "maj7",
        ChordQuality.Minor7 => "m7",
        ChordQuality.Diminished => "dim",
        ChordQuality.Augmented => "aug",
        ChordQuality.Sus2 => "sus2",
        ChordQuality.Sus4 => "sus4",
        _ => ""
    };

    public static string ToDisplayLabel(this ChordQuality quality) => quality switch
    {
        ChordQuality.Major => "Maj",
        ChordQuality.Minor => "min",
        ChordQuality.Dominant7 => "7",
        ChordQuality.Major7 => "maj7",
        ChordQuality.Minor7 => "min7",
        ChordQuality.Diminished => "dim",
        ChordQuality.Augmented => "aug",
        ChordQuality.Sus2 => "sus2",
        ChordQuality.Sus4 => "sus4",
        _ => ""
    };

    public static int[] GetIntervals(this ChordQuality quality) => quality switch
    {
        ChordQuality.Major => [0, 4, 7],
        ChordQuality.Minor => [0, 3, 7],
        ChordQuality.Dominant7 => [0, 4, 7, 10],
        ChordQuality.Major7 => [0, 4, 7, 11],
        ChordQuality.Minor7 => [0, 3, 7, 10],
        ChordQuality.Diminished => [0, 3, 6],
        ChordQuality.Augmented => [0, 4, 8],
        ChordQuality.Sus2 => [0, 2, 7],
        ChordQuality.Sus4 => [0, 5, 7],
        _ => [0, 4, 7]
    };
}
