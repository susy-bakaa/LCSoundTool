using System.Collections.Generic;
using UnityEngine;

namespace LCSoundTool
{
    public class SoundToolUpdater : MonoBehaviour
    {
        [HideInInspector] public SoundTool originSoundTool;

        public void Update()
        {
            if (originSoundTool != null && originSoundTool != SoundTool.Instance)
            {
                UnityEngine.Object.Destroy(originSoundTool.gameObject);
                return;
            }

            if (originSoundTool.toggleIndepthDebugLog.IsDown() && !originSoundTool.wasKeyDown2)
            {
                originSoundTool.wasKeyDown2 = true;
                originSoundTool.wasKeyDown = false;
            }
            if (originSoundTool.toggleIndepthDebugLog.IsUp() && originSoundTool.wasKeyDown2)
            {
                originSoundTool.wasKeyDown2 = false;
                originSoundTool.wasKeyDown = false;
                SoundTool.debugAudioSources = !SoundTool.debugAudioSources;
                SoundTool.indepthDebugging = SoundTool.debugAudioSources;
                SoundTool.Instance.logger.LogDebug($"Toggling in-depth AudioSource debug logs {SoundTool.debugAudioSources}!");
                return;
            }

            if (!originSoundTool.wasKeyDown2 && !originSoundTool.toggleIndepthDebugLog.IsDown() && originSoundTool.toggleAudioSourceDebugLog.IsDown() && !originSoundTool.wasKeyDown)
            {
                originSoundTool.wasKeyDown = true;
                originSoundTool.wasKeyDown2 = false;
            }
            if (originSoundTool.toggleAudioSourceDebugLog.IsUp() && originSoundTool.wasKeyDown)
            {
                originSoundTool.wasKeyDown = false;
                originSoundTool.wasKeyDown2 = false;
                SoundTool.debugAudioSources = !SoundTool.debugAudioSources;
                SoundTool.Instance.logger.LogDebug($"Toggling AudioSource debug logs {SoundTool.debugAudioSources}!");
            }
        }

        public AudioSource[] GetAllPlayOnAwakeAudioSources()
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
            List<AudioSource> results = new List<AudioSource>();

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].playOnAwake)
                {
                    results.Add(sources[i]);
                }
            }

            return results.ToArray();
        }
    }
}
