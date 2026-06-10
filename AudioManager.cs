using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Librairies Audio")]
    [Tooltip("Glissez ici toutes vos AudioLibrary (Player, Props, UI, Music...)")]
    public AudioLibrary[] libraries;

    private Dictionary<string, AudioLibrary.SoundData> soundDictionary;

    [Header("Settings Globaux")]
    [SerializeField] private int maxSourcesPerObject = 3;

    private AudioSource musicSource;
    private AudioSource SFXSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeDictionary();
        SetupMusicSource();
        SetupSFXSource();
    }

    /// <summary>
    /// Regroupe le contenu de toutes les librairies dans un seul dictionnaire pour une recherche ultra-rapide.
    /// </summary>
    private void InitializeDictionary()
    {
        soundDictionary = new Dictionary<string, AudioLibrary.SoundData>();

        foreach (AudioLibrary lib in libraries)
        {
            if (lib == null) continue;

            foreach (var sound in lib.sounds)
            {
                if (!soundDictionary.ContainsKey(sound.name))
                {
                    soundDictionary.Add(sound.name, sound);
                }
                else
                {
                    Debug.LogWarning($"Attention : Le son '{sound.name}' existe en double dans vos librairies !");
                }
            }
        }
    }

    private void SetupMusicSource()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.name = "MusicSource";
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.playOnAwake = false;
    }

    private void SetupSFXSource()
    {
        SFXSource = gameObject.AddComponent<AudioSource>();
        SFXSource.name = "SFXSource";
        SFXSource.loop = false;
        SFXSource.spatialBlend = 0f;
        SFXSource.playOnAwake = false;
    }

    private AudioLibrary.SoundData GetSoundData(string _soundName)
    {
        if (soundDictionary.TryGetValue(_soundName, out AudioLibrary.SoundData data))
        {
            return data;
        }
        Debug.LogWarning($"Le son '{_soundName}' n'a été trouvé dans aucune de vos librairies.");
        return null;
    }

    // ==========================================
    // GESTION DE LA MUSIQUE
    // ==========================================

    public void PlayMusic(string _name)
    {
        var data = GetSoundData(_name);
        if (data == null) return;

        musicSource.clip = data.clip;
        musicSource.volume = data.volume;
        musicSource.loop = data.loop;
        musicSource.outputAudioMixerGroup = data.mixerGroup;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Fonction pour changer de musique avec un fondu
    /// </summary>
    /// <param name="_nextMusicName"></param>
    /// <param name="_fadeDuration"></param>
    public void SwitchMusic(string _nextMusicName, float _fadeDuration = 1.5f)
    {
        StartCoroutine(MusicFadeTransition(_nextMusicName, _fadeDuration));
    }

    private IEnumerator MusicFadeTransition(string _nextMusicName, float _duration)
    {
        if (musicSource != null)
        {
            var data = GetSoundData(_nextMusicName);
            if (data == null) yield break;

            float startVolume = musicSource.volume;

            if (musicSource.isPlaying)
            {
                for (float t = 0; t < _duration; t += Time.deltaTime)
                {
                    musicSource.volume = Mathf.Lerp(startVolume, 0, t / _duration);
                    yield return null;
                }
            }

            musicSource.clip = data.clip;
            musicSource.loop = data.loop;
            musicSource.outputAudioMixerGroup = data.mixerGroup;
            musicSource.Play();

            for (float t = 0; t < _duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, data.volume, t / _duration);
                yield return null;
            }
            musicSource.volume = data.volume;
        }
    }

    // ==========================================
    // GESTION DES EFFETS SONORES (SFX)
    // ==========================================

    public void PlaySound(string _soundName, GameObject _sender, bool _isOneShot = false)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        AudioSource source;

        if (_isOneShot)
        {
            // PlayOneShot ne supporte pas le loop : on force un avertissement si le son est configuré en loop
            if (data.loop)
                Debug.LogWarning($"Le son '{_soundName}' est configuré en loop mais joué en OneShot : le loop sera ignoré.");

            source = _sender.GetComponent<AudioSource>();
            if (source == null) source = SearchOrCreateAudioSource(_sender,data);

            ApplySourceSettings(source, data);

            source.loop = false;

            source.PlayOneShot(data.clip, data.volume);
        }
        else
        {
            source = SearchOrCreateAudioSource(_sender, data);

            ApplySourceSettings(source, data);


            source.Stop();
            source.clip = data.clip;
            source.volume = data.volume;
            source.loop = data.loop; 
            source.Play();
        }
    }

    public void PlaySound(string _soundName)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        ApplySourceSettings(SFXSource, data);

        SFXSource.Stop();
        SFXSource.clip = data.clip;
        SFXSource.volume = data.volume;
        SFXSource.loop = data.loop;
        SFXSource.Play();
    }

    /// <summary>
    /// Joue un son et exécute une action (callback) une fois que le son est terminé.
    /// </summary>
    public void PlaySoundWithCallback(string _soundName, GameObject _sender, Action _onComplete, bool _isOneShot = false)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null)
        {
            _onComplete?.Invoke();
            return;
        }

        PlaySound(_soundName, _sender, _isOneShot);

        float pitchMultiplier = Mathf.Abs(data.pitch) > 0.01f ? Mathf.Abs(data.pitch) : 1f;
        float actualDuration = data.clip.length / pitchMultiplier;

        StartCoroutine(WaitForSoundRoutine(actualDuration, _onComplete));
    }

    private IEnumerator WaitForSoundRoutine(float _duration, Action _onComplete)
    {
        yield return new WaitForSeconds(_duration);

        _onComplete?.Invoke();
    }

    public void PlaySoundAtLocation(string _soundName, Vector3 _position)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        AudioSource.PlayClipAtPoint(data.clip, _position, data.volume);
    }

    public void StopSound(string _soundName, GameObject _target)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        AudioSource[] sources = _target.GetComponentsInChildren<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source.clip == data.clip && source.isPlaying)
            {
                source.Stop();
                source.clip = null;
                return;
            }
        }
    }

    /// <summary>
    /// Retourne l'AudioSource en cours de lecture pour un son donné sur un GameObject.
    /// Permet de cacher la référence et de la modifier directement sans recherche à chaque frame.
    /// </summary>
    public AudioSource GetAudioSource(string _soundName, GameObject _target)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return null;

        AudioSource[] sources = _target.GetComponents<AudioSource>();
        foreach (AudioSource source in sources)
        {
            if (source.clip == data.clip)
                return source;
        }
        return null;
    }

    public void SetRandomPitch(string _soundName, GameObject _target, float _minPitch, float _maxPitch)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        AudioSource[] sources = _target.GetComponentsInChildren<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source.clip == data.clip)
            {
                source.pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
                return;
            }
        }
    }

    public void SetVolume(string _soundName, GameObject _target, float _volume)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return;

        AudioSource[] sources = _target.GetComponents<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source.clip == data.clip)
            {
                source.volume = _volume;
            }
        }
    }

    public void TransitionToSnapshot(AudioMixerSnapshot _snapshot, float _duration)
    {
        if (_snapshot == null) return;
        _snapshot.TransitionTo(_duration);
    }

    /// --------------------
    ///        TOOLS
    /// --------------------

    /// <summary>
    /// Applique tous les paramètres communs (mixer, pitch, spatialBlend, distances 3D) sur une source.
    /// Centralisé ici pour éviter les oublis lors de la réutilisation d'une source recyclée.
    /// </summary>
    private void ApplySourceSettings(AudioSource _source, AudioLibrary.SoundData _data)
    {
        _source.outputAudioMixerGroup = _data.mixerGroup;
        _source.pitch = _data.pitch;
        _source.loop = _data.loop;

        if (_data.is3D)
        {
            _source.spatialBlend = 1.0f;
            _source.minDistance = _data.minDistance;
            _source.maxDistance = _data.maxDistance;
            _source.rolloffMode = _data.rolloffMode;
        }
        else
        {
            _source.spatialBlend = 0.0f;
        }
    }

    private AudioSource SearchOrCreateAudioSource(GameObject _target, AudioLibrary.SoundData _data)
    {
        AudioSource[] allSources = _target.GetComponents<AudioSource>();

        foreach (AudioSource s in allSources)
        {
            if (!s.isPlaying) return s;
        }

        if (allSources.Length < maxSourcesPerObject)
        {
            AudioSource newSource = _target.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = _data.loop;
            newSource.Stop();
            return newSource;
        }

        Debug.LogWarning($"Limite de sources atteinte sur {_target.name}, recyclage d'une source active.");
        return allSources[0];
    }

    public bool HasSound(string _name)
    {
        return soundDictionary.ContainsKey(_name);
    }

    /// <summary>
    /// Vérifie si un son spécifique est en cours de lecture sur un GameObject.
    /// </summary>
    public bool IsSoundPlaying(string _soundName, GameObject _target)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return false;

        AudioSource[] sources = _target.GetComponents<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source.clip == data.clip && source.isPlaying)
            {
                return true;
            }
        }

        return false;
    }

    public void SetPitch(string _soundName , float _newPitch)
    {
        var data = GetSoundData(_soundName);
        if (data == null || data.clip == null) return ;

        data.pitch = _newPitch;
    }

    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
}