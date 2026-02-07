namespace ChordBox.Models;

public static class ChordParser
{
    private static readonly (string text, NoteName note)[] RootMappings =
    [
        ("c#", NoteName.CSharp),
        ("db", NoteName.CSharp),
        ("d",  NoteName.D),
        ("eb", NoteName.EFlat),
        ("e",  NoteName.E),
        ("f#", NoteName.FSharp),
        ("gb", NoteName.FSharp),
        ("f",  NoteName.F),
        ("g#", NoteName.AFlat),
        ("ab", NoteName.AFlat),
        ("g",  NoteName.G),
        ("a#", NoteName.BFlat),
        ("bb", NoteName.BFlat),
        ("a",  NoteName.A),
        ("b",  NoteName.B),
        ("c",  NoteName.C),
    ];

    private static readonly (string suffix, ChordQuality quality)[] QualityMappings =
    [
        ("maj7", ChordQuality.Major7),
        ("sus2", ChordQuality.Sus2),
        ("sus4", ChordQuality.Sus4),
        ("dim",  ChordQuality.Diminished),
        ("aug",  ChordQuality.Augmented),
        ("m7",   ChordQuality.Minor7),
        ("m",    ChordQuality.Minor),
        ("7",    ChordQuality.Dominant7),
        ("",     ChordQuality.Major),
    ];

    /// <summary>
    /// Try to parse a chord string like "am7", "F#dim", "Bb", "c", etc.
    /// Returns true if a valid chord was found.
    /// </summary>
    public static bool TryParse(string input, out Chord? chord)
    {
        chord = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string lower = input.Trim().ToLowerInvariant();

        // Split off optional bass note: "cm7/e" → "cm7" + "e"
        string mainPart = lower;
        string? bassPart = null;
        int slashIdx = lower.IndexOf('/');
        if (slashIdx > 0 && slashIdx < lower.Length - 1)
        {
            mainPart = lower[..slashIdx];
            bassPart = lower[(slashIdx + 1)..];
        }

        foreach (var (rootText, note) in RootMappings)
        {
            if (!mainPart.StartsWith(rootText)) continue;

            string remainder = mainPart[rootText.Length..];

            foreach (var (suffix, quality) in QualityMappings)
            {
                if (remainder == suffix)
                {
                    NoteName? bassNote = null;
                    if (bassPart != null)
                    {
                        if (!TryParseNoteName(bassPart, out var bn))
                            return false;
                        bassNote = bn;
                    }
                    chord = new Chord(note, quality, bassNote);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryParseNoteName(string text, out NoteName note)
    {
        foreach (var (rootText, n) in RootMappings)
        {
            if (text == rootText)
            {
                note = n;
                return true;
            }
        }
        note = default;
        return false;
    }

    /// <summary>
    /// Check if the input could potentially become a valid chord with more characters.
    /// Used to determine if input is "in progress" vs "invalid".
    /// </summary>
    public static bool IsPartialMatch(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;

        string lower = input.Trim().ToLowerInvariant();

        foreach (var (rootText, _) in RootMappings)
        {
            // Input could be partial root
            if (rootText.StartsWith(lower)) return true;

            if (!lower.StartsWith(rootText)) continue;

            string remainder = lower[rootText.Length..];
            if (remainder.Length == 0) return true;

            foreach (var (suffix, _) in QualityMappings)
            {
                if (suffix.Length > 0 && suffix.StartsWith(remainder))
                    return true;
            }
        }

        return false;
    }
}
