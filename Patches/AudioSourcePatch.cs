using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace LCSoundTool.Patches
{
    [HarmonyPatch(typeof(AudioSource))]
    internal class AudioSourcePatch
    {
        private static Dictionary<string, AudioClip> originalClips = new Dictionary<string, AudioClip>();

        #region HARMONY PATCHES

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
            // You can use this naming convention to identify these ClipAtPoint sounds for your replacements.
            GameObject gameObject = new GameObject($"ClipAtPoint_{clip}");
            gameObject.transform.position = position;
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;

            // Don't think we need to call this seperately, as Play() should do it already cuz it is patched.
            // RunDynamicClipReplacement(audioSource);

            audioSource.Play();

            DebugPlayClipAtPointMethod(audioSource, position);

            UnityEngine.Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));

            return false;
        }
        [HarmonyPatch(nameof(AudioSource.PlayOneShotHelper), new[] { typeof(AudioSource), typeof(AudioClip), typeof(float) })]
        [HarmonyPrefix]
        public static void PlayOneShotHelper_Patch(AudioSource source, ref AudioClip clip, float volumeScale)
        {
            clip = ReplaceClipWithNew(clip, source);

            DebugPlayOneShotMethod(source, clip);
        }
        #endregion

        #region DEBUG METHODS
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
        #endregion

        #region DYNAMIC CLIP REPLACEMENT METHODS
        private static void RunDynamicClipReplacement(AudioSource instance)
        {
            if (instance == null || instance.clip == null) 
                return;

            string sourceName = instance.gameObject.name;
            string clipName;
            bool replaceClip = true;

            if (originalClips.TryGetValue(sourceName, out AudioClip originalClip))
            {
                clipName = originalClip.GetName();
            }
            else
            {
                clipName = instance.clip.GetName();
            }

            string finalName = clipName;

            if (SoundTool.replacedClips.Keys.Count > 0)
            {
                string[] keys = SoundTool.replacedClips.Keys.ToArray();

                if (keys.Length > 0)
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        string[] splitName = keys[i].Split("#");

                        if (splitName.Length == 2)
                        {
                            if (splitName[1].Contains(instance.gameObject.name))
                            {
                                finalName = $"{clipName}#{splitName[1]}";
                            }
                        }
                    }
                }
            }

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(finalName))
            {
                if (!SoundTool.replacedClips[finalName].canPlay)
                {
                    return;
                }

                if (!originalClips.ContainsKey(sourceName))
                {
                    originalClips.Add(sourceName, instance.clip);
                }

                if (!string.IsNullOrEmpty(SoundTool.replacedClips[finalName].source))
                {
                    replaceClip = false;
                    string[] sources = SoundTool.replacedClips[finalName].source.Split(',');

                    if (instance != null && instance.gameObject.name != null)
                    {
                        if (sources.Length > 1)
                        {
                            for (int i = 0; i < sources.Length; i++)
                            {
                                if (sources[i] == instance.gameObject.name)
                                {
                                    replaceClip = true;
                                }
                            }
                        }
                        else
                        {
                            if (sources[0] == instance.gameObject.name)
                            {
                                replaceClip = true;
                            }
                        }
                    }
                }

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[finalName].clips;

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
                        // Return the chosen audio clip if allowed, otherwise revert it to vanilla and return the vanilla sound instead.
                        if (replaceClip)
                        {
                            instance.clip = rc.clip;
                            return;
                        }
                        else
                        {
                            if (originalClips.ContainsKey(sourceName))
                            {
                                instance.clip = originalClips[sourceName];
                                originalClips.Remove(sourceName);
                                return;
                            }
                            return;
                        }
                    }

                    // Subtract the chance of the current clip from randomValue
                    randomValue -= rc.chance;
                }
            }
            // If clipName doesn't exist in the dictionary, check if it exists in the original clips if so use that and remove it
            else if (originalClips.ContainsKey(sourceName))
            {
                instance.clip = originalClips[sourceName];
                originalClips.Remove(sourceName);
                return;
            }
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original, AudioSource source = null)
        {
            if (original == null) 
                return original;

            string clipName = original.GetName();
            bool replaceClip = true;

            string finalName = clipName;

            if (source != null)
            {
                if (SoundTool.replacedClips.Keys.Count > 0)
                {
                    string[] keys = SoundTool.replacedClips.Keys.ToArray();

                    if (keys.Length > 0)
                    {
                        for (int i = 0; i < keys.Length; i++)
                        {
                            string[] splitName = keys[i].Split("#");

                            if (splitName.Length == 2)
                            {
                                if (splitName[1].Contains(source.gameObject.name))
                                {
                                    finalName = $"{clipName}#{splitName[1]}";
                                }
                            }
                        }
                    }
                }
            }

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(finalName))
            {
                if (!SoundTool.replacedClips[finalName].canPlay)
                {
                    return original;
                }

                if (!originalClips.ContainsKey(finalName))
                {
                    originalClips.Add(finalName, original);
                }

                if (!string.IsNullOrEmpty(SoundTool.replacedClips[finalName].source))
                {
                    replaceClip = false;
                    string[] sources = SoundTool.replacedClips[finalName].source.Split(',');

                    if (source != null && source.gameObject.name != null)
                    {
                        if (sources.Length > 1)
                        {
                            for (int i = 0; i < sources.Length; i++)
                            {
                                if (sources[i] == source.gameObject.name)
                                {
                                    replaceClip = true;
                                }
                            }
                        }
                        else
                        {
                            if (sources[0] == source.gameObject.name)
                            {
                                replaceClip = true;
                            }
                        }
                    }
                }

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[finalName].clips;

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
                        // Return the chosen audio clip if allowed, otherwise revert it to vanilla and return the vanilla sound instead.
                        if (replaceClip)
                        {
                            return rc.clip;
                        }
                        else
                        {
                            if (originalClips.ContainsKey(finalName))
                            {
                                AudioClip temp = originalClips[finalName];
                                originalClips.Remove(finalName);
                                return temp;
                            }
                            return original;
                        }
                    }

                    // Subtract the chance of the current clip from randomValue
                    randomValue -= rc.chance;
                }
            }
            // If clipName doesn't exist in the dictionary, check if it exists in the original clips if so use that and remove it
            else if (originalClips.ContainsKey(finalName))
            {
                AudioClip temp = originalClips[finalName];
                originalClips.Remove(finalName);
                return temp;
            }

            return original;
        }
        #endregion
    }
}
