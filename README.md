# ChordBox

**ChordBox** is a desktop chord progression composer and player built with **WPF (.NET)** and **MIDI**. It lets you quickly lay out chord progressions, hear them played back in various styles and instruments, and experiment with song structure using loops, sections, per-beat chord editing, and key/scale analysis.

## Features

### Chord Editing
- **Inline keyboard editing** — Click a bar to select it, then type chord names directly (e.g. `a` → A, `am` → Am, `am7` → Am7, `f#dim` → F#dim, `c/e` → C/E). The chord parses live as you type.
- **Slash chords (chord with bass)** — Type a slash after the chord followed by the bass note (e.g. `Am/G`, `C/E`, `G/B`). The bass note is used in playback, arpeggios, and saved in song files.
- **Per-beat editing** — Press `→` to cycle through individual beats, `←` to go back. Type a different chord for each beat in a bar.
- **All-beats default** — Editing starts in "all beats" mode. Arrow keys switch to per-beat; navigating past first/last beat returns to all-beats mode.
- **Bar Settings dialog** — Click the ⚙ button on any bar to open the full chord picker with root note buttons, quality selection, and per-bar time signature override.
- **Keyboard navigation** — `Tab` moves to the next bar, `Shift+Tab` goes to the previous bar, `Delete` clears the chord, `Backspace` copies the previous beat's chord (per-beat mode) or clears, `Escape` deselects.
- **Collapsed beat display** — Repeating sequential chords are collapsed (e.g. "Am | Am | F | F" shows as "Am | – | F | –").
- **Copy / paste bars** — `Ctrl+C` copies the selected bar, `Ctrl+V` pastes it onto another selected bar.
- **Delete bar** — Click the 🗑 button on any bar card to remove it (adjusts loops and re-indexes automatically).

### Playback
- **MIDI playback** with selectable **styles** and **instruments** (Piano, Guitar, Organ, Synth, Strings, etc.).
- **Instrument-aware playback** — The engine distinguishes between piano/keyboard and guitar/string instruments:
  - **Piano mode**: All chord notes play simultaneously as a vertical harmony event. Rhythm is controlled by PlayStyle patterns (Pop, Rock, Ballad, etc.).
  - **Guitar mode**: Chord notes play sequentially over ~20ms to simulate a real strum gesture. Direction (down/up), articulation (ringing/muted), and rhythm are controlled by a separate **strum pattern**.
- **Strum patterns** — Guitar instruments use strum patterns instead of PlayStyles. Each pattern is a sequence of strum events with direction (down ↓, up ↑, rest), duration (1/4, 1/8, 1/16), and articulation (ring or mute). Predefined patterns include: Basic Down, Eighth Down-Up, Folk (DDU-UDU), Pop Ballad, Country, Driving Eighths, Reggae Chop, Ska Offbeat, Punk Muted, Funk 16ths, and 16th Pattern.
- **Strum pattern editor** — Click the ✏ button (visible when a guitar instrument is selected) to open the editor. Add or remove strum events; editing a predefined pattern automatically creates a custom copy.
- **SoundFont (.sf2) support** — Load custom SoundFont files for higher-quality instrument sounds via [MeltySynth](https://github.com/sinshu/meltysynth). Falls back to Windows MIDI (Microsoft GS Wavetable Synth) when no SoundFont is loaded. Recently loaded SoundFonts appear in a quick-access dropdown.
- **Arpeggio styles** (piano mode) — Broken-chord patterns including Arp Eighths Up/Down, Arp Up-Down, Arp Bass+Up, Arp Melodic, and **triplet arpeggios**.
- **Note sustain** — Styles like Whole Notes, Half Notes, and Ballad sustain notes naturally until the next chord action. Rhythmic styles use configurable gate fractions for staccato/legato control.
- **Tempo control** via slider (40–300 BPM) and **TAP tempo** — tap the TAP button repeatedly to set the tempo by feel.
- **Count-in metronome** before playback starts.
- **Play from any bar** using the ▶ button on each bar card.
- **Live parameter changes** — Tempo, style, instrument, strum pattern, and loop settings update in real-time during playback.
- **Power chord mode** — Automatically engages for electric guitar instruments.

### Song Structure
- **Flexible time signatures** — Global time signature (2/4 to 7/4) with per-bar overrides.
- **Section markers** — Label bar ranges as Intro, Verse, Pre-Chorus, Chorus, Bridge, Solo, Interlude, Outro, or custom names. Sections and loops are unified into a single concept.
- **Nested loops** — Define loop regions with repeat counts. Loops can be fully nested inside each other. A section with repeat count = 1 acts as a label-only marker (no looping).
- **Visual loop/section layers** — Color-coded brackets with section labels displayed above each bar. Non-overlapping loops share the same layer row; only truly overlapping loops stack.
- **Loop/section editor** — Set name, section type, bar range, and repeat count.

### Composing Help (Key Helper)
- **Key & Scale detection** — Auto-detects the key and scale of your song using weighted functional harmony scoring (tonic, dominant, subdominant weighting with positional emphasis on first/last chords and secondary dominant recognition).
- **Grouped chord display** — Shows chords organized by category:
  - **Diatonic Triads** — The 7 standard chords of the key (I, ii, iii, IV, V, vi, vii°).
  - **7th Chords** — Diatonic seventh chords (maj7, m7, dom7).
  - **Suspended** — Sus2 and sus4 variants on each scale degree.
  - **Secondary Dominants** — V7/ii, V7/iii, V7/IV, V7/V, V7/vi, V7/vii°.
  - **Borrowed Chords** — Chords from the parallel major/minor scale.
  - **Diminished & Augmented** — Dim and aug variants on each scale degree.
- **Click to preview** — Click any chord to hear it played through the current instrument.
- **Remembers your choice** — Manually selected key/scale persists across open/close until you change the song.

### Undo / Redo
- **Full undo/redo** — All chord edits, bar additions/removals, and loop changes are tracked with snapshot-based undo/redo.
- **Keyboard shortcuts** — `Ctrl+Z` to undo, `Ctrl+Y` to redo.

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `→` / `←` | Navigate beats in inline editing |
| `Tab` | Next bar |
| `Shift+Tab` | Previous bar |
| `Ctrl+C` | Copy selected bar |
| `Ctrl+V` | Paste to selected bar |
| `Delete` | Clear chord or input |
| `Backspace` | Copy previous beat's chord (per-beat) or clear |
| `Escape` | Deselect bar |

### Lyrics
- **Per-bar lyrics** — Toggle lyrics display and type lyrics below each bar.
- **Auto-shrinking text** — Long lyrics automatically reduce font size to fit in one line instead of wrapping.
- **Loop-aware lyrics** — When a bar is inside loops, multiple lyrics input fields appear (one per total repeat). This lets you write different lyrics for each repetition (e.g. Verse 1 / Verse 2 / Verse 3).
- **Tab navigation** — Press Tab in a lyrics field to jump to the next bar's lyrics.

### File Management
- **Save/Load** songs in `.cbs` (ChordBox Song) JSON format.
- **New song** creates 8 empty bars with a default C–Am–F–G7 progression.

## Technology

| Component | Technology |
|-----------|-----------|
| **Framework** | .NET 10 / WPF (Windows Presentation Foundation) |
| **Language** | C# 13 |
| **Architecture** | MVVM (Model-View-ViewModel) |
| **MIDI** | [NAudio](https://github.com/naudio/NAudio) — `NAudio.Midi` for MIDI output |
| **SoundFont** | [MeltySynth](https://github.com/sinshu/meltysynth) — SoundFont synthesizer |
| **UI** | XAML with custom dark theme, data binding, converters |
| **Serialization** | `System.Text.Json` for `.cbs` song files |

### Project Structure

```
ChordBox/
├── Audio/
│   ├── MidiChordPlayer.cs        # MIDI playback engine with live parameter updates
│   └── SoundFontPlayer.cs        # SoundFont (.sf2) rendering via MeltySynth + NAudio
├── Converters/
│   └── BoolToVisibilityConverter.cs  # WPF value converters
├── Models/
│   ├── Bar.cs                    # Bar model (chord events, time sig, lyrics)
│   ├── Chord.cs                  # Chord (root + quality → MIDI notes)
│   ├── ChordEvent.cs             # Chord placed at a specific beat
│   ├── ChordParser.cs            # Parses typed text into Chord objects
│   ├── ChordQuality.cs           # Enum: Major, Minor, 7, maj7, m7, dim, aug, sus2, sus4
│   ├── Instrument.cs             # MIDI instruments with categories
│   ├── LoopRegion.cs             # Loop/section definition (start/end bar, repeats, section type)
│   ├── NoteName.cs               # 12-tone note names with display strings
│   ├── PlayStyle.cs              # Beat patterns (actions, velocities, gate fractions)
│   ├── ScaleHelper.cs            # Key detection, diatonic/7th/sus/secdom/borrowed chord generation
│   ├── SongFile.cs               # Serialization model for .cbs files
│   └── SongSnapshot.cs           # Lightweight state snapshots for undo/redo
├── ViewModels/
│   ├── BarViewModel.cs           # Per-bar UI state (editing, playback, loops, lyrics)
│   ├── LoopLayerInfo.cs          # Visual info for nested loop/section display
│   ├── MainViewModel.cs          # Main application logic and commands
│   ├── RelayCommand.cs           # ICommand implementation
│   ├── UndoManager.cs            # Generic undo/redo stack manager
│   └── ViewModelBase.cs          # INotifyPropertyChanged base class
├── MainWindow.xaml               # Main UI layout
├── MainWindow.xaml.cs            # Code-behind (keyboard routing, shortcuts)
├── App.xaml                      # Application resources and theme
└── App.xaml.cs                   # Application entry point
```

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- Windows (WPF is Windows-only)
- A MIDI output device (Windows built-in Microsoft GS Wavetable Synth works), or a `.sf2` SoundFont file

### Build & Run

```bash
dotnet build
dotnet run
```

## Usage

1. **Click a bar** to select it for inline editing.
2. **Type a chord name** (e.g. `c`, `am7`, `f#`, `bbm`) — it updates live.
3. **Press →** to edit individual beats, **Tab** to move to the next bar.
4. **Click ⚙** on a bar to open the full Bar Settings dialog for detailed editing.
5. **Press Play** to hear your progression with the selected style and instrument.
6. **Click 🎼 Key Helper** to see which key your song is in and explore all matching chord variations.
7. **Click 🔊 Load SF2** to load a SoundFont for higher-quality playback.
8. **Use 🔁 Set Loop** to define sections and loops with optional section labels (Intro, Verse, Chorus, etc.).
9. **Ctrl+Z / Ctrl+Y** to undo and redo any changes.

## Future Features

The following features are planned or under consideration for future development:

### Composition & Theory
- **Chord suggestions** — AI-powered or rule-based suggestions for the next chord based on common progressions and music theory.
- **Modulation detection** — Detect key changes within a song and display them.
- **Roman numeral analysis** — Show chord function (I, IV, V, etc.) above each bar relative to the detected key.
- **Melody line editor** — Add a simple melody track on top of the chord progression.

### Playback & Sound
- **Audio export** — Export the song as WAV or MP3 using MIDI rendering.
- **Metronome toggle** — Optional click track during playback.
- **Swing / shuffle feel** — Adjust the rhythmic feel beyond straight timing. A per-song or per-section "swing amount" slider (0–100%) would delay every off-beat note proportionally, creating a triplet shuffle feel. The UI would show a small swing icon in the transport bar.
- **Velocity dynamics** — Per-bar or per-beat velocity overrides for crescendo/decrescendo effects. Each bar card would show a small volume curve. Users could set a velocity range (e.g. pp → ff) across a bar range, and the playback engine would interpolate note velocities accordingly.
- **Multiple instrument tracks** — Layer multiple instruments playing simultaneously.

### UI & Workflow
- **Drag-and-drop bars** — Reorder bars by dragging them.
- **Print / PDF export** — Generate a printable chord chart / lead sheet.
- **Dark/light theme toggle** — Switch between dark and light UI themes.
- **Resizable bar cards** — Dynamically size cards based on window width.

### Import & Export
- **MusicXML import/export** — Interoperability with notation software (MuseScore, Finale, Sibelius).
- **MIDI file import** — Parse a MIDI file and extract the chord progression.
- **Guitar tab view** — Show chord diagrams or tablature for guitar players.
- **Nashville number system** — Alternative chord display using scale degree numbers.

### Collaboration
- **Cloud save** — Save songs to a cloud service for access across devices.
- **Sharing** — Share chord charts via link or embed.
