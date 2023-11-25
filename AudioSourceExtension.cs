using System.Collections;
using UnityEngine;

namespace LCSoundTool
{
    public class AudioSourceExtension : MonoBehaviour
    {
        public AudioSource audioSource;
        public bool playOnAwake = false;
        public bool loop = false;

        void Awake()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Play();

            SoundTool.Instance.logger.LogDebug($"Started playback of {audioSource} with clip {audioSource.clip} in Awake function!");
        }

        void Start()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Play();

            SoundTool.Instance.logger.LogDebug($"Started playback of {audioSource} with clip {audioSource.clip} in Start function!");
        }

        /*void Update()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
            {
                audioSource.Play();
                SoundTool.Instance.logger.LogDebug($"Started playback of {audioSource} with clip {audioSource.clip} in Update function!");
            }
        }*/
    }
}
