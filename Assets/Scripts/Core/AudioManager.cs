using UnityEngine;

namespace StateCraft.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _musicSource;

        private AudioClip _uiClickClip;
        private AudioClip _warAlertClip;
        private AudioClip _notificationClip;

        private AudioClip[] _musicTracks;
        private int _currentTrackIndex = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;

            _uiClickClip = Resources.Load<AudioClip>("Audio/UIClick");
            _warAlertClip = Resources.Load<AudioClip>("Audio/WarAlert");
            _notificationClip = Resources.Load<AudioClip>("Audio/Notification");

            var allClips = Resources.LoadAll<AudioClip>("Audio");
            var tracksList = new System.Collections.Generic.List<AudioClip>();

            foreach (var clip in allClips)
            {
                if (clip == null) continue;
                string clipName = clip.name.ToLower();

                if (clipName == "uiclick" || clipName == "waralert" || clipName == "notification")
                    continue;

                tracksList.Add(clip);
                Debug.Log($"[AudioManager] Added background music track to playlist: {clip.name}");
            }

            for (int i = tracksList.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = tracksList[i];
                tracksList[i] = tracksList[j];
                tracksList[j] = tmp;
            }

            _musicTracks = tracksList.ToArray();
            Debug.Log($"[AudioManager] Loaded {_musicTracks.Length} background music tracks.");

            AudioListener.volume = 0.3f;

            float musicVol = PlayerPrefs.GetFloat("Settings_MusicVolume", 1.0f);
            _musicSource.volume = musicVol;

            if (_musicTracks.Length > 0)
            {
                PlayNextTrack();
            }
        }

        private void Update()
        {
            if (_musicTracks != null && _musicTracks.Length > 0 && _musicSource != null)
            {
                if (!_musicSource.isPlaying && _musicSource.clip != null)
                {
                    PlayNextTrack();
                }
            }
        }

        private void PlayNextTrack()
        {
            if (_musicTracks.Length == 0) return;

            _musicSource.clip = _musicTracks[_currentTrackIndex];
            _musicSource.Play();

            _currentTrackIndex = (_currentTrackIndex + 1) % _musicTracks.Length;
        }

        public void PlayMoodTrack(string mood)
        {
            string targetName = $"statecraft_bgm_{mood}".ToLower();
            foreach (var clip in _musicTracks)
            {
                if (clip != null && clip.name.ToLower() == targetName)
                {
                    _musicSource.Stop();
                    _musicSource.clip = clip;
                    _musicSource.Play();
                    Debug.Log($"[AudioManager] Playing mood track: {clip.name}");
                    return;
                }
            }
            Debug.LogWarning($"[AudioManager] Mood track '{targetName}' not found. Falling back to playlist.");
            PlayNextTrack();
        }

        public void SetMusicVolume(float volume)
        {
            if (_musicSource != null)
                _musicSource.volume = volume;
        }

        public void PlayUIClick()
        {
            if (_sfxSource != null && _uiClickClip != null)
            {
                _sfxSource.PlayOneShot(_uiClickClip, 0.6f);
            }
        }

        public void PlayWarAlert()
        {
            if (_sfxSource != null && _warAlertClip != null)
            {
                _sfxSource.PlayOneShot(_warAlertClip, 1.0f);
            }
        }

        public void PlayNotification()
        {
            if (_sfxSource != null && _notificationClip != null)
            {
                _sfxSource.PlayOneShot(_notificationClip, 0.7f);
            }
        }
    }
}
