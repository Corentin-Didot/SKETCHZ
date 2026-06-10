using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewAudioLibrary", menuName = "Audio/Library")]
public class AudioLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        public bool is3D = true;

        [Header("3D Settings (actif si is3D = true)")]
        [Tooltip("Distance à partir de laquelle le son commence à s'atténuer")]
        [Min(0.1f)] public float minDistance = 1f;
        [Tooltip("Distance à partir de laquelle le son n'est plus audible")]
        [Min(1f)] public float maxDistance = 50f;
        [Tooltip("Courbe d'atténuation du volume en 3D")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    }

    public List<SoundData> sounds;

    public SoundData GetSound(string _name)
    {
        return sounds.Find(s => s.name == _name);
    }
}