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
            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"instance {instance} instance.clip {instance.clip}");

            if (instance == null || instance.clip == null) 
                return;

            string sourceName = instance.gameObject.name;
            string clipName;
            bool replaceClip = true;

            //if (originalClips.TryGetValue(sourceName, out AudioClip originalClip))
            //{
            //    clipName = originalClip.GetName();
            //    if (SoundTool.infoDebugging)
            //        SoundTool.Instance.logger.LogDebug($"originalClips contained sourceName");
            //}
            //else
            //{
            clipName = instance.clip.GetName();
            //if (SoundTool.infoDebugging)
            //    SoundTool.Instance.logger.LogDebug($"originalClips did not contain sourceName");
            //}

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"sourceName {instance.gameObject.name} clipName {clipName} replaceClip {replaceClip}");

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
                            //if (SoundTool.infoDebugging)
                            //    SoundTool.Instance.logger.LogDebug($"splitName[0] {splitName[0]} splitName[1] {splitName[1]}");

                            if (splitName[0].Contains(clipName) && splitName[1].Contains(instance.gameObject.name))
                            {
                                finalName = $"{clipName}#{splitName[1]}";
                            }
                        }
                    }
                }
            }

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"finalName after splitName operation {finalName}");

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(finalName))
            {
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replacedClips contained finalName");

                if (!SoundTool.replacedClips[finalName].canPlay)
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].canPlay {SoundTool.replacedClips[finalName].canPlay}");
                    return;
                }

                if (!originalClips.ContainsKey(sourceName))
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"originalClips did not contain sourceName, adding sourceName {sourceName}");
                    originalClips.Add(sourceName, instance.clip);
                }

                if (!string.IsNullOrEmpty(SoundTool.replacedClips[finalName].source))
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].source {SoundTool.replacedClips[finalName].source} was not null or empty");

                    replaceClip = false;
                    string[] sources = SoundTool.replacedClips[finalName].source.Split(',');

                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"sources array {sources.Length} {sources}");

                    if (instance != null && instance.gameObject.name != null)
                    {
                        if (sources.Length > 1)
                        {
                            for (int i = 0; i < sources.Length; i++)
                            {
                                if (sources[i] == instance.gameObject.name)
                                {
                                    if (SoundTool.infoDebugging)
                                        SoundTool.Instance.logger.LogDebug($"sources[i] {sources[i]} matches instance.gameObject.name {instance.gameObject.name}");
                                    replaceClip = true;
                                }
                                else
                                {
                                    if (SoundTool.infoDebugging)
                                        SoundTool.Instance.logger.LogDebug($"sources[i] {sources[i]} does not match instance.gameObject.name {instance.gameObject.name}");
                                }
                            }
                        }
                        else
                        {
                            if (sources[0] == instance.gameObject.name)
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"sources[0] {sources[0]} matches instance.gameObject.name {instance.gameObject.name}");
                                replaceClip = true;
                            }
                            else
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"sources[0] {sources[0]} does not match instance.gameObject.name {instance.gameObject.name}");
                            }
                        }
                    }
                }
                else
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].source was empty or null '{SoundTool.replacedClips[finalName].source}'");
                }

                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replaceClip {replaceClip}");

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[finalName].clips;

                // Calculate total chance
                float totalChance = 0f;
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    totalChance += rc.chance;
                }

                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"totalChance {totalChance}");

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
                            if (SoundTool.infoDebugging)
                                SoundTool.Instance.logger.LogDebug($"clip replaced with {rc.clip}");
                            instance.clip = rc.clip;
                            return;
                        }
                        else
                        {
                            if (SoundTool.infoDebugging)
                                SoundTool.Instance.logger.LogDebug($"clip was not replaced with {rc.clip}");
                            if (originalClips.ContainsKey(sourceName))
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"originalClips.ContainsKey(sourceName), clip was restored to {originalClips[sourceName]}");
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
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replacedClips did not contain finalName but originalClips contained sourceName");
                instance.clip = originalClips[sourceName];
                originalClips.Remove(sourceName);
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"clip was restored to {originalClips[sourceName]}");
                return;
            }
        }

        private static AudioClip ReplaceClipWithNew(AudioClip original, AudioSource source = null)
        {
            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"original {original} source {source}");

            if (original == null) 
                return original;

            string clipName = original.GetName();
            bool replaceClip = true;

            string finalName = clipName;

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"sourceName {source.gameObject.name} clipName {clipName} replaceClip {replaceClip}");

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
                                if (splitName[0].Contains(clipName) && splitName[1].Contains(source.gameObject.name))
                                {
                                    finalName = $"{clipName}#{splitName[1]}";
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"source was null, this means we can't check for sourceName for this sound!");
            }

            if (SoundTool.infoDebugging)
                SoundTool.Instance.logger.LogDebug($"finalName after splitName operation {finalName}");

            // Check if clipName exists in the dictionary
            if (SoundTool.replacedClips.ContainsKey(finalName))
            {
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replacedClips contained finalName");

                if (!SoundTool.replacedClips[finalName].canPlay)
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].canPlay {SoundTool.replacedClips[finalName].canPlay}");
                    return original;
                }

                if (!originalClips.ContainsKey(finalName))
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"originalClips did not contain finalName, adding finalName {finalName}");
                    originalClips.Add(finalName, original);
                }

                if (!string.IsNullOrEmpty(SoundTool.replacedClips[finalName].source))
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].source {SoundTool.replacedClips[finalName].source} was not null or empty");

                    replaceClip = false;
                    string[] sources = SoundTool.replacedClips[finalName].source.Split(',');

                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"sources array {sources.Length} {sources}");

                    if (source != null && source.gameObject.name != null)
                    {
                        if (sources.Length > 1)
                        {
                            for (int i = 0; i < sources.Length; i++)
                            {
                                if (sources[i] == source.gameObject.name)
                                {
                                    if (SoundTool.infoDebugging)
                                        SoundTool.Instance.logger.LogDebug($"sources[i] {sources[i]} matches instance.gameObject.name {source.gameObject.name}");
                                    replaceClip = true;
                                }
                                else
                                {
                                    if (SoundTool.infoDebugging)
                                        SoundTool.Instance.logger.LogDebug($"sources[i] {sources[i]} does not match instance.gameObject.name {source.gameObject.name}");
                                }
                            }
                        }
                        else
                        {
                            if (sources[0] == source.gameObject.name)
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"sources[0] {sources[0]} matches instance.gameObject.name {source.gameObject.name}");
                                replaceClip = true;
                            }
                            else
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"sources[0] {sources[0]} does not match instance.gameObject.name {source.gameObject.name}");
                            }
                        }
                    }
                }
                else
                {
                    if (SoundTool.infoDebugging)
                        SoundTool.Instance.logger.LogDebug($"replacedClips[finalName].source was empty or null '{SoundTool.replacedClips[finalName].source}'");
                }

                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replaceClip {replaceClip}");

                List<RandomAudioClip> randomAudioClip = SoundTool.replacedClips[finalName].clips;

                // Calculate total chance
                float totalChance = 0f;
                foreach (RandomAudioClip rc in randomAudioClip)
                {
                    totalChance += rc.chance;
                }

                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"totalChance {totalChance}");

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
                            if (SoundTool.infoDebugging)
                                SoundTool.Instance.logger.LogDebug($"clip replaced with {rc.clip}");
                            return rc.clip;
                        }
                        else
                        {
                            if (SoundTool.infoDebugging)
                                SoundTool.Instance.logger.LogDebug($"clip was not replaced with {rc.clip}");
                            if (originalClips.ContainsKey(finalName))
                            {
                                if (SoundTool.infoDebugging)
                                    SoundTool.Instance.logger.LogDebug($"originalClips.ContainsKey(finalName), clip was restored to {originalClips[finalName]}");
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
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"replacedClips did not contain finalName but originalClips contained finalName");
                AudioClip temp = originalClips[finalName];
                originalClips.Remove(finalName);
                if (SoundTool.infoDebugging)
                    SoundTool.Instance.logger.LogDebug($"clip was restored to {originalClips[finalName]}");
                return temp;
            }

            return original;
        }
        #endregion
    }
}
