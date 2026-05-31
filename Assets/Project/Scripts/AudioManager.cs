// ============================================================
//  AudioManager.cs
//  Centralized audio system.
//  - Plays background music with seamless looping & crossfade
//  - SFX pool (no repeated GetComponent overhead)
//  - Volume controls (master / music / sfx) persisted in PlayerPrefs
//  - Spatial 2D audio helpers
//
//  Place in: Assets/Scripts/Managers/
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────
        [Header("Music Sources (two for crossfade)")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;

        [Header("SFX Pool")]
        [SerializeField] private int sfxPoolSize = 12;
        [SerializeField] private Transform sfxPoolParent;

        [Header("Default Volumes")]
        [SerializeField][Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField][Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
        [SerializeField][Range(0f, 1f)] private float defaultSfxVolume = 1f;

        // ── State ─────────────────────────────────────────────────
        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;

        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;

        private Queue<AudioSource> _sfxPool;
        private Coroutine _crossfadeRoutine;

        // PlayerPrefs keys
        private const string KEY_MASTER = "Vol_Master";
        private const string KEY_MUSIC = "Vol_Music";
        private const string KEY_SFX = "Vol_SFX";

        // ── Properties ────────────────────────────────────────────
        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumes(); }
        }
        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); ApplyVolumes(); SaveVolumes(); }
        }
        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); SaveVolumes(); }
        }

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumes();
            BuildSFXPool();
            SetupMusicSources();
        }

        // ──────────────────────────────────────────────────────────
        // Music API
        // ──────────────────────────────────────────────────────────

        /// <summary>Play a music clip immediately (no crossfade).</summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            _activeMusicSource.clip = clip;
            _activeMusicSource.loop = loop;
            _activeMusicSource.volume = _musicVolume * _masterVolume;
            _activeMusicSource.Play();
        }

        /// <summary>Crossfade from current music to a new clip over 'duration' seconds.</summary>
        public void CrossfadeTo(AudioClip newClip, float duration = 1.5f, bool loop = true)
        {
            if (newClip == null) return;
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(newClip, duration, loop));
        }

        public void StopMusic(float fadeTime = 1f)
        {
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(FadeOutRoutine(_activeMusicSource, fadeTime));
        }

        // ──────────────────────────────────────────────────────────
        // SFX API
        // ──────────────────────────────────────────────────────────

        /// <summary>Play a one-shot sound effect at a world position.</summary>
        public void PlaySFX(AudioClip clip, Vector3 worldPos,
                             float pitchVariance = 0.05f, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetPooledSFXSource();
            if (source == null) return;

            source.transform.position = worldPos;
            source.clip = clip;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.Play();

            StartCoroutine(ReturnToPool(source, clip.length + 0.1f));
        }

        /// <summary>Play a 2D (non-spatial) SFX, e.g. UI clicks.</summary>
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            AudioSource source = GetPooledSFXSource();
            if (source == null) return;

            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.Play();

            StartCoroutine(ReturnToPool(source, clip.length + 0.1f));
        }

        // ──────────────────────────────────────────────────────────
        // Private Helpers
        // ──────────────────────────────────────────────────────────
        private void SetupMusicSources()
        {
            _activeMusicSource = musicSourceA;
            _inactiveMusicSource = musicSourceB;
            musicSourceB.volume = 0f;
        }

        private void BuildSFXPool()
        {
            _sfxPool = new Queue<AudioSource>();
            if (sfxPoolParent == null)
            {
                sfxPoolParent = new GameObject("SFXPool").transform;
                sfxPoolParent.SetParent(transform);
            }

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(sfxPoolParent);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f; // 3D by default
                _sfxPool.Enqueue(src);
            }
        }

        private AudioSource GetPooledSFXSource()
        {
            if (_sfxPool.Count == 0) return null;
            return _sfxPool.Dequeue();
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.spatialBlend = 1f;
            source.pitch = 1f;
            _sfxPool.Enqueue(source);
        }

        private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration, bool loop)
        {
            // Swap sources
            (_activeMusicSource, _inactiveMusicSource) =
                (_inactiveMusicSource, _activeMusicSource);

            _activeMusicSource.clip = newClip;
            _activeMusicSource.loop = loop;
            _activeMusicSource.volume = 0f;
            _activeMusicSource.Play();

            float targetVol = _musicVolume * _masterVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _activeMusicSource.volume = Mathf.Lerp(0f, targetVol, t);
                _inactiveMusicSource.volume = Mathf.Lerp(targetVol, 0f, t);
                yield return null;
            }

            _inactiveMusicSource.Stop();
            _activeMusicSource.volume = targetVol;
            _inactiveMusicSource.volume = 0f;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float startVol = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            source.Stop();
        }

        private void ApplyVolumes()
        {
            float target = _musicVolume * _masterVolume;
            if (_activeMusicSource != null && _activeMusicSource.isPlaying)
                _activeMusicSource.volume = target;
        }

        private void LoadVolumes()
        {
            _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, defaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, defaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, defaultSfxVolume);
        }

        private void SaveVolumes()
        {
            PlayerPrefs.SetFloat(KEY_MASTER, _masterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC, _musicVolume);
            PlayerPrefs.SetFloat(KEY_SFX, _sfxVolume);
            PlayerPrefs.Save();
        }
    }
}