using UnityEngine;

namespace LCSoundDebug
{
    public class SoundDebug : MonoBehaviour
    {
        [HideInInspector] public Plugin source;

        public void Update()
        {
            if (source != null && source != Plugin.Instance)
            {
                return;
            }

            if (Plugin.toggleAudioSourceDebugLog.IsDown() && !Plugin.wasKeyDown)
            {
                Plugin.wasKeyDown = true;
            }
            if (Plugin.toggleAudioSourceDebugLog.IsUp() && Plugin.wasKeyDown)
            {
                Plugin.wasKeyDown = false;
                Plugin.debugAudioSources = !Plugin.debugAudioSources;
                Plugin.Instance.logger.LogDebug($"Toggling AudioSource debug logs {Plugin.debugAudioSources}!");
            }
        }
    }
}
