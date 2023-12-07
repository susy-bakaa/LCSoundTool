using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LCSoundTool.Patches
{
    [HarmonyPatch(typeof(NetworkSceneManager))]
    internal class NetworkSceneManagerPatch
    {
        [HarmonyPatch("OnSceneLoaded")]
        [HarmonyPostfix]
        public static void OnSceneLoaded_Patch()
        {
            if (SoundTool.Instance == null)
                return;

            SoundTool.Instance.logger.LogDebug($"Grabbing all playOnAwake AudioSources...");

            AudioSource[] sources = SoundTool.Instance.GetAllPlayOnAwakeAudioSources();

            SoundTool.Instance.logger.LogDebug($"Found {sources.Length} playOnAwake AudioSources!");
            SoundTool.Instance.logger.LogDebug($"Starting setup on {sources.Length/* - 3*/} compatable playOnAwake AudioSources...");

            foreach (AudioSource s in sources)
            {
                //if (!s.name.Contains("ThrusterCloseAudio") && !s.name.Contains("ThrusterAmbientAudio") && !s.name.Contains("Ship3dSFX"))
                //{
                    if (s.transform.TryGetComponent(out AudioSourceExtension sExt))
                    {
                        sExt.playOnAwake = true;
                        sExt.audioSource = s;
                        sExt.loop = s.loop;
                        s.playOnAwake = false;
                        SoundTool.Instance.logger.LogDebug($"-Set- {System.Array.IndexOf(sources, s) + 1} {s} done!");
                    }
                    else
                    {
                        AudioSourceExtension sExtNew = s.gameObject.AddComponent<AudioSourceExtension>();
                        sExtNew.audioSource = s;
                        sExtNew.playOnAwake = true;
                        sExtNew.loop = s.loop;
                        s.playOnAwake = false;
                        SoundTool.Instance.logger.LogDebug($"-Add- {System.Array.IndexOf(sources, s) + 1} {s} done!");
                    }
                //}
            }

            SoundTool.Instance.logger.LogDebug($"Done setting up {sources.Length/* - 3*/} compatable playOnAwake AudioSources!");
        }
    }
}
