using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace LCSoundTool.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        private static Dictionary<string, AudioClip> originalClips = new Dictionary<string, AudioClip>();

        [HarmonyPatch(nameof(AudioSource.Play), new Type[] { })]
        [HarmonyPrefix]
        public static void PlayPatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);

            if (SoundTool.debugAudioSources && __instance != null)
                SoundTool.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
        [HarmonyPrefix]
        public static void PlayUlongPatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);

            if (SoundTool.debugAudioSources && __instance != null)
                SoundTool.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
        [HarmonyPrefix]
        public static void PlayDoublePatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);

            if (SoundTool.debugAudioSources && __instance != null)
                SoundTool.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root} is playing {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.PlayDelayed), new[] { typeof(float) })]
        [HarmonyPrefix]
        public static void PlayDelayPatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);

            if (SoundTool.debugAudioSources && __instance != null)
                SoundTool.Instance.logger.LogDebug($"{__instance} at {__instance.transform.root}  is playing  {__instance.clip.name}");
        }
        [HarmonyPatch(nameof(AudioSource.PlayClipAtPoint), new[] { typeof(AudioClip), typeof(Vector3), typeof(float) })]
        [HarmonyPrefix]
        public static bool PlayClipAtPointPatch(AudioClip clip, Vector3 position, float volume)
        {
            GameObject gameObject = new GameObject("One shot audio");
            gameObject.transform.position = position;
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;

            RunDynamicClipReplacement(audioSource);

            audioSource.Play();

            if (SoundTool.debugAudioSources)
                SoundTool.Instance.logger.LogDebug($"{audioSource} at {audioSource.transform.root} is playing {audioSource.clip.name} at point {position}");

            UnityEngine.Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));

            return false;
        }
        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), new[] { typeof(AudioSource), typeof(AudioClip), typeof(float) })]
        [HarmonyPrefix]
        public static void PlayOneShotPatch(AudioSource source, ref AudioClip clip, float volumeScale)
        {
            clip = ReplaceClipWithNew(clip);

            if (SoundTool.debugAudioSources)
                SoundTool.Instance.logger.LogDebug($"{source} at {source.transform.root} is playing one shot {clip.name}");
        }

        private static void RunDynamicClipReplacement(AudioSource instance)
        {
            if (instance == null || instance.clip == null) return;

            string clipName = instance.clip.GetName();

            if (SoundTool.replacedClips.ContainsKey(clipName))
            {
                if (!originalClips.ContainsKey(clipName))
                {
                    originalClips.Add(clipName, instance.clip);
                }

                instance.clip = SoundTool.replacedClips[clipName];
            }
            else if (originalClips.ContainsKey(clipName))
            {
                instance.clip = originalClips[clipName];
                originalClips.Remove(clipName);
            }
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original)
        {
            if (original == null) return original;

            string clipName = original.GetName();

            if (SoundTool.replacedClips.ContainsKey(clipName))
            {
                if (!originalClips.ContainsKey(clipName))
                {
                    originalClips.Add(clipName, original);
                }

                return SoundTool.replacedClips[clipName];
            }
            else if (originalClips.ContainsKey(clipName))
            {
                AudioClip temp = originalClips[clipName];
                originalClips.Remove(clipName);
                return temp;
            }

            return original;
        }
    }
}
