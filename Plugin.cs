using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LCSoundDebug.Patches;
using UnityEngine;

namespace LCSoundDebug
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundDebug";
        private const string PLUGIN_NAME = "LC Sound Debugger";
        private const string PLUGIN_VERSION = "1.0.0";

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        public static Plugin Instance;

        internal ManualLogSource logger;

        public static KeyboardShortcut toggleAudioSourceDebugLog;
        public static bool wasKeyDown;

        public static bool debugAudioSources;

        private GameObject soundDebugGameObject;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

                logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

                toggleAudioSourceDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[0]);

                debugAudioSources = false;

                harmony.PatchAll(typeof(AudioSourcePatch));
                //harmony.PatchAll(typeof(Plugin));
            }
        }

        private void Update()
        {
            if (soundDebugGameObject == null)
            {
                GameObject previous = GameObject.Find("SoundDebug");

                if (previous != null)
                {
                    UnityEngine.Object.Destroy(previous);
                }

                soundDebugGameObject = new GameObject("SoundDebug");
                SoundDebug soundDebug = soundDebugGameObject.AddComponent<SoundDebug>();
                soundDebug.source = this;

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

        private void OnDestroy()
        {
            if (soundDebugGameObject == null)
            {
                GameObject previous = GameObject.Find("SoundDebug");

                if (previous != null)
                {
                    UnityEngine.Object.Destroy(previous);
                }

                soundDebugGameObject = new GameObject("SoundDebug");
                SoundDebug soundDebug = soundDebugGameObject.AddComponent<SoundDebug>();
                soundDebug.source = this;

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