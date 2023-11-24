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

            if (originSoundTool.toggleAudioSourceDebugLog.IsDown() && !originSoundTool.wasKeyDown)
            {
                originSoundTool.wasKeyDown = true;
            }
            if (originSoundTool.toggleAudioSourceDebugLog.IsUp() && originSoundTool.wasKeyDown)
            {
                originSoundTool.wasKeyDown = false;
                SoundTool.debugAudioSources = !SoundTool.debugAudioSources;
                SoundTool.Instance.logger.LogDebug($"Toggling AudioSource debug logs {SoundTool.debugAudioSources}!");
            }
        }
    }
}
