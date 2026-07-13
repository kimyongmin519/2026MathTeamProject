using System;
using UnityEngine;

namespace Member.KYM.Scripts
{
    public class MainBGM : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Start()
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
