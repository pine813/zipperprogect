using UnityEngine;

namespace BlueRunner
{
    public enum Tone { Jump, Coin, Dash, Hurt, Stomp, Checkpoint, Win }

    public sealed class GameAudio : MonoBehaviour
    {
        static GameAudio instance;
        AudioSource source;
        AudioClip[] clips;
        public static bool Muted { get; private set; }
        void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .24f;
            clips = new[] {
                Synthesize("Jump", 420, 820, .16f), Synthesize("Coin", 1046, 1568, .19f),
                Synthesize("Dash", 280, 100, .16f), Synthesize("Hurt", 170, 65, .28f),
                Synthesize("Stomp", 230, 480, .15f), Synthesize("Checkpoint", 523, 1046, .6f),
                Synthesize("Win", 659, 1318, .8f)
            };
        }
        public static void Play(Tone tone) { if (instance != null && !Muted) instance.source.PlayOneShot(instance.clips[(int)tone]); }
        public static void ToggleMute() { Muted = !Muted; if (instance != null) instance.source.mute = Muted; }
        static AudioClip Synthesize(string name, float from, float to, float duration)
        {
            const int rate = 22050;
            var data = new float[(int)(rate * duration)];
            float phase = 0;
            for (int i = 0; i < data.Length; i++) {
                float t = i / (float)data.Length;
                phase += Mathf.Lerp(from, to, t) / rate * Mathf.PI * 2;
                float envelope = Mathf.Min(t * 35, 1) * Mathf.Pow(1 - t, 1.7f);
                data[i] = (Mathf.Sin(phase) + .15f * Mathf.Sin(phase * 2)) * envelope * .6f;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (clips != null) foreach (var clip in clips) Destroy(clip);
        }
    }
}
