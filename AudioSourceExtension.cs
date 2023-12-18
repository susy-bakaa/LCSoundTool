using System.Collections;
using UnityEngine;

namespace LCSoundTool
{
    public class AudioSourceExtension : MonoBehaviour
    {
        public AudioSource audioSource;
        public bool playOnAwake = false;
        public bool loop = false;

        private bool updateHasBeenLogged = false;
        private bool hasPlayed = false;

        void OnEnable()
        {
            if (audioSource == null)
                return;

            //if (audioSource.clip == null)
            //    return;

            if (audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Play();

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Started playback of {audioSource} with clip {audioSource.clip} in OnEnable function!");
            updateHasBeenLogged = false;
            hasPlayed = false;
        }

        void OnDisable()
        {
            if (audioSource == null)
                return;

            //if (audioSource.clip == null)
            //    return;

            if (!audioSource.isPlaying)
                return;

            if (playOnAwake)
                audioSource.Stop();

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Stopped playback of {audioSource} with clip {audioSource.clip} in OnDisable function!");
            updateHasBeenLogged = false;
            hasPlayed = false;
        }

        void Update()
        {
            if (audioSource == null)
                return;

            if (audioSource.clip == null)
            {
                hasPlayed = false;
            }

            if (audioSource.isPlaying)
            {
                updateHasBeenLogged = false;
                return;
            }

            if (!audioSource.isActiveAndEnabled)
            {
                hasPlayed = false;
                return;
            }

            if (playOnAwake)
            {
                if (audioSource.clip != null && !hasPlayed)
                {
                    audioSource.Play();
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Started playback of {audioSource} with clip {audioSource.clip} in Update function!");
                    updateHasBeenLogged = false;
                    hasPlayed = true;
                }
                else if (!updateHasBeenLogged)
                {
                    updateHasBeenLogged = true;
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"(AudioSourceExtension) Can not start playback of {audioSource} with missing clip in Update function!");
                }
            }
        }
    }
}
