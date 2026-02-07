using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChordBox.Models;

public class SongFile
{
    public string Title { get; set; } = "Untitled";
    public int Tempo { get; set; } = 120;
    public string StyleName { get; set; } = "Pop";
    public string InstrumentName { get; set; } = "Acoustic Piano";
    public int GlobalBeatsPerBar { get; set; } = 4;
    public List<BarData> Bars { get; set; } = new();
    public List<LoopData> Loops { get; set; } = new();
    public string? StrumPatternName { get; set; }
    public StrumPatternData? CustomStrumPattern { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static SongFile? FromJson(string json) =>
        JsonSerializer.Deserialize<SongFile>(json, JsonOptions);
}

public class BarData
{
    public int BeatsPerBarOverride { get; set; }
    public string Lyrics { get; set; } = "";
    public List<ChordEventData> ChordEvents { get; set; } = new();
}

public class ChordEventData
{
    public string Root { get; set; } = "C";
    public string Quality { get; set; } = "Major";
    public string? BassNote { get; set; }
    public int StartBeat { get; set; }
    public int DurationBeats { get; set; } = 4;
}

public class LoopData
{
    public string Name { get; set; } = "";
    public int StartBarIndex { get; set; }
    public int EndBarIndex { get; set; }
    public int RepeatCount { get; set; } = 2;
    public string SectionType { get; set; } = "None";
}

public class StrumPatternData
{
    public string Name { get; set; } = "Custom";
    public List<StrumEventData> Events { get; set; } = new();
}

public class StrumEventData
{
    public string Type { get; set; } = "Down";
    public string Articulation { get; set; } = "Ring";
    public double DurationInBeats { get; set; } = 0.5;
}
