using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LCSoundTool.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        private static Dictionary<string, AudioClip> originalClips = new Dictionary<string, AudioClip>();

        [HarmonyPatch(nameof(AudioSource.Play), new Type[] { })]
        [HarmonyPrefix]
        public static void Play_Patch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);
            DebugPlayMethod(__instance);
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
        [HarmonyPrefix]
        public static void Play_UlongPatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);
            DebugPlayMethod(__instance);
        }
        [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
        [HarmonyPrefix]
        public static void Play_DoublePatch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);
            DebugPlayMethod(__instance);
        }
        [HarmonyPatch(nameof(AudioSource.PlayDelayed), new[] { typeof(float) })]
        [HarmonyPrefix]
        public static void PlayDelayed_Patch(AudioSource __instance)
        {
            RunDynamicClipReplacement(__instance);
            DebugPlayDelayedMethod(__instance);
        }
        [HarmonyPatch(nameof(AudioSource.PlayClipAtPoint), new[] { typeof(AudioClip), typeof(Vector3), typeof(float) })]
        [HarmonyPrefix]
        public static bool PlayClipAtPoint_Patch(AudioClip clip, Vector3 position, float volume)
        {
            GameObject gameObject = new GameObject("One shot audio");
            gameObject.transform.position = position;
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;

            RunDynamicClipReplacement(audioSource);

            audioSource.Play();

            DebugPlayClipAtPointMethod(audioSource, position);

            UnityEngine.Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));

            return false;
        }
        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), new[] { typeof(AudioSource), typeof(AudioClip), typeof(float) })]
        [HarmonyPrefix]
        public static void PlayOneShotHelper_Patch(AudioSource source, ref AudioClip clip, float volumeScale)
        {
            clip = ReplaceClipWithNew(clip);

            DebugPlayOneShotMethod(source, clip);
        }

        private static void DebugPlayMethod(AudioSource instance)
        {
            if (instance == null)
                return;

            if (SoundTool.debugAudioSources && !SoundTool.indepthDebugging && instance != null)
            {
                SoundTool.Instance.logger.LogDebug($"{instance} at {instance.transform.root} is playing {instance.clip.name}");
            }
            else if (SoundTool.indepthDebugging && instance != null)
            {
                SoundTool.Instance.logger.LogDebug($"{instance} is playing {instance.clip.name} at");

                Transform start = instance.transform;

                while (start.parent != null || start != instance.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {start.parent}");
                    start = start.parent;
                }

                if (start == instance.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {instance.transform.root}");
                }
            }
        }

        private static void DebugPlayDelayedMethod(AudioSource instance)
        {
            if (instance == null)
                return;

            if (SoundTool.debugAudioSources && !SoundTool.indepthDebugging && instance != null)
            {
                SoundTool.Instance.logger.LogDebug($"{instance} at {instance.transform.root} is playing {instance.clip.name} with delay");
            }
            else if (SoundTool.indepthDebugging && instance != null)
            {
                SoundTool.Instance.logger.LogDebug($"{instance} is playing {instance.clip.name} with delay at");

                Transform start = instance.transform;

                while (start.parent != null || start != instance.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {start.parent}");
                    start = start.parent;
                }

                if (start == instance.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {instance.transform.root}");
                }
            }
        }

        private static void DebugPlayClipAtPointMethod(AudioSource audioSource, Vector3 position)
        {
            if (audioSource == null)
                return;

            if (SoundTool.debugAudioSources && !SoundTool.indepthDebugging && audioSource != null)
            {
                SoundTool.Instance.logger.LogDebug($"{audioSource} at {audioSource.transform.root} is playing {audioSource.clip.name} at point {position}");
            }
            else if (SoundTool.indepthDebugging && audioSource != null)
            {
                SoundTool.Instance.logger.LogDebug($"{audioSource} is playing {audioSource.clip.name} located at point {position} within ");

                Transform start = audioSource.transform;

                while (start.parent != null || start != audioSource.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {start.parent}");
                    start = start.parent;
                }

                if (start == audioSource.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {audioSource.transform.root}");
                }
            }
        }

        private static void DebugPlayOneShotMethod(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null)
                return;

            if (SoundTool.debugAudioSources && !SoundTool.indepthDebugging && source != null)
            {
                SoundTool.Instance.logger.LogDebug($"{source} at {source.transform.root} is playing one shot {clip.name}");
            }
            else if (SoundTool.indepthDebugging && source != null)
            {
                SoundTool.Instance.logger.LogDebug($"{source} is playing one shot {clip.name} at");

                Transform start = source.transform;

                while (start.parent != null || start != source.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {start.parent}");
                    start = start.parent;
                }

                if (start == source.transform.root)
                {
                    SoundTool.Instance.logger.LogDebug($"--- {source.transform.root}");
                }
            }
        }

        private static void RunDynamicClipReplacement(AudioSource instance)
        {
            if (instance == null || instance.clip == null) 
                return;

            string clipName = instance.clip.GetName();

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(clipName))
            {
                if (!originalClips.ContainsKey(clipName))
                {
                    originalClips.Add(clipName, instance.clip);
                }

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[clipName];

                // Calculate total chance
                float totalChance = 0f;
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    totalChance += rc.chance;
                }

                // Generate a random value between 0 and totalChance
                float randomValue = UnityEngine.Random.Range(0f, totalChance);

                // Choose the clip based on the random value and chances
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    if (randomValue <= rc.chance)
                    {
                        // Use the chosen audio clip
                        instance.clip = rc.clip;
                        return;
                    }

                    // Subtract the chance of the current clip from randomValue
                    randomValue -= rc.chance;
                }
            }
            // If clipName doesn't exist in the dictionary, check if it exists in the original clips if so use that and remove it
            else if (originalClips.ContainsKey(clipName))
            {
                instance.clip = originalClips[clipName];
                originalClips.Remove(clipName);
            }

            /*if (SoundTool.replacedClips.ContainsKey(clipName))
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
            }*/
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original)
        {
            if (original == null) 
                return original;

            string clipName = original.GetName();

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(clipName))
            {
                if (!originalClips.ContainsKey(clipName))
                {
                    originalClips.Add(clipName, original);
                }

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[clipName];

                // Calculate total chance
                float totalChance = 0f;
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    totalChance += rc.chance;
                }

                // Generate a random value between 0 and totalChance
                float randomValue = UnityEngine.Random.Range(0f, totalChance);

                // Choose the clip based on the random value and chances
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    if (randomValue <= rc.chance)
                    {
                        // Return the chosen audio clip
                        return rc.clip;
                    }

                    // Subtract the chance of the current clip from randomValue
                    randomValue -= rc.chance;
                }
            }
            // If clipName doesn't exist in the dictionary, check if it exists in the original clips if so use that and remove it
            else if (originalClips.ContainsKey(clipName))
            {
                AudioClip temp = originalClips[clipName];
                originalClips.Remove(clipName);
                return temp;
            }

            /*if (SoundTool.replacedClips.ContainsKey(clipName))
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
            }*/

            return original;
        }
    }
}
