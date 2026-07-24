using UnityEngine;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private ToyAudioLibrary library;
        [SerializeField, Range(2, 8)] private int sourcePoolSize = 4;
        [SerializeField] private bool soundEnabled = true;
        [SerializeField] private bool musicEnabled = true;

        private AudioSource[] _sources;
        private AudioSource _musicSource;
        private int _nextSource;

        public bool SoundEnabled => soundEnabled;
        public bool MusicEnabled => musicEnabled;

        private void Awake()
        {
            sourcePoolSize = Mathf.Clamp(sourcePoolSize, 2, 8);
            _sources = new AudioSource[sourcePoolSize];
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.priority = 128 + i;
                _sources[i] = source;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.priority = 200;
            RefreshMusic();
        }

        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
            if (!enabled && _sources != null)
            {
                for (int i = 0; i < _sources.Length; i++) _sources[i].Stop();
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            RefreshMusic();
        }

        public void Play(ToyAudioCue cue)
        {
            if (!soundEnabled || library == null || _sources == null) return;
            ToyAudioEntry entry = library.Find(cue);
            if (entry == null || entry.clip == null) return;
            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Length;
            source.Stop();
            source.clip = entry.clip;
            source.volume = entry.volume;
            source.pitch = entry.pitch;
            source.Play();
        }

        private void RefreshMusic()
        {
            if (_musicSource == null || library == null) return;
            AudioClip clip = library.MusicLoop;
            if (!musicEnabled || clip == null)
            {
                _musicSource.Stop();
                return;
            }
            if (_musicSource.clip != clip) _musicSource.clip = clip;
            _musicSource.volume = library.MusicVolume;
            if (!_musicSource.isPlaying) _musicSource.Play();
        }
    }
}
