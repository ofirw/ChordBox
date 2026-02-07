namespace ChordBox.Models;

// Each slot = one eighth note.  4/4 bar → 8 slots: "1 & 2 & 3 & 4 &"
// Even indices (0,2,4,6) = on-beat, Odd indices (1,3,5,7) = off-beat ("and")
public enum BeatAction
{
    Rest,
    FullChord,
    ChordTones,
    BassOnly,
    BassAndRoot,
    ArpUp,        // play chord notes ascending one per sub-beat
    ArpDown,      // play chord notes descending one per sub-beat
    TripletArp,   // bass-note-note triplet filling the whole beat
}

public class PlayStyle
{
    public string Name { get; }
    public string Category { get; }
    /// <summary>Eighth-note resolution: 2 slots per beat. Length = 2 × beatsPerBar.</summary>
    public BeatAction[] SubBeatActions { get; }
    public int[] Velocities { get; }
    public double[] GateFractions { get; }

    public PlayStyle(string name, string category, BeatAction[] subBeatActions, int[] velocities, double[]? gateFractions = null)
    {
        Name = name;
        Category = category;
        SubBeatActions = subBeatActions;
        Velocities = velocities;
        GateFractions = gateFractions ?? Enumerable.Repeat(1.0, subBeatActions.Length).ToArray();
    }

    public override string ToString() => Name;

    // ════════════════════════════════════════════
    // Notation:  8 slots = "1 & 2 & 3 & 4 &"
    //            index:     0 1 2 3 4 5 6 7
    // ════════════════════════════════════════════

    // ── Basic ──
    public static readonly PlayStyle WholeNotes = S("Whole Notes", "Basic",
        //                  1   &   2   &   3   &   4   &
        [FC, _,  _,  _,  _,  _,  _,  _ ],
        [95, 0,  0,  0,  0,  0,  0,  0 ]);

    public static readonly PlayStyle HalfNotes = S("Half Notes", "Basic",
        [FC, _,  _,  _,  FC, _,  _,  _ ],
        [90, 0,  0,  0,  80, 0,  0,  0 ]);

    public static readonly PlayStyle Ballad = S("Ballad", "Basic",
        [FC, _,  _,  CT, _,  _,  CT, _ ],
        [88, 0,  0,  60, 0,  0,  55, 0 ]);

    // ── Pop ──
    public static readonly PlayStyle PopLight = S("Pop Light", "Pop",
        [FC, _,  CT, _,  FC, _,  CT, _ ],
        [90, 0,  55, 0,  80, 0,  55, 0 ],
        [.9, 1,  .4, 1,  .9, 1,  .4, 1 ]);

    public static readonly PlayStyle Pop = S("Pop", "Pop",
        [FC, CT, CT, _,  FC, CT, CT, _ ],
        [90, 55, 58, 0,  82, 55, 58, 0 ],
        [.8, .4, .4, 1,  .8, .4, .4, 1 ]);

    public static readonly PlayStyle PopDriving = S("Pop Driving", "Pop",
        [FC, CT, FC, CT, FC, CT, FC, CT],
        [95, 60, 85, 58, 90, 60, 85, 58],
        [.8, .4, .8, .4, .8, .4, .8, .4]);

    public static readonly PlayStyle PopSyncopated = S("Pop Syncopated", "Pop",
        [FC, _,  _,  FC, _,  FC, _,  _ ],
        [90, 0,  0,  75, 0,  80, 0,  0 ]);

    // ── Rock ──
    public static readonly PlayStyle RockSteady = S("Rock Steady", "Rock",
        [FC, _,  FC, _,  FC, _,  FC, _ ],
        [100,0,  90, 0,  95, 0,  90, 0 ],
        [.9, 1,  .75,1,  .9, 1,  .75,1 ]);

    public static readonly PlayStyle RockEighths = S("Rock Eighths", "Rock",
        [FC, FC, FC, FC, FC, FC, FC, FC],
        [100,80, 90, 78, 95, 80, 90, 78],
        [.85,.7, .85,.7, .85,.7, .85,.7]);

    public static readonly PlayStyle RockDriving = S("Rock Driving", "Rock",
        [FC, CT, FC, CT, FC, FC, FC, CT],
        [105,75, 95, 72, 100,90, 95, 72],
        [.8, .5, .8, .5, .8, .7, .8, .5]);

    public static readonly PlayStyle RockPunchy = S("Rock Punchy", "Rock",
        [FC, _,  FC, FC, _,  FC, FC, _ ],
        [110,0,  95, 90, 0,  95, 90, 0 ],
        [.6, 1,  .5, .5, 1,  .5, .5, 1 ]);

    public static readonly PlayStyle HardRock = S("Hard Rock", "Rock",
        [FC, FC, FC, FC, FC, FC, FC, FC],
        [115,95, 110,92, 112,95, 108,92],
        [.7, .6, .7, .6, .7, .6, .7, .6]);

    public static readonly PlayStyle Country = S("Country", "Rock",
        [BO, _,  FC, _,  BR, _,  FC, _ ],
        [90, 0,  75, 0,  88, 0,  75, 0 ],
        [.8, 1,  .5, 1,  .8, 1,  .5, 1 ]);

    public static readonly PlayStyle CountryTrain = S("Country Train", "Rock",
        [BO, CT, FC, CT, BR, CT, FC, CT],
        [90, 60, 75, 58, 88, 60, 75, 58],
        [.7, .4, .5, .4, .7, .4, .5, .4]);

    // ── Metal ──
    public static readonly PlayStyle Metal = S("Metal", "Metal",
        [FC, FC, FC, FC, FC, FC, FC, FC],
        [120,100,115,98, 118,100,115,98],
        [.45,.45,.45,.45,.45,.45,.45,.45]);

    public static readonly PlayStyle MetalPalmMute = S("Metal Palm Mute", "Metal",
        [BR, BR, BR, BR, FC, _,  BR, BR],
        [110,90, 105,88, 120,0,  105,88],
        [.3, .3, .3, .3, .6, 1,  .3, .3]);

    public static readonly PlayStyle MetalGallop = S("Metal Gallop", "Metal",
        [FC, _,  FC, FC, FC, _,  FC, FC],
        [120,0,  95, 100,118,0,  95, 100],
        [.5, 1,  .3, .4, .5, 1,  .3, .4]);

    public static readonly PlayStyle MetalThrash = S("Metal Thrash", "Metal",
        [FC, FC, FC, FC, FC, FC, FC, FC],
        [127,105,120,105,125,105,120,105],
        [.35,.35,.35,.35,.35,.35,.35,.35]);

    public static readonly PlayStyle MetalMelodic = S("Metal Melodic", "Metal",
        [FC, _,  CT, _,  FC, CT, _,  CT],
        [110,0,  75, 0,  105,80, 0,  72],
        [.8, 1,  .6, 1,  .8, .5, 1,  .5]);

    // ── Jazz / Latin ──
    public static readonly PlayStyle Jazz = S("Jazz Comping", "Jazz / Latin",
        [_,  _,  FC, _,  _,  CT, _,  _ ],
        [0,  0,  82, 0,  0,  72, 0,  0 ],
        [1,  1,  .5, 1,  1,  .45,1,  1 ]);

    public static readonly PlayStyle JazzSwing = S("Jazz Swing", "Jazz / Latin",
        [FC, _,  _,  CT, FC, _,  _,  CT],
        [85, 0,  0,  68, 80, 0,  0,  65],
        [.7, 1,  1,  .4, .7, 1,  1,  .4]);

    public static readonly PlayStyle BossaNova = S("Bossa Nova", "Jazz / Latin",
        [FC, _,  _,  CT, _,  CT, _,  _ ],
        [85, 0,  0,  68, 0,  65, 0,  0 ],
        [.75,1,  1,  .4, 1,  .4, 1,  1 ]);

    public static readonly PlayStyle Reggae = S("Reggae", "Jazz / Latin",
        [_,  FC, _,  _,  _,  FC, _,  _ ],
        [0,  90, 0,  0,  0,  88, 0,  0 ],
        [1,  .35,1,  1,  1,  .35,1,  1 ]);

    public static readonly PlayStyle ReggaeSka = S("Ska", "Jazz / Latin",
        [_,  FC, _,  FC, _,  FC, _,  FC],
        [0,  88, 0,  85, 0,  88, 0,  85],
        [1,  .3, 1,  .3, 1,  .3, 1,  .3]);

    public static readonly PlayStyle Funk = S("Funk", "Jazz / Latin",
        [FC, _,  _,  FC, _,  FC, _,  CT],
        [100,0,  0,  90, 0,  92, 0,  65],
        [.5, 1,  1,  .4, 1,  .4, 1,  .3]);

    public static readonly PlayStyle FunkTight = S("Funk Tight", "Jazz / Latin",
        [FC, CT, _,  FC, CT, _,  FC, CT],
        [100,65, 0,  88, 62, 0,  92, 65],
        [.35,.3, 1,  .35,.3, 1,  .35,.3]);

    // ── Arpeggio (broken chord in eighths) ──
    public static readonly PlayStyle ArpEighthsUp = S("Arp Eighths Up", "Arpeggio",
        [AU, AU, AU, AU, AU, AU, AU, AU],
        [85, 70, 72, 68, 82, 70, 72, 68],
        [.9, .9, .9, .9, .9, .9, .9, .9]);

    public static readonly PlayStyle ArpEighthsDown = S("Arp Eighths Down", "Arpeggio",
        [AD, AD, AD, AD, AD, AD, AD, AD],
        [85, 70, 72, 68, 82, 70, 72, 68],
        [.9, .9, .9, .9, .9, .9, .9, .9]);

    public static readonly PlayStyle ArpUpDown = S("Arp Up-Down", "Arpeggio",
        [AU, AU, AU, AU, AD, AD, AD, AD],
        [85, 70, 72, 75, 82, 70, 72, 68],
        [.9, .9, .9, .9, .9, .9, .9, .9]);

    public static readonly PlayStyle ArpBassUp = S("Arp Bass + Up", "Arpeggio",
        [BO, _,  AU, AU, AU, AU, AU, AU],
        [90, 0,  72, 68, 70, 72, 68, 65],
        [.9, 1,  .9, .9, .9, .9, .9, .9]);

    public static readonly PlayStyle ArpMelodic = S("Arp Melodic", "Arpeggio",
        [AU, _,  AU, AU, _,  AU, AU, _ ],
        [85, 0,  72, 75, 0,  72, 68, 0 ],
        [.85,1,  .85,.85,1,  .85,.85,1 ]);

    // ── Triplet Arpeggio (bass-note-note, 3 per beat) ──
    // TripletArp on the on-beat fills the whole beat; off-beat slot is ignored.
    public static readonly PlayStyle TripletArpUp = S("Triplet Arp Up", "Arpeggio",
        [TA, _,  TA, _,  TA, _,  TA, _ ],
        [85, 0,  80, 0,  82, 0,  78, 0 ]);

    public static readonly PlayStyle TripletArpBallad = S("Triplet Ballad", "Arpeggio",
        [TA, _,  _,  _,  TA, _,  _,  _ ],
        [88, 0,  0,  0,  82, 0,  0,  0 ]);

    public static readonly PlayStyle TripletArpDriving = S("Triplet Driving", "Arpeggio",
        [TA, AU, TA, AU, TA, AU, TA, AU],
        [90, 65, 85, 62, 88, 65, 82, 62]);

    // ── Special ──
    public static readonly PlayStyle Waltz = S("Waltz (3/4)", "Special",
        //                  1   &   2   &   3   &
        [BO, _,  FC, _,  FC, _ ],
        [95, 0,  70, 0,  68, 0 ],
        [.9, 1,  .5, 1,  .5, 1 ]);

    public static readonly PlayStyle WaltzArp = S("Waltz Arp (3/4)", "Special",
        [BO, AU, FC, AU, AU, AU],
        [90, 65, 72, 62, 65, 60],
        [.9, .8, .5, .8, .8, .8]);

    public static readonly PlayStyle March = S("March", "Special",
        [FC, _,  FC, _,  FC, _,  FC, _ ],
        [100,0,  85, 0,  95, 0,  85, 0 ],
        [.6, 1,  .5, 1,  .6, 1,  .5, 1 ]);

    public static readonly PlayStyle Shuffle = S("Shuffle", "Special",
        [FC, _,  CT, FC, _,  CT, FC, CT],
        [95, 0,  65, 90, 0,  62, 92, 65],
        [.7, 1,  .4, .7, 1,  .4, .7, .4]);

    // ═══════ All styles list ═══════
    public static readonly PlayStyle[] AllStyles =
    [
        // Basic
        WholeNotes, HalfNotes, Ballad,
        // Pop
        PopLight, Pop, PopDriving, PopSyncopated,
        // Rock
        RockSteady, RockEighths, RockDriving, RockPunchy, HardRock,
        Country, CountryTrain,
        // Metal
        Metal, MetalPalmMute, MetalGallop, MetalThrash, MetalMelodic,
        // Jazz / Latin
        Jazz, JazzSwing, BossaNova, Reggae, ReggaeSka, Funk, FunkTight,
        // Arpeggio
        ArpEighthsUp, ArpEighthsDown, ArpUpDown, ArpBassUp, ArpMelodic,
        TripletArpUp, TripletArpBallad, TripletArpDriving,
        // Special
        Waltz, WaltzArp, March, Shuffle,
    ];

    // ── shorthand aliases for readability ──
    private const BeatAction _ = BeatAction.Rest;
    private const BeatAction FC = BeatAction.FullChord;
    private const BeatAction CT = BeatAction.ChordTones;
    private const BeatAction BO = BeatAction.BassOnly;
    private const BeatAction BR = BeatAction.BassAndRoot;
    private const BeatAction AU = BeatAction.ArpUp;
    private const BeatAction AD = BeatAction.ArpDown;
    private const BeatAction TA = BeatAction.TripletArp;

    private static PlayStyle S(string name, string cat, BeatAction[] a, int[] v, double[]? g = null)
        => new(name, cat, a, v, g);
}
