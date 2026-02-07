namespace ChordBox.Models;

public enum StrumType
{
    Down,
    Up,
    Rest,
}

public enum StrumArticulation
{
    Ring,   // notes sustain across strums
    Mute,   // notes are choked before next strum
}

public class StrumEvent
{
    public StrumType Type { get; set; }
    public StrumArticulation Articulation { get; set; }
    public double DurationInBeats { get; set; } // 1.0 = quarter, 0.5 = eighth, 0.25 = sixteenth

    public StrumEvent(StrumType type, double durationInBeats, StrumArticulation articulation = StrumArticulation.Ring)
    {
        Type = type;
        DurationInBeats = durationInBeats;
        Articulation = articulation;
    }

    public StrumEvent() : this(StrumType.Down, 0.5) { }

    public string DisplayLabel
    {
        get
        {
            string dir = Type switch
            {
                StrumType.Down => "↓",
                StrumType.Up => "↑",
                StrumType.Rest => "—",
                _ => "?"
            };
            string dur = DurationInBeats switch
            {
                1.0 => "1/4",
                0.5 => "1/8",
                0.25 => "1/16",
                _ => $"{DurationInBeats}"
            };
            string art = (Type != StrumType.Rest && Articulation == StrumArticulation.Mute) ? " ✕" : "";
            return $"{dir}{dur}{art}";
        }
    }

    public StrumEvent Clone() => new(Type, DurationInBeats, Articulation);
}

public class StrumPattern
{
    public string Name { get; set; }
    public List<StrumEvent> Events { get; set; }
    public bool IsCustom { get; set; }

    public double TotalBeats => Events.Sum(e => e.DurationInBeats);

    public StrumPattern(string name, List<StrumEvent> events, bool isCustom = false)
    {
        Name = name;
        Events = events;
        IsCustom = isCustom;
    }

    public override string ToString() => Name;

    public StrumPattern Clone() => new(Name, Events.Select(e => e.Clone()).ToList(), true);

    // ═══════ Predefined patterns ═══════

    // Basic quarter-note downstrums: D D D D = 4 beats
    public static readonly StrumPattern BasicDown = new("Basic Down", [
        D(1.0), D(1.0), D(1.0), D(1.0)
    ]);

    // Eighth-note down-up: DU DU DU DU = 4 beats
    public static readonly StrumPattern EighthDownUp = new("Eighth Down-Up", [
        D(.5), U(.5), D(.5), U(.5), D(.5), U(.5), D(.5), U(.5)
    ]);

    // Classic folk DDU-UDU: D D U _ U D U _ = 4 beats
    public static readonly StrumPattern Folk = new("Folk (DDU-UDU)", [
        D(.5), D(.5), U(.5), R(.5), U(.5), D(.5), U(.5), R(.5)
    ]);

    // Pop ballad: D _ _ U D _ _ U = 4 beats
    public static readonly StrumPattern PopBallad = new("Pop Ballad", [
        D(.5), R(.25), R(.25), U(.5), D(.5), R(.25), R(.25), U(.5)
    ]);

    // Country boom-chicka: D _ DU D _ DU = 4 beats
    public static readonly StrumPattern Country = new("Country", [
        D(.5), R(.5), D(.25), U(.25), D(.5), R(.5), D(.25), U(.25)
    ]);

    // Reggae offbeat chop
    public static readonly StrumPattern ReggaeChop = new("Reggae Chop", [
        R(.5), Dm(.5), R(.5), R(.5), R(.5), Dm(.5), R(.5), R(.5)
    ]);

    // Punk eighth muted: Dm Dm Dm Dm Dm Dm Dm Dm = 4 beats
    public static readonly StrumPattern PunkMuted = new("Punk Muted", [
        Dm(.5), Dm(.5), Dm(.5), Dm(.5), Dm(.5), Dm(.5), Dm(.5), Dm(.5)
    ]);

    // Funk 16ths muted: Dm Dm Dm Um x4 = 4 beats
    public static readonly StrumPattern Funk16 = new("Funk 16ths", [
        Dm(.25), Dm(.25), Dm(.25), Um(.25),
        Dm(.25), Dm(.25), Dm(.25), Um(.25),
        Dm(.25), Dm(.25), Dm(.25), Um(.25),
        Dm(.25), Dm(.25), Dm(.25), Um(.25),
    ]);

    // Driving eighths (ring): D D D D D D D D = 4 beats
    public static readonly StrumPattern DrivingEighths = new("Driving Eighths", [
        D(.5), D(.5), D(.5), D(.5), D(.5), D(.5), D(.5), D(.5)
    ]);

    // Ska offbeat: _ U _ U _ U _ U = 4 beats
    public static readonly StrumPattern Ska = new("Ska Offbeat", [
        R(.5), Um(.5), R(.5), Um(.5), R(.5), Um(.5), R(.5), Um(.5)
    ]);

    // 16th mixed: D _ D U _ U D U = 2 beats (repeats)
    public static readonly StrumPattern Sixteenth = new("16th Pattern", [
        D(.25), R(.25), D(.25), U(.25), R(.25), U(.25), D(.25), U(.25)
    ]);

    public static readonly StrumPattern[] AllPatterns =
    [
        BasicDown, EighthDownUp, Folk, PopBallad, Country,
        DrivingEighths, ReggaeChop, Ska,
        PunkMuted, Funk16, Sixteenth,
    ];

    // ── shorthand helpers ──
    private static StrumEvent D(double dur) => new(StrumType.Down, dur, StrumArticulation.Ring);
    private static StrumEvent U(double dur) => new(StrumType.Up, dur, StrumArticulation.Ring);
    private static StrumEvent Dm(double dur) => new(StrumType.Down, dur, StrumArticulation.Mute);
    private static StrumEvent Um(double dur) => new(StrumType.Up, dur, StrumArticulation.Mute);
    private static StrumEvent R(double dur) => new(StrumType.Rest, dur);
}
