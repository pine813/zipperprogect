using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarryAbyss
{
    /// <summary>
    /// A tiny procedural concert hall: felt piano, a quiet string-like pad,
    /// sub-bass gravity, and collision sounds built from stone, light and air.
    /// </summary>
    public sealed class StarryAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float BarSeconds = 4.8f;

        private AudioSource[] _musicSources;
        private double[] _musicEnds;
        private AudioSource[] _padSources;
        private double[] _padEnds;
        private AudioSource[] _sfxSources;
        private AudioSource _droneSource;
        private AudioSource _portalSource;
        private AudioClip _droneClip;
        private AudioClip _portalClip;
        private AudioClip _uiClip;
        private AudioClip _thudClip;
        private AudioClip _bigBangClip;
        private AudioClip _collapseClip;
        private AudioClip _riserClip;
        private AudioClip _portalChordClip;
        private AudioClip _endingClip;
        private AudioClip[] _collisionClips;

        private readonly Dictionary<int, AudioClip> _pianoCache = new Dictionary<int, AudioClip>();
        private readonly Dictionary<int, AudioClip> _padCache = new Dictionary<int, AudioClip>();
        private Coroutine _musicRoutine;
        private bool _initialized;
        private bool _musicRunning;
        private bool _muted;
        private int _barIndex;
        private int _musicSourceCursor;
        private int _sfxCursor;
        private float _dangerTarget;
        private float _portalTarget;
        private float _danger;
        private float _portalNear;
        private float _lastSfxTime;
        private float _lastSparkTime;

        private static readonly int[][] Chords =
        {
            new[] { 50, 57, 61, 64, 66 }, // Dmaj9
            new[] { 47, 54, 57, 61, 64 }, // Bm11
            new[] { 43, 50, 54, 57, 62 }, // Gmaj9
            new[] { 45, 50, 52, 55, 59 }, // A9sus4
            new[] { 50, 57, 61, 64, 69 }, // Dmaj9(add9)
            new[] { 47, 54, 57, 62, 66 }, // Bm11
            new[] { 43, 50, 54, 59, 66 }, // Gmaj9
            new[] { 45, 49, 52, 57, 61 }  // A7
        };

        private static readonly int[] BassNotes = { 38, 35, 43, 45, 38, 35, 43, 45 };

        private static readonly int[][] ArousalPatterns =
        {
            new[] { 0, 1, 2, 4 },
            new[] { 1, 2, 3, 4 },
            new[] { 0, 1, 3, 4 },
            new[] { 2, 3, 4, 1 }
        };

        private static readonly float[] BeatPattern = { 0f, 1.5f, 2.25f, 3.05f };

        private static readonly int[][] Melody =
        {
            new[] { 74, 78 },
            new[] { 78, 81, 83 },
            new int[0],
            new[] { 83, 81, 78 },
            new[] { 86, 83 },
            new[] { 81, 78 },
            new[] { 76, 78, 74 },
            new[] { 74, 81 }
        };

        public bool Muted => _muted;
        public bool Ready => _initialized;

        public void Initialize()
        {
            if (_initialized) return;
            try
            {
                BuildAudioRoots();
                BuildLoops();
                BuildEffects();
                ApplyMute();
                _initialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Star score could not awaken: " + e);
                _initialized = false;
            }
        }

        private void BuildAudioRoots()
        {
            var musicRoot = new GameObject("Music / Felt Piano");
            musicRoot.transform.SetParent(transform, false);

            _musicSources = new AudioSource[14];
            _musicEnds = new double[14];
            for (int i = 0; i < _musicSources.Length; i++)
            {
                var src = musicRoot.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = 0.2f;
                src.panStereo = 0f;
                _musicSources[i] = src;
            }

            _padSources = new AudioSource[2];
            _padEnds = new double[2];
            for (int i = 0; i < _padSources.Length; i++)
            {
                var pad = musicRoot.AddComponent<AudioSource>();
                pad.playOnAwake = false;
                pad.loop = false;
                pad.spatialBlend = 0f;
                pad.volume = 0.19f;
                _padSources[i] = pad;
            }

            var sfxRoot = new GameObject("Star Impacts");
            sfxRoot.transform.SetParent(transform, false);
            _sfxSources = new AudioSource[8];
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                var src = sfxRoot.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = 0.8f;
                _sfxSources[i] = src;
            }

            var droneGo = new GameObject("Singularity Breath");
            droneGo.transform.SetParent(transform, false);
            _droneSource = droneGo.AddComponent<AudioSource>();
            _droneSource.playOnAwake = false;
            _droneSource.loop = true;
            _droneSource.spatialBlend = 0f;
            _droneSource.volume = 0f;

            var portalGo = new GameObject("Gate Hum");
            portalGo.transform.SetParent(transform, false);
            _portalSource = portalGo.AddComponent<AudioSource>();
            _portalSource.playOnAwake = false;
            _portalSource.loop = true;
            _portalSource.spatialBlend = 0f;
            _portalSource.volume = 0f;
        }

        private void BuildLoops()
        {
            _droneClip = MakeDroneClip(8f);
            _portalClip = MakePortalHumClip(6f);
            _droneSource.clip = _droneClip;
            _portalSource.clip = _portalClip;
            _droneSource.Play();
            _portalSource.Play();
        }

        private void BuildEffects()
        {
            _uiClip = MakeUiClip();
            _thudClip = MakeThudClip();
            _bigBangClip = MakeBigBangClip();
            _collapseClip = MakeCollapseClip();
            _riserClip = MakeRiserClip();
            _portalChordClip = MakePortalClip();
            _endingClip = MakeEndingClip();
            _collisionClips = new AudioClip[4];
            for (int tier = 0; tier < 4; tier++)
            {
                _collisionClips[tier] = MakeCollisionClip(tier);
            }
        }

        // ---------------------------------------------------------
        // Music
        // ---------------------------------------------------------

        public void StartMusic()
        {
            if (!_initialized) Initialize();
            if (!_initialized || _musicRunning) return;
            for (int i = 0; i < Chords.Length; i++) GetPadClip(i);
            _musicRunning = true;
            _barIndex = 0;
            _droneTargetOverride = -1f;
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(MusicLoop());
        }

        public void StopMusic()
        {
            _musicRunning = false;
            if (_musicRoutine != null)
            {
                StopCoroutine(_musicRoutine);
                _musicRoutine = null;
            }
        }

        private IEnumerator MusicLoop()
        {
            double nextBar = AudioSettings.dspTime + 0.65;
            int bar = _barIndex;
            while (_musicRunning)
            {
                ScheduleBar(bar, nextBar);
                bar++;
                _barIndex = bar;
                double nextStart = nextBar + BarSeconds;
                double waitUntil = nextStart - 0.65;
                while (_musicRunning && AudioSettings.dspTime < waitUntil)
                    yield return null;
                nextBar = nextStart;
            }
        }

        private void ScheduleBar(int bar, double dspTime)
        {
            int chordIndex = ((bar % 8) + 8) % 8;
            var chord = Chords[chordIndex];

            var padClip = GetPadClip(chordIndex);
            var padSource = TakePadSource(dspTime, padClip.length);
            if (padSource != null)
            {
                padSource.clip = padClip;
                padSource.volume = 0.145f;
                padSource.PlayScheduled(dspTime);
            }

            // The left hand: low, unhurried, almost like a memory.
            PlayPiano(BassNotes[chordIndex], dspTime + 0.02f, 0.84f, -0.12f);
            PlayPiano(BassNotes[chordIndex] + 12, dspTime + BarSeconds * 0.53f, 0.34f, 0.08f);

            // A broken chord in the middle of the hall.
            var pattern = ArousalPatterns[bar % ArousalPatterns.Length];
            for (int i = 0; i < BeatPattern.Length; i++)
            {
                int toneIndex = pattern[i % pattern.Length] % chord.Length;
                int note = chord[toneIndex] + ((i % 2 == 0) ? 12 : 24);
                float velocity = i == 0 ? 0.48f : 0.28f + (i == 2 ? 0.06f : 0f);
                PlayPiano(note, dspTime + BeatPattern[i] * (BarSeconds / 4f), velocity, i % 2 == 0 ? -0.28f : 0.3f);
            }

            // The right hand: two or three notes, with a great deal of air.
            var melody = Melody[bar % Melody.Length];
            for (int i = 0; i < melody.Length; i++)
            {
                double when = dspTime + (1.2 + i * 1.32) * (BarSeconds / 4f);
                float vel = i == 0 ? 0.55f : 0.3f;
                PlayPiano(melody[i], when, vel, i % 2 == 0 ? 0.12f : -0.16f);
                if (bar % 4 == 0 && i == 0)
                    PlayPiano(melody[i] + 12, when + 0.85f, 0.18f, 0.28f);
            }

            if (bar % 8 == 5)
            {
                PlayPiano(86, dspTime + BarSeconds * 0.73f, 0.24f, -0.55f);
            }
        }

        private void PlayPiano(int midi, double dspTime, float velocity, float pan)
        {
            if (_musicSources == null) return;
            var clip = GetPianoClip(midi);
            int index = PickMusicSource(dspTime, 3.4);
            var source = _musicSources[index];
            source.clip = clip;
            source.volume = Mathf.Clamp(velocity * 0.29f, 0.02f, 0.3f);
            source.panStereo = Mathf.Clamp(pan, -1f, 1f);
            source.pitch = 1f;
            source.PlayScheduled(dspTime);
            _musicEnds[index] = dspTime + clip.length;
        }

        private int PickMusicSource(double when, double minFree)
        {
            int best = 0;
            double bestEnd = double.MaxValue;
            for (int i = 0; i < _musicSources.Length; i++)
            {
                if (!_musicSources[i].isPlaying && _musicEnds[i] <= when + 0.05)
                {
                    return i;
                }
                if (_musicEnds[i] < bestEnd)
                {
                    bestEnd = _musicEnds[i];
                    best = i;
                }
            }
            _musicSourceCursor = (_musicSourceCursor + 1) % _musicSources.Length;
            return best;
        }

        private AudioSource TakePadSource(double when, float duration)
        {
            for (int i = 0; i < _padSources.Length; i++)
            {
                if (_padEnds[i] <= when + 0.1)
                {
                    _padEnds[i] = when + duration;
                    return _padSources[i];
                }
            }
            _padEnds[0] = when + duration;
            return _padSources[0];
        }

        private AudioClip GetPianoClip(int midi)
        {
            if (_pianoCache.TryGetValue(midi, out var c)) return c;
            c = SynthesizePiano(midi, 3.4f);
            _pianoCache[midi] = c;
            return c;
        }

        private AudioClip GetPadClip(int chordIndex)
        {
            if (_padCache.TryGetValue(chordIndex, out var c)) return c;
            c = SynthesizePad(Chords[chordIndex], 6.6f);
            _padCache[chordIndex] = c;
            return c;
        }

        // ---------------------------------------------------------
        // Effects
        // ---------------------------------------------------------

        public void SetDanger(float danger, float nearPortal)
        {
            _dangerTarget = Mathf.Clamp01(danger);
            _portalTarget = Mathf.Clamp01(nearPortal);
        }

        public void PlayCollision(float strength, int tier, float pan)
        {
            if (!_initialized || _collisionClips == null) return;
            if (Time.unscaledTime - _lastSfxTime < 0.035f) return;
            _lastSfxTime = Time.unscaledTime;
            var source = NextSfxSource();
            if (source == null) return;
            tier = Mathf.Clamp(tier, 0, 3);
            source.clip = _collisionClips[tier];
            source.pitch = Random.Range(0.94f, 1.06f);
            source.panStereo = Mathf.Clamp(pan, -0.8f, 0.8f);
            source.volume = (0.11f + strength * 0.32f) * (0.65f + tier * 0.1f);
            source.Play();
        }

        public void PlayThud(float strength = 0.5f)
        {
            if (!_initialized) return;
            var source = NextSfxSource();
            if (source == null) return;
            source.clip = _thudClip;
            source.pitch = Random.Range(0.85f, 1.05f);
            source.volume = 0.16f + strength * 0.28f;
            source.panStereo = 0f;
            source.Play();
        }

        public void PlayUi()
        {
            if (!_initialized) return;
            var source = NextSfxSource();
            if (source == null) return;
            source.clip = _uiClip;
            source.volume = 0.14f;
            source.pitch = Random.Range(0.97f, 1.03f);
            source.panStereo = 0f;
            source.Play();
        }

        public void PlaySpark()
        {
            if (!_initialized) return;
            if (Time.unscaledTime - _lastSparkTime < 0.045f) return;
            _lastSparkTime = Time.unscaledTime;
            var source = NextSfxSource();
            if (source == null) return;
            source.clip = _uiClip;
            source.volume = 0.065f;
            source.pitch = Random.Range(1.28f, 1.55f);
            source.panStereo = Random.Range(-0.25f, 0.25f);
            source.Play();
        }

        public void PlayPortal()
        {
            if (!_initialized) return;
            StopMusic();
            var source = NextSfxSource();
            if (source != null)
            {
                source.clip = _portalChordClip;
                source.volume = 0.7f;
                source.pitch = 0.98f;
                source.panStereo = 0.1f;
                source.Play();
            }
            _portalTarget = 1f;
        }

        public void BeginCollapse()
        {
            if (!_initialized) return;
            StopMusic();
            var source = NextSfxSource();
            if (source != null)
            {
                source.clip = _collapseClip;
                source.volume = 0.5f;
                source.panStereo = 0f;
                source.Play();
            }
            var riser = NextSfxSource();
            if (riser != null)
            {
                riser.clip = _riserClip;
                riser.volume = 0.26f;
                riser.Play();
            }
        }

        public void PlayBigBang()
        {
            if (!_initialized) return;
            _droneTargetOverride = 0f;
            var source = NextSfxSource();
            if (source != null)
            {
                source.clip = _bigBangClip;
                source.volume = 0.7f;
                source.pitch = 0.96f;
                source.panStereo = 0f;
                source.Play();
            }
        }

        public void PlayEnding()
        {
            if (!_initialized) return;
            var source = NextSfxSource();
            if (source == null) return;
            source.clip = _endingClip;
            source.volume = 0.24f;
            source.pitch = 1f;
            source.panStereo = 0f;
            source.Play();
        }

        private float _droneTargetOverride = -1f;

        public void SetPaused(bool paused)
        {
            if (AudioListener.pause == paused) return;
            AudioListener.pause = paused;
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            ApplyMute();
        }

        private void ApplyMute()
        {
            AudioListener.volume = _muted ? 0f : 1f;
        }

        private AudioSource NextSfxSource()
        {
            if (_sfxSources == null) return null;
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                int index = (_sfxCursor + i) % _sfxSources.Length;
                if (!_sfxSources[index].isPlaying)
                {
                    _sfxCursor = (index + 1) % _sfxSources.Length;
                    return _sfxSources[index];
                }
            }
            var fallback = _sfxSources[_sfxCursor];
            _sfxCursor = (_sfxCursor + 1) % _sfxSources.Length;
            return fallback;
        }

        private void Update()
        {
            _danger = Mathf.MoveTowards(_danger, _dangerTarget, Time.unscaledDeltaTime * 0.6f);
            _portalNear = Mathf.MoveTowards(_portalNear, _portalTarget, Time.unscaledDeltaTime * 0.8f);
            if (_droneSource != null)
            {
                float target = 0.02f + _danger * 0.15f;
                if (_droneTargetOverride >= 0f) target = _droneTargetOverride;
                _droneSource.volume = Mathf.MoveTowards(_droneSource.volume, target, Time.unscaledDeltaTime * 0.15f);
            }
            if (_portalSource != null)
                _portalSource.volume = Mathf.MoveTowards(_portalSource.volume, 0.006f + _portalNear * 0.05f, Time.unscaledDeltaTime * 0.2f);
        }

        // ---------------------------------------------------------
        // Synthesis
        // ---------------------------------------------------------

        private AudioClip SynthesizePiano(int midi, float duration)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            float f = MidiToHz(midi);
            float norm = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = 1f - Mathf.Exp(-t * 180f);
                float decay = Mathf.Exp(-t * (1.15f + midi * 0.008f));
                float body = Mathf.Sin(2f * Mathf.PI * f * t) * 0.64f;
                body += Mathf.Sin(2f * Mathf.PI * f * 2.002f * t + 0.4f) * 0.18f * Mathf.Exp(-t * 2.2f);
                body += Mathf.Sin(2f * Mathf.PI * f * 3.001f * t + 1.1f) * 0.08f * Mathf.Exp(-t * 3.2f);
                body += Mathf.Sin(2f * Mathf.PI * f * 4.01f * t) * 0.035f * Mathf.Exp(-t * 4.8f);
                float hammer = t < 0.06f ? (Noise(i * 3 + midi) * 0.1f * Mathf.Exp(-t * 75f)) : 0f;
                float sample = (body * attack * decay + hammer) * 0.78f;
                data[i] = sample;
                norm = Mathf.Max(norm, Mathf.Abs(sample));
            }
            if (norm > 0.0001f)
            {
                float k = 0.78f / norm;
                for (int i = 0; i < length; i++) data[i] *= k;
            }
            for (int i = 0; i < 500; i++)
            {
                float a = i / 500f;
                data[i] *= a;
                data[length - 1 - i] *= a;
            }
            AddEcho(data, 0.115f, 0.17f);
            AddEcho(data, 0.27f, 0.08f);
            SoftClip(data);
            var clip = AudioClip.Create("FeltPiano_" + midi, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip SynthesizePad(int[] chord, float duration)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 1.6f);
                float release = Mathf.Clamp01((duration - t) / 2.2f);
                float env = attack * attack * (3f - 2f * attack) * release;
                float s = 0f;
                for (int n = 0; n < chord.Length; n++)
                {
                    float f = MidiToHz(chord[n] - 12);
                    s += Mathf.Sin(2f * Mathf.PI * f * t + n * 0.7f) * 0.12f;
                    s += Mathf.Sin(2f * Mathf.PI * f * 2.003f * t + n) * 0.035f;
                    s += Mathf.Sin(2f * Mathf.PI * f * 1.004f * t + 1.2f + n * 0.13f) * 0.07f;
                }
                float air = Noise(i * 7 + nHash(chord[0], chord[1])) * 0.006f;
                data[i] = (s + air) * env * 0.42f;
            }
            AddEcho(data, 0.19f, 0.14f);
            AddEcho(data, 0.47f, 0.07f);
            SoftClip(data);
            var clip = AudioClip.Create("Pad_" + chord[0], length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeDroneClip(float duration)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float fade = Mathf.Clamp01(t / 1.4f) * Mathf.Clamp01((duration - t) / 1.4f);
                float s = Mathf.Sin(2f * Mathf.PI * 41f * t) * 0.12f;
                s += Mathf.Sin(2f * Mathf.PI * 61.7f * t + 0.8f) * 0.035f;
                s += Noise(i * 11) * 0.023f;
                data[i] = s * fade * 0.8f;
            }
            var clip = AudioClip.Create("SingularityDrone", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakePortalHumClip(float duration)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float fade = Mathf.Clamp01(t / 1f) * Mathf.Clamp01((duration - t) / 1f);
                float trem = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.17f * t);
                float s = Mathf.Sin(2f * Mathf.PI * 528f * t) * 0.035f;
                s += Mathf.Sin(2f * Mathf.PI * 792f * t + 0.5f) * 0.018f;
                s += Mathf.Sin(2f * Mathf.PI * 1056f * t + 1.3f) * 0.007f;
                data[i] = s * trem * fade;
            }
            var clip = AudioClip.Create("PortalHum", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeUiClip()
        {
            int length = Mathf.CeilToInt(0.45f * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 7f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.1f * env;
            }
            var clip = AudioClip.Create("UiPing", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeThudClip()
        {
            int length = Mathf.CeilToInt(0.8f * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float freq = Mathf.Lerp(85f, 33f, Mathf.Clamp01(t / 0.5f));
                float env = Mathf.Exp(-t * 4.5f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            var clip = AudioClip.Create("Thud", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeCollisionClip(int tier)
        {
            float duration = 1.25f + tier * 0.35f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var chord = new[] { 38, 45, 50, 57 };
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float lowEnv = Mathf.Exp(-t * (5f - tier * 0.4f));
                float freq = Mathf.Lerp(110f - tier * 10f, 31f, Mathf.Clamp01(t / 0.7f));
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * lowEnv * (0.35f + tier * 0.12f);
                float noiseEnv = Mathf.Exp(-t * (9f - tier));
                s += Noise(i * 5 + tier * 97) * noiseEnv * (0.13f + tier * 0.04f);
                if (tier >= 2)
                {
                    for (int c = 0; c < chord.Length; c++)
                    {
                        float f = MidiToHz(chord[c]);
                        s += Mathf.Sin(2f * Mathf.PI * f * t + c * 0.8f) * Mathf.Exp(-t * 1.1f) * 0.035f;
                    }
                }
                if (tier >= 1)
                {
                    s += Mathf.Sin(2f * Mathf.PI * (740f + tier * 140f) * t) * Mathf.Exp(-t * 5.5f) * 0.018f;
                }
                data[i] = s;
            }
            SoftClip(data);
            var clip = AudioClip.Create("Collision_" + tier, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeBigBangClip()
        {
            float duration = 8f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var chord = new[] { 26, 38, 45, 50, 54, 57, 62, 66 };
            float lowNoise = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float s = 0f;
                float subEnv = Mathf.Exp(-t * 0.75f);
                float subFreq = Mathf.Lerp(58f, 23f, Mathf.Clamp01(t / 3.2f));
                s += Mathf.Sin(2f * Mathf.PI * subFreq * t) * subEnv * 0.34f;
                lowNoise += 0.055f * (Noise(i * 3 + 11) - lowNoise);
                s += lowNoise * Mathf.Exp(-t * 0.45f) * 0.34f;
                for (int c = 0; c < chord.Length; c++)
                {
                    float f = MidiToHz(chord[c]);
                    float env = Mathf.Exp(-t * (0.22f + c * 0.025f)) * Mathf.Clamp01(t / 0.045f);
                    s += Mathf.Sin(2f * Mathf.PI * f * t + c * 0.55f) * env * (0.048f - c * 0.0035f);
                }
                // A round, low onset instead of the old bright transient crack.
                s += Mathf.Sin(2f * Mathf.PI * 46f * t) * Mathf.Exp(-t * 3.4f) * 0.11f;
                data[i] = s;
            }
            LowPass(data, 0.18f, 2);
            AddEcho(data, 0.36f, 0.16f);
            AddEcho(data, 0.61f, 0.09f);
            SoftClip(data);
            var clip = AudioClip.Create("NewUniverse", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeCollapseClip()
        {
            float duration = 8f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 0.36f);
                float s = Mathf.Sin(2f * Mathf.PI * 45f * t) * env * 0.08f;
                s += Mathf.Sin(2f * Mathf.PI * 38f * t + 0.4f) * env * 0.06f;
                s += Mathf.Sin(2f * Mathf.PI * (84f - t * 5f) * t) * Mathf.Exp(-t * 0.5f) * 0.03f;
                int beat = Mathf.FloorToInt(t / 1.65f);
                float local = t - beat * 1.65f;
                float pulse = Mathf.Exp(-local * 4f) * Mathf.Clamp01(1f - beat / 6f);
                s += Mathf.Sin(2f * Mathf.PI * (62f - local * 60f) * local) * pulse * 0.14f;
                data[i] = s;
            }
            LowPass(data, 0.24f, 1);
            AddEcho(data, 0.52f, 0.2f);
            AddEcho(data, 1.03f, 0.1f);
            SoftClip(data);
            var clip = AudioClip.Create("Collapse", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeRiserClip()
        {
            float duration = 7f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float noise = Noise(i * 13 + 17);
                float filter = Mathf.Lerp(0.002f, 0.045f, p * p);
                phase += filter * (noise - phase);
                float env = Mathf.Clamp01(t / 1.5f) * Mathf.Clamp01((duration - t) / 0.08f);
                float toneFreq = Mathf.Lerp(43f, 72f, p);
                float s = phase * env * Mathf.Lerp(0.16f, 0.4f, p);
                s += Mathf.Sin(2f * Mathf.PI * toneFreq * t) * env * 0.06f;
                data[i] = s;
            }
            LowPass(data, 0.22f, 1);
            AddEcho(data, 0.44f, 0.18f);
            SoftClip(data);
            var clip = AudioClip.Create("Riser", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakePortalClip()
        {
            float duration = 7f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var notes = new[] { 50, 57, 62, 66, 69, 74 };
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float s = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float start = n * 0.26f;
                    if (t < start) continue;
                    float local = t - start;
                    float env = (1f - Mathf.Exp(-local * 3.4f)) * Mathf.Exp(-local * 0.28f);
                    s += Mathf.Sin(2f * Mathf.PI * MidiToHz(notes[n]) * local + n) * env * (0.062f - n * 0.0035f);
                }
                s += Mathf.Sin(2f * Mathf.PI * 73.42f * t) * Mathf.Exp(-t * 0.34f) * 0.035f;
                s += Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 0.7f) * 0.03f;
                data[i] = s;
            }
            LowPass(data, 0.22f, 1);
            AddEcho(data, 0.42f, 0.2f);
            AddEcho(data, 0.77f, 0.1f);
            SoftClip(data);
            var clip = AudioClip.Create("Wayfarer", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeEndingClip()
        {
            float duration = 7.5f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var notes = new[] { 50, 57, 62, 66, 69 };
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 1.35f);
                float release = Mathf.Clamp01((duration - t) / 2.2f);
                float s = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.035f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float f = MidiToHz(notes[n]);
                    float wobble = 1f + Mathf.Sin(t * 0.7f + n) * 0.0015f;
                    float voice = Mathf.Sin(2f * Mathf.PI * f * wobble * t + n * 1.1f);
                    voice += Mathf.Sin(2f * Mathf.PI * f * 2f * t + n * 0.4f) * 0.12f;
                    s += voice * (0.043f - n * 0.004f);
                }
                data[i] = s * attack * release;
            }
            LowPass(data, 0.16f, 2);
            AddEcho(data, 0.55f, 0.22f);
            AddEcho(data, 0.93f, 0.1f);
            SoftClip(data);
            var clip = AudioClip.Create("Afterglow", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float MidiToHz(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        private static float Noise(int seed)
        {
            unchecked
            {
                uint x = (uint)seed;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return (x & 0xFFFFFF) / 8388608f - 1f;
            }
        }

        private static int nHash(int a, int b)
        {
            return a * 397 ^ b * 7919;
        }

        private static void LowPass(float[] data, float alpha, int passes)
        {
            alpha = Mathf.Clamp01(alpha);
            for (int pass = 0; pass < passes; pass++)
            {
                float y = 0f;
                for (int i = 0; i < data.Length; i++)
                {
                    y += alpha * (data[i] - y);
                    data[i] = y;
                }
            }
        }

        private static void SoftClip(float[] data)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            if (max > 0.0001f)
            {
                float k = 0.92f / max;
                for (int i = 0; i < data.Length; i++) data[i] = (float)System.Math.Tanh(data[i] * k);
            }
        }

        private static void AddEcho(float[] data, float delaySeconds, float gain)
        {
            int delay = Mathf.Max(1, Mathf.RoundToInt(delaySeconds * SampleRate));
            for (int i = data.Length - 1; i >= delay; i--)
            {
                data[i] += data[i - delay] * gain;
            }
        }
    }
}
