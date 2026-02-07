namespace ChordBox.Models;

public enum ScaleType { Major, Minor }

public class ScaleChordInfo
{
    public Chord Chord { get; }
    public string Degree { get; }
    public string Display => Chord.DisplayName;

    public ScaleChordInfo(Chord chord, string degree)
    {
        Chord = chord;
        Degree = degree;
    }

    public override string ToString() => $"{Degree} {Display}";
}

public class ScaleChordGroup
{
    public string GroupName { get; }
    public List<ScaleChordInfo> Chords { get; }

    public ScaleChordGroup(string groupName, List<ScaleChordInfo> chords)
    {
        GroupName = groupName;
        Chords = chords;
    }
}

public static class ScaleHelper
{
    private static readonly int[] MajorIntervals = [0, 2, 4, 5, 7, 9, 11];
    private static readonly int[] MinorIntervals = [0, 2, 3, 5, 7, 8, 10];

    private static readonly ChordQuality[] MajorQualities =
        [ChordQuality.Major, ChordQuality.Minor, ChordQuality.Minor,
         ChordQuality.Major, ChordQuality.Major, ChordQuality.Minor, ChordQuality.Diminished];

    private static readonly string[] MajorDegrees = ["I", "ii", "iii", "IV", "V", "vi", "vii°"];

    private static readonly ChordQuality[] MinorQualities =
        [ChordQuality.Minor, ChordQuality.Diminished, ChordQuality.Major,
         ChordQuality.Minor, ChordQuality.Minor, ChordQuality.Major, ChordQuality.Major];

    private static readonly string[] MinorDegrees = ["i", "ii°", "III", "iv", "v", "VI", "VII"];

    public static List<ScaleChordInfo> GetDiatonicChords(NoteName key, ScaleType scale)
    {
        var intervals = scale == ScaleType.Major ? MajorIntervals : MinorIntervals;
        var qualities = scale == ScaleType.Major ? MajorQualities : MinorQualities;
        var degrees = scale == ScaleType.Major ? MajorDegrees : MinorDegrees;

        var result = new List<ScaleChordInfo>();
        for (int i = 0; i < 7; i++)
        {
            int noteValue = ((int)key + intervals[i]) % 12;
            var root = (NoteName)noteValue;
            result.Add(new ScaleChordInfo(new Chord(root, qualities[i]), degrees[i]));
        }
        return result;
    }

    /// <summary>
    /// Get all chord groups: diatonic triads, 7th chords, suspended, secondary dominants, borrowed chords.
    /// </summary>
    public static List<ScaleChordGroup> GetAllChordGroups(NoteName key, ScaleType scale)
    {
        var intervals = scale == ScaleType.Major ? MajorIntervals : MinorIntervals;
        var degrees = scale == ScaleType.Major ? MajorDegrees : MinorDegrees;
        var groups = new List<ScaleChordGroup>();

        // 1. Diatonic triads
        groups.Add(new ScaleChordGroup("Diatonic Triads", GetDiatonicChords(key, scale)));

        // 2. Diatonic 7th chords
        var maj7Degrees = scale == ScaleType.Major
            ? new[] { ("Imaj7", 0, ChordQuality.Major7), ("ii7", 1, ChordQuality.Minor7), ("iii7", 2, ChordQuality.Minor7),
                      ("IVmaj7", 3, ChordQuality.Major7), ("V7", 4, ChordQuality.Dominant7), ("vi7", 5, ChordQuality.Minor7) }
            : new[] { ("i7", 0, ChordQuality.Minor7), ("IIImaj7", 2, ChordQuality.Major7),
                      ("iv7", 3, ChordQuality.Minor7), ("v7", 4, ChordQuality.Minor7),
                      ("VImaj7", 5, ChordQuality.Major7), ("VII7", 6, ChordQuality.Dominant7) };
        var sevenths = new List<ScaleChordInfo>();
        foreach (var (deg, idx, q) in maj7Degrees)
        {
            int noteVal = ((int)key + intervals[idx]) % 12;
            sevenths.Add(new ScaleChordInfo(new Chord((NoteName)noteVal, q), deg));
        }
        groups.Add(new ScaleChordGroup("7th Chords", sevenths));

        // 3. Suspended chords (sus2, sus4 on each scale degree)
        var sus = new List<ScaleChordInfo>();
        for (int i = 0; i < 7; i++)
        {
            int noteVal = ((int)key + intervals[i]) % 12;
            var root = (NoteName)noteVal;
            sus.Add(new ScaleChordInfo(new Chord(root, ChordQuality.Sus2), $"{degrees[i]}sus2"));
            sus.Add(new ScaleChordInfo(new Chord(root, ChordQuality.Sus4), $"{degrees[i]}sus4"));
        }
        groups.Add(new ScaleChordGroup("Suspended", sus));

        // 4. Secondary dominants (V7/x for each diatonic chord except I/i)
        var secDom = new List<ScaleChordInfo>();
        for (int i = 1; i < 7; i++)
        {
            // The dominant of degree i is a perfect 5th above it
            int targetNote = ((int)key + intervals[i]) % 12;
            int domRoot = (targetNote + 7) % 12; // perfect 5th above
            string label = scale == ScaleType.Major ? $"V7/{MajorDegrees[i]}" : $"V7/{MinorDegrees[i]}";
            secDom.Add(new ScaleChordInfo(new Chord((NoteName)domRoot, ChordQuality.Dominant7), label));
        }
        groups.Add(new ScaleChordGroup("Secondary Dominants", secDom));

        // 5. Borrowed chords (from parallel major/minor)
        var borrowed = new List<ScaleChordInfo>();
        var parallelScale = scale == ScaleType.Major ? ScaleType.Minor : ScaleType.Major;
        var parallelChords = GetDiatonicChords(key, parallelScale);
        var diatonicSet = new HashSet<(NoteName, ChordQuality)>(
            GetDiatonicChords(key, scale).Select(c => (c.Chord.Root, c.Chord.Quality)));
        foreach (var pc in parallelChords)
        {
            if (!diatonicSet.Contains((pc.Chord.Root, pc.Chord.Quality)))
                borrowed.Add(new ScaleChordInfo(pc.Chord, $"♭{pc.Degree}"));
        }
        if (borrowed.Count > 0)
        {
            string parallelLabel = parallelScale == ScaleType.Minor ? "Borrowed (from minor)" : "Borrowed (from major)";
            groups.Add(new ScaleChordGroup(parallelLabel, borrowed));
        }

        // 6. Diminished & Augmented
        var dimAug = new List<ScaleChordInfo>();
        for (int i = 0; i < 7; i++)
        {
            int noteVal = ((int)key + intervals[i]) % 12;
            var root = (NoteName)noteVal;
            dimAug.Add(new ScaleChordInfo(new Chord(root, ChordQuality.Diminished), $"{degrees[i]}dim"));
            dimAug.Add(new ScaleChordInfo(new Chord(root, ChordQuality.Augmented), $"{degrees[i]}aug"));
        }
        groups.Add(new ScaleChordGroup("Diminished & Augmented", dimAug));

        return groups;
    }

    public static (NoteName Key, ScaleType Scale) DetectKey(IEnumerable<Chord> chords)
    {
        var chordList = chords.ToList();
        if (chordList.Count == 0)
            return (NoteName.C, ScaleType.Major);

        NoteName bestKey = NoteName.C;
        ScaleType bestScale = ScaleType.Major;
        double bestScore = double.MinValue;

        foreach (NoteName key in Enum.GetValues<NoteName>())
        {
            foreach (ScaleType scale in Enum.GetValues<ScaleType>())
            {
                double score = ScoreKey(key, scale, chordList);
                if (score > bestScore || (score == bestScore && scale == ScaleType.Major))
                {
                    bestScore = score;
                    bestKey = key;
                    bestScale = scale;
                }
            }
        }

        return (bestKey, bestScale);
    }

    private static double ScoreKey(NoteName key, ScaleType scale, List<Chord> chords)
    {
        var intervals = scale == ScaleType.Major ? MajorIntervals : MinorIntervals;
        var qualities = scale == ScaleType.Major ? MajorQualities : MinorQualities;

        // Build diatonic chord lookup: semitone offset → expected quality
        var diatonic = new Dictionary<int, ChordQuality>();
        for (int i = 0; i < 7; i++)
            diatonic[intervals[i]] = qualities[i];

        // Function scores by scale degree
        // I=+5, V=+4, IV=+3, vi/ii=+2 (major); i=+5, v=+4, iv=+3, III/VII=+2 (minor)
        var degreeScores = scale == ScaleType.Major
            ? new Dictionary<int, double>
              { [0]=5, [1]=2, [2]=1.5, [3]=3, [4]=4, [5]=2, [6]=0.5 }
            : new Dictionary<int, double>
              { [0]=5, [1]=0.5, [2]=2, [3]=3, [4]=4, [5]=2, [6]=1.5 };

        // Build secondary dominant set: V7 of each diatonic degree (except tonic)
        var secDomRoots = new HashSet<int>();
        for (int i = 1; i < 7; i++)
        {
            int target = ((int)key + intervals[i]) % 12;
            int domRoot = (target + 7) % 12; // perfect 5th above
            secDomRoots.Add(domRoot);
        }

        double total = 0;
        for (int pos = 0; pos < chords.Count; pos++)
        {
            var chord = chords[pos];
            int semitone = ((int)chord.Root - (int)key + 12) % 12;

            // Positional weight: first chord ×1.5, last chord ×2.0
            double posWeight = 1.0;
            if (pos == 0) posWeight = 1.5;
            if (pos == chords.Count - 1) posWeight = 2.0;

            // Check diatonic match
            bool matched = false;
            for (int deg = 0; deg < 7; deg++)
            {
                if (intervals[deg] == semitone)
                {
                    if (chord.Quality == qualities[deg])
                    {
                        // Exact diatonic match
                        total += degreeScores[deg] * posWeight;
                    }
                    else
                    {
                        // Root matches but wrong quality (e.g. minor where major expected)
                        total += 0.5 * posWeight;
                    }
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                // Secondary dominant bonus
                if (chord.Quality == ChordQuality.Dominant7 && secDomRoots.Contains((int)chord.Root))
                    total += 3.0 * posWeight;
                else
                    // Non-functional chord penalty
                    total -= 1.0 * posWeight;
            }
        }

        return total;
    }
}
