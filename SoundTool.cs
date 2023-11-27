using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using LCSoundTool.Patches;
using UnityEngine;
using UnityEngine.Networking;

namespace LCSoundTool
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class SoundTool : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundTool";
        private const string PLUGIN_NAME = "LC Sound Tool";
        private const string PLUGIN_VERSION = "1.2.2";

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        public static SoundTool Instance;

        internal ManualLogSource logger;

        public KeyboardShortcut toggleAudioSourceDebugLog;
        public KeyboardShortcut toggleIndepthDebugLog;
        public bool wasKeyDown;
        public bool wasKeyDown2;

        public static bool debugAudioSources;
        public static bool indepthDebugging;

        private GameObject soundToolGameObject;
        public SoundToolUpdater updater { get; private set; }

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
                toggleIndepthDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[1] { KeyCode.LeftAlt });

                debugAudioSources = false;
                indepthDebugging = false;

                replacedClips = new Dictionary<string, AudioClip>();

                harmony.PatchAll(typeof(AudioSourcePatch));
                harmony.PatchAll(typeof(NetworkSceneManagerPatch));

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
                updater = soundToolGameObject.AddComponent<SoundToolUpdater>();
                updater.originSoundTool = this;

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

        public static AudioClip GetAudioClip(string modFolder, string soundName)
        {
            return GetAudioClip(modFolder, string.Empty, soundName);
        }

        public static AudioClip GetAudioClip(string modFolder, string subFolder, string soundName)
        {
            bool tryLoading = true;
            string legacy = " ";

            // path stuff
            var path = Path.Combine(Paths.PluginPath, modFolder, subFolder, soundName);
            var pathOmitSubDir = Path.Combine(Paths.PluginPath, modFolder, soundName);
            var pathDir = Path.Combine(Paths.PluginPath, modFolder, subFolder);

            var pathLegacy = Path.Combine(Paths.PluginPath, subFolder, soundName);
            var pathDirLegacy = Path.Combine(Paths.PluginPath, subFolder);

            // check if file and directory are valid, else skip loading
            if (!Directory.Exists(pathDir))
            {
                if (!string.IsNullOrEmpty(subFolder))
                    Instance.logger.LogWarning($"Requested directory at BepInEx/Plugins/{modFolder}/{subFolder} does not exist!");
                else
                {
                    Instance.logger.LogWarning($"Requested directory at BepInEx/Plugins/{modFolder} does not exist!");
                    if (!modFolder.Contains("-"))
                        Instance.logger.LogWarning($"This sound mod might not be compatable with mod managers. You should contact the sound mod's author.");
                }
                //Directory.CreateDirectory(pathDir);
                tryLoading = false;
            }
            if (!File.Exists(path))
            {
                Instance.logger.LogWarning($"Requested audio file does not exist at path {path}!");
                tryLoading = false;

                Instance.logger.LogDebug($"Looking for audio file from mod root instead at {pathOmitSubDir}...");
                if (File.Exists(pathOmitSubDir))
                {
                    Instance.logger.LogDebug($"Found audio file at path {pathOmitSubDir}!");
                    path = pathOmitSubDir;
                    tryLoading = true;
                }
                else
                {
                    Instance.logger.LogWarning($"Requested audio file does not exist at mod root path {pathOmitSubDir}!");
                }
            }
            if (Directory.Exists(pathDirLegacy))
            {
                if (!string.IsNullOrEmpty(subFolder))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins/{subFolder} found!");
                else if (!modFolder.Contains("-"))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins found!");
            }
            if (File.Exists(pathLegacy))
            {
                Instance.logger.LogWarning($"Legacy path contains the requested audio file at path {pathLegacy}!");
                legacy = " legacy ";
                path = pathLegacy;
                tryLoading = true;
            }

            AudioClip result = null;

            if (tryLoading)
            {
                Instance.logger.LogDebug($"Loading AudioClip {soundName} from{legacy}path: {path}");
                result = LoadClip(path);
                Instance.logger.LogDebug($"Finished loading AudioClip {soundName} with length of {result.length}!");
            }
            else
            {
                Instance.logger.LogWarning($"Failed to load AudioClip {soundName} from invalid{legacy}path at {path}!");
            }

            // return the clip we got
            return result;
        }

        static AudioClip LoadClip(string path)
        {
            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV))
            {
                uwr.SendWebRequest();

                // we have to wrap tasks in try/catch, otherwise it will just fail silently
                try
                {
                    while (!uwr.isDone)
                    {
                        
                    }

                    if (uwr.result != UnityWebRequest.Result.Success)
                        Instance.logger.LogError($"Failed to load AudioClip from path: {path} Full error: {uwr.error}");
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Instance.logger.LogError($"{err.Message}, {err.StackTrace}");
                }
            }

            return clip;
        }
    }
}