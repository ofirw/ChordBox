namespace ChordBox.Models;

public enum SectionType
{
    None,       // Regular loop (no section label)
    Intro,
    Verse,
    PreChorus,
    Chorus,
    Bridge,
    Outro,
    Solo,
    Interlude,
    Custom      // Uses the Name field as custom label
}

public class LoopRegion
{
    public string Name { get; set; }
    public int StartBarIndex { get; set; }
    public int EndBarIndex { get; set; }
    public int RepeatCount { get; set; }
    public int ColorIndex { get; set; }
    public SectionType SectionType { get; set; }

    /// <summary>
    /// True if this is a section marker (may or may not also loop).
    /// A section with RepeatCount &lt;= 1 is a label-only marker.
    /// </summary>
    public bool IsSection => SectionType != SectionType.None;
    public bool IsLoop => RepeatCount > 1;

    public string DisplayLabel
    {
        get
        {
            if (SectionType != SectionType.None && SectionType != SectionType.Custom)
                return string.IsNullOrWhiteSpace(Name) ? SectionType.ToString() : $"{SectionType}: {Name}";
            return Name;
        }
    }

    public LoopRegion(int startBarIndex, int endBarIndex, int repeatCount = 2, int colorIndex = 0, string name = "", SectionType sectionType = SectionType.None)
    {
        StartBarIndex = startBarIndex;
        EndBarIndex = endBarIndex;
        RepeatCount = repeatCount;
        ColorIndex = colorIndex;
        Name = name;
        SectionType = sectionType;
    }

    public bool ContainsBar(int barIndex) => barIndex >= StartBarIndex && barIndex <= EndBarIndex;

    public bool FullyContains(LoopRegion other) =>
        StartBarIndex <= other.StartBarIndex && EndBarIndex >= other.EndBarIndex;

    public bool IsPartialOverlap(int startBar, int endBar)
    {
        bool overlaps = startBar <= EndBarIndex && endBar >= StartBarIndex;
        if (!overlaps) return false;
        bool oneContainsOther = (startBar <= StartBarIndex && endBar >= EndBarIndex)
                             || (StartBarIndex <= startBar && EndBarIndex >= endBar);
        return !oneContainsOther;
    }
}
