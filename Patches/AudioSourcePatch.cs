using System;
using HarmonyLib;
using UnityEngine;

namespace LCSoundDebug.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        [HarmonyPatch(nameof(AudioSource.Play), new Type[] { })]
        [HarmonyPrefix]
        public static void LogPlayingAudio(AudioSource __instance)
        {
            if (Plugin.debugAudioSources)
                Plugin.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
        [HarmonyPrefix]
        public static void LogPlayingAudio2(AudioSource __instance)
        {
            if (Plugin.debugAudioSources)
                Plugin.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
        [HarmonyPrefix]
        public static void LogPlayingAudio3(AudioSource __instance)
        {
            if (Plugin.debugAudioSources)
                Plugin.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.PlayDelayed), new[] { typeof(float) })]
        [HarmonyPrefix]
        public static void LogPlayingDelayedAudio(AudioSource __instance)
        {
            if (Plugin.debugAudioSources)
                Plugin.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root}  is playing  {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.PlayClipAtPoint), new[] { typeof(AudioClip), typeof(Vector3), typeof(float) })]
        public static void LogPlayingClipAtPointAudio2(AudioSource ___audioSource)
        {
            if (Plugin.debugAudioSources)
                Plugin.Instance.logger.LogDebug($"{___audioSource} at {___audioSource.transform.root} is playing {___audioSource.clip.name} at point");
        }
        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper))]
        [HarmonyPrefix]
        public static void LogOneShotPlayingAudio2(AudioSource source, AudioClip clip)
        {
            if (Plugin.debugAudioSources)
            {
                Plugin.Instance.logger.LogDebug($"{source} at {source.transform.root} is playing one shot {clip.name}");
            }
        }
    }
}
