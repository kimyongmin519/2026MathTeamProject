using UnityEngine;

namespace KimLIb.SoundSystem
{
    public enum AudioTypes
    {
        Sfx,Music
    }
    
    [CreateAssetMenu(fileName = "Sound clip data", menuName = "Sound/Clip data", order = 10)]
    public class SoundClipSO : ScriptableObject
    {
        public AudioTypes audioTypes;
        public AudioClip audioClip;
        public bool loop = false;
        public bool randomizePitch = false;

        [Range(0, 1f)] 
        public float randomPitchModifier;

        [Range(0.1f, 2f)]
        public float volume = 1f;
        
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }
}