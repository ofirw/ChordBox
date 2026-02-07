namespace ChordBox.Models;

public class Instrument
{
    public string Name { get; }
    public string Category { get; }
    public int MidiProgram { get; }
    public bool UsePowerChords { get; }
    public bool IsStrummed { get; }

    public Instrument(string name, string category, int midiProgram, bool usePowerChords = false, bool isStrummed = false)
    {
        Name = name;
        Category = category;
        MidiProgram = midiProgram;
        UsePowerChords = usePowerChords;
        IsStrummed = isStrummed;
    }

    public override string ToString() => UsePowerChords ? $"{Name} ⚡" : Name;

    // General MIDI program numbers (0-indexed)
    // Piano
    public static readonly Instrument AcousticPiano = new("Acoustic Piano", "Piano", 0);
    public static readonly Instrument ElectricPiano = new("Electric Piano", "Piano", 4);
    // Guitar
    public static readonly Instrument ClassicGuitar = new("Classic Guitar (Nylon)", "Guitar", 24, isStrummed: true);
    public static readonly Instrument AcousticGuitar = new("Acoustic Guitar (Steel)", "Guitar", 25, isStrummed: true);
    public static readonly Instrument JazzGuitar = new("Jazz Guitar (Clean)", "Guitar", 26, isStrummed: true);
    public static readonly Instrument CleanElectric = new("Clean Electric Guitar", "Guitar", 27, isStrummed: true);
    public static readonly Instrument MutedGuitar = new("Muted Guitar", "Guitar", 28, isStrummed: true);
    public static readonly Instrument CrunchRhythm = new("Crunch Rhythm Guitar", "Guitar", 29, isStrummed: true);
    public static readonly Instrument DistortionGuitar = new("Distortion Guitar", "Guitar", 30, usePowerChords: true, isStrummed: true);
    // Organ / Keys
    public static readonly Instrument Organ = new("Drawbar Organ", "Organ / Keys", 16);
    public static readonly Instrument ChurchOrgan = new("Church Organ", "Organ / Keys", 19);
    public static readonly Instrument Accordion = new("Accordion", "Organ / Keys", 21);
    public static readonly Instrument Harmonica = new("Harmonica", "Organ / Keys", 22);
    // Ensemble / Pad
    public static readonly Instrument StringEnsemble = new("String Ensemble", "Ensemble / Pad", 48);
    public static readonly Instrument SynthStrings = new("Synth Strings", "Ensemble / Pad", 50);
    public static readonly Instrument ChoirAahs = new("Choir Aahs", "Ensemble / Pad", 52);
    public static readonly Instrument SynthPad = new("Synth Pad (Warm)", "Ensemble / Pad", 89);
    public static readonly Instrument NewAgePad = new("New Age Pad", "Ensemble / Pad", 88);
    // Percussion / Mallet
    public static readonly Instrument Vibraphone = new("Vibraphone", "Mallet", 11);
    public static readonly Instrument Marimba = new("Marimba", "Mallet", 12);

    public static readonly Instrument[] AllInstruments =
    [
        AcousticPiano, ElectricPiano,
        ClassicGuitar, AcousticGuitar, JazzGuitar, CleanElectric, MutedGuitar,
        CrunchRhythm, DistortionGuitar,
        Organ, ChurchOrgan, Accordion, Harmonica,
        StringEnsemble, SynthStrings, ChoirAahs,
        SynthPad, NewAgePad,
        Vibraphone, Marimba,
    ];
}
