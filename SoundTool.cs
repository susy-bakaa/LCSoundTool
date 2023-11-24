using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using LCSoundTool.Patches;
using UnityEngine;

namespace LCSoundTool
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class SoundTool : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundTool";
        private const string PLUGIN_NAME = "LC Sound Tool";
        private const string PLUGIN_VERSION = "1.1.0";

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        public static SoundTool Instance;

        internal ManualLogSource logger;


        public KeyboardShortcut toggleAudioSourceDebugLog;
        public bool wasKeyDown;

        public static bool debugAudioSources;

        private GameObject soundToolGameObject;


        public static Dictionary<string, AudioClip> replacedClips { get; private set; }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip)
        {
            if (string.IsNullOrEmpty(originalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without original clip specified! This is not allowed.");
                return;
            }
            if (newClip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without new clip specified! This is not allowed.");
                return;
            }

            if (replacedClips.ContainsKey(originalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced! This is not allowed.");
                return;
            }

            replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip)
        {
            if (originalClip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without original clip specified! This is not allowed.");
                return;
            }
            if (newClip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without new clip specified! This is not allowed.");
                return;
            }

            ReplaceAudioClip(originalClip.name, newClip);
        }

        public static void RestoreAudioClip(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            if (!replacedClips.ContainsKey(name))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            replacedClips.Remove(name);
        }

        public static void RestoreAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            RestoreAudioClip(clip.name);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

                logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

                toggleAudioSourceDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[0]);

                debugAudioSources = false;

                replacedClips = new Dictionary<string, AudioClip>();

                harmony.PatchAll(typeof(AudioSourcePatch));
                //harmony.PatchAll(typeof(Plugin));

                SoundToolUpdater();
            }
        }

        private void Update()
        {
            SoundToolUpdater();
        }

        private void OnDestroy()
        {
            SoundToolUpdater();
        }

        private void SoundToolUpdater()
        {
            if (soundToolGameObject == null)
            {
                GameObject previous = GameObject.Find("SoundToolUpdater");

                if (previous != null)
                {
                    UnityEngine.Object.Destroy(previous);
                }

                soundToolGameObject = new GameObject("SoundToolUpdater");
                SoundToolUpdater soundToolUpdater = soundToolGameObject.AddComponent<SoundToolUpdater>();
                soundToolUpdater.originSoundTool = this;

                if (Instance != null && Instance != this)
                {
                    UnityEngine.Object.Destroy(Instance);
                    Instance = this;
                }
                else if (Instance == null)
                {
                    Instance = this;
                }
            }
        }

        /*[HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPrefix]
        public static void HookPlayerControllerBUpdate()
        {
            if (toggleAudioSourceDebugLog.IsDown() && !wasKeyDown)
            {
                wasKeyDown = true;
            }
            if (toggleAudioSourceDebugLog.IsUp() && wasKeyDown)
            {
                wasKeyDown = false;
                debugAudioSources = !debugAudioSources;
                Instance.logger.LogDebug($"Toggling AudioSource debug logs {debugAudioSources}!");            }
        }*/
    }
}