using MeltySynth;
using NAudio.Wave;

namespace ChordBox.Audio;

/// <summary>
/// Renders MIDI-like note events through a SoundFont (.sf2) using MeltySynth + NAudio WaveOut.
/// </summary>
public class SoundFontPlayer : IDisposable
{
    private Synthesizer? _synth;
    private WaveOut? _waveOut;
    private string? _loadedSfPath;
    private int _sampleRate = 44100;

    public bool IsLoaded => _synth != null;
    public string? LoadedPath => _loadedSfPath;

    /// <summary>
    /// Load a SoundFont file. Returns true on success.
    /// </summary>
    public bool Load(string sf2Path)
    {
        try
        {
            Unload();
            var settings = new SynthesizerSettings(_sampleRate);
            _synth = new Synthesizer(sf2Path, settings);
            _loadedSfPath = sf2Path;
            return true;
        }
        catch
        {
            _synth = null;
            _loadedSfPath = null;
            return false;
        }
    }

    public void Unload()
    {
        StopAudio();
        _synth = null;
        _loadedSfPath = null;
    }

    public void SetProgram(int channel, int program)
    {
        _synth?.ProcessMidiMessage(channel, 0xC0, program, 0);
    }

    public void NoteOn(int channel, int note, int velocity)
    {
        _synth?.NoteOn(channel, note, velocity);
    }

    public void NoteOff(int channel, int note)
    {
        _synth?.NoteOff(channel, note);
    }

    public void NoteOffAll()
    {
        if (_synth == null) return;
        for (int ch = 0; ch < 16; ch++)
        {
            for (int note = 0; note < 128; note++)
                _synth.NoteOff(ch, note);
        }
    }

    /// <summary>
    /// Start real-time audio output. Must be called before NoteOn/NoteOff will produce sound.
    /// </summary>
    public void StartAudio()
    {
        if (_synth == null || _waveOut != null) return;
        var provider = new SynthWaveProvider(_synth, _sampleRate);
        _waveOut = new WaveOut { DesiredLatency = 50 };
        _waveOut.Init(provider);
        _waveOut.Play();
    }

    public void StopAudio()
    {
        NoteOffAll();
        if (_waveOut != null)
        {
            try { _waveOut.Stop(); } catch { }
            try { _waveOut.Dispose(); } catch { }
            _waveOut = null;
        }
    }

    /// <summary>
    /// Get available preset names from the loaded SoundFont.
    /// </summary>
    public List<(int Bank, int Program, string Name)> GetPresets()
    {
        if (_synth == null) return new();
        var presets = new List<(int, int, string)>();
        foreach (var p in _synth.SoundFont.Presets)
        {
            presets.Add((p.BankNumber, p.PatchNumber, p.Name));
        }
        presets.Sort((a, b) => a.Item1 == b.Item1 ? a.Item2.CompareTo(b.Item2) : a.Item1.CompareTo(b.Item1));
        return presets;
    }

    public void Dispose()
    {
        StopAudio();
    }

    /// <summary>
    /// NAudio WaveProvider that reads audio from MeltySynth Synthesizer.
    /// </summary>
    private class SynthWaveProvider : IWaveProvider
    {
        private readonly Synthesizer _synth;
        private readonly float[] _leftBuffer;
        private readonly float[] _rightBuffer;

        public WaveFormat WaveFormat { get; }

        public SynthWaveProvider(Synthesizer synth, int sampleRate)
        {
            _synth = synth;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
            _leftBuffer = new float[sampleRate]; // 1 second buffer
            _rightBuffer = new float[sampleRate];
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int sampleCount = count / 8; // 2 channels × 4 bytes per float
            if (sampleCount > _leftBuffer.Length) sampleCount = _leftBuffer.Length;

            _synth.Render(_leftBuffer.AsSpan(0, sampleCount), _rightBuffer.AsSpan(0, sampleCount));

            int byteIndex = offset;
            for (int i = 0; i < sampleCount; i++)
            {
                var leftBytes = BitConverter.GetBytes(_leftBuffer[i]);
                var rightBytes = BitConverter.GetBytes(_rightBuffer[i]);
                Buffer.BlockCopy(leftBytes, 0, buffer, byteIndex, 4);
                byteIndex += 4;
                Buffer.BlockCopy(rightBytes, 0, buffer, byteIndex, 4);
                byteIndex += 4;
            }

            return sampleCount * 8;
        }
    }
}
