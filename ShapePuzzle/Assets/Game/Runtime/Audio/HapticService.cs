using UnityEngine;

namespace ToyPuzzle
{
    public enum HapticCue
    {
        Selection,
        Invalid,
        Correct,
        Completion
    }

    public interface IHapticBackend
    {
        bool IsSupported { get; }
        void Play(HapticCue cue);
    }

    public sealed class HapticService : MonoBehaviour
    {
        [SerializeField] private bool hapticsEnabled = true;
        private IHapticBackend _backend;

        public bool HapticsEnabled => hapticsEnabled;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            _backend = new HandheldHapticBackend();
#else
            _backend = new NoOpHapticBackend();
#endif
        }

        public void SetEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
        }

        public void Play(HapticCue cue)
        {
            if (hapticsEnabled && _backend != null && _backend.IsSupported) _backend.Play(cue);
        }

        private sealed class NoOpHapticBackend : IHapticBackend
        {
            public bool IsSupported => false;
            public void Play(HapticCue cue)
            {
                _ = cue;
            }
        }

        private sealed class HandheldHapticBackend : IHapticBackend
        {
            public bool IsSupported => Application.isMobilePlatform;

            public void Play(HapticCue cue)
            {
                // Unity's dependency-free mobile fallback exposes one safe impulse only.
                // Reserve it for significant feedback; selection remains visual/audio.
                if (cue == HapticCue.Invalid || cue == HapticCue.Correct || cue == HapticCue.Completion)
                {
                    Handheld.Vibrate();
                }
            }
        }
    }
}
