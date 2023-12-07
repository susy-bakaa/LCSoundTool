using System.Collections;
using UnityEngine;

namespace LCSoundTool
{
    public class AudioSourceExtension : MonoBehaviour
    {
        public AudioSource audioSource;
        public bool playOnAwake = false;
        public bool loop = false;

        /*void Awake()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Play();

            if (SoundTool.indepthDebugging)
                SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Started playback of {audioSource} with clip {audioSource.clip} in Awake function!");
        }*/

        void OnEnable()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Play();

            if (SoundTool.indepthDebugging)
                SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Started playback of {audioSource} with clip {audioSource.clip} in OnEnable function!");
        }

        void OnDisable()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
                return;

            if (!audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Stop();

            if (SoundTool.indepthDebugging)
                SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Stopped playback of {audioSource} with clip {audioSource.clip} in OnDisable function!");
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
