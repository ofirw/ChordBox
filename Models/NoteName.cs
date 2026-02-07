namespace ChordBox.Models;

public enum NoteName
{
    C = 0, CSharp = 1, D = 2, EFlat = 3, E = 4, F = 5,
    FSharp = 6, G = 7, AFlat = 8, A = 9, BFlat = 10, B = 11
}

public static class NoteNameExtensions
{
    private static readonly string[] DisplayNames =
        ["C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

    public static string ToDisplayString(this NoteName note) =>
        DisplayNames[(int)note];

    public static int ToMidiBase(this NoteName note) => 48 + (int)note;
}
