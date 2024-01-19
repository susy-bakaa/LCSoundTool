using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using LCSoundTool.Networking;
using LCSoundTool.Patches;
using LCSoundTool.Resources;
using LCSoundTool.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LCSoundTool
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, LCSoundToolMod.PluginInfo.PLUGIN_VERSION)]
    public class SoundTool : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundTool";
        private const string PLUGIN_NAME = "LC Sound Tool";
        //private const string PLUGIN_VERSION = "1.3.2";

        private ConfigEntry<bool> configUseNetworking;
        private ConfigEntry<bool> configSyncRandomSeed;
        private ConfigEntry<float> configPlayOnAwakePatchRepeatDelay;
        private ConfigEntry<bool> configPrintInfoByDefault;

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        public static SoundTool Instance;

        internal ManualLogSource logger;

        public KeyboardShortcut toggleAudioSourceDebugLog;
        public KeyboardShortcut toggleIndepthDebugLog;
        public KeyboardShortcut toggleInformationalDebugLog;
        public KeyboardShortcut printAllSoundsDebugLog;
        public bool wasKeyDown;
        public bool wasKeyDown2;
        public bool wasKeyDown3;
        public bool wasKeyDown4;

        public static bool debugAudioSources;
        public static bool indepthDebugging;
        public static bool infoDebugging;

        public static bool IsDebuggingOn()
        {
            if (debugAudioSources || indepthDebugging || infoDebugging)
                return true;
            else
                return false;
        }

        public static bool networkingInitialized { get; private set; }
        public static bool networkingAvailable { get; private set; }

        public static Dictionary<string, ReplacementAudioClip> replacedClips { get; private set; }
        public static Dictionary<string, AudioClip> networkedClips { get { return NetworkHandler.networkedAudioClips; } }

        public static event Action ClientNetworkedAudioChanged { add { NetworkHandler.ClientNetworkedAudioChanged += value; } remove { NetworkHandler.ClientNetworkedAudioChanged -= value; } }
        public static event Action HostNetworkedAudioChanged { add { NetworkHandler.HostNetworkedAudioChanged += value; } remove { NetworkHandler.HostNetworkedAudioChanged -= value; } }

        public static Dictionary<string, AudioType> clipTypes { get; private set; }

        public enum AudioType { wav, ogg, mp3 }

        #region UNITY METHODS
        private void Awake()
        {
            networkingAvailable = true;

            if (Instance == null)
            {
                Instance = this;
            }

            configUseNetworking = Config.Bind("Experimental", "EnableNetworking", false, "Whether or not to use the networking built into this plugin. If set to true everyone in the lobby needs LCSoundTool installed and networking enabled to join.");
            configSyncRandomSeed = Config.Bind("Experimental", "SyncUnityRandomSeed", false, "Whether or not to sync the default Unity randomization seed with all clients. For this feature, networking has to be set to true. Will send the UnityEngine.Random.seed from the host to all clients automatically upon loading a networked scene.");
            configPlayOnAwakePatchRepeatDelay = Config.Bind("Experimental", "NewPlayOnAwakePatchRepeatDelay", 90f, "How long to wait between checks for new playOnAwake AudioSources. Runs the same patching that is done when each scene is loaded with this delay between each run. DO NOT set too low or high. Anything below 10 or above 600 can cause issues. This time is in seconds. Set to 0 to disable rerunning the patch, but be warned that this might break runtime initialized playOnAwake AudioSources.");
            configPrintInfoByDefault = Config.Bind("Logging", "PrintInfoByDefault", false, "Whether or not to print additional information logs created by this mod by default. If set to false, informational logs may be toggled on any time with LeftAlt + F5.");

            // NetcodePatcher stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

            logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            toggleAudioSourceDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[0]);
            toggleIndepthDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[1] { KeyCode.LeftAlt });
            toggleInformationalDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[1] { KeyCode.LeftControl });
            printAllSoundsDebugLog = new KeyboardShortcut(KeyCode.F5, new KeyCode[1] { KeyCode.LeftShift });

            debugAudioSources = false;
            indepthDebugging = false;
            if (configPrintInfoByDefault.Value)
                infoDebugging = true;
            else
                infoDebugging = false;

            replacedClips = new Dictionary<string, ReplacementAudioClip>();
            clipTypes = new Dictionary<string, AudioType>();
        }

        private void Start()
        {
            if (!configUseNetworking.Value)
            {
                networkingAvailable = false;
                Instance.logger.LogWarning($"Networking disabled. Mod in fully client side mode, but no networked actions can take place! You can safely ignore this if you want the mod to run fully client side.");
            }
            else
            {
                networkingAvailable = true;
            }

            if (configUseNetworking.Value)
            {
                logger.LogDebug("Loading SoundTool AssetBundle...");

                // AssetBundle for NetworkHandler gameobject
                Assets.bundle = AssetBundle.LoadFromMemory(LCSoundToolMod.Properties.Resources.soundtool);

                if (Assets.bundle == null)
                {
                    logger.LogError("Failed to load SoundTool AssetBundle!");
                }
                else
                {
                    logger.LogDebug("Finished loading SoundTool AssetBundle!");
                }
            }

            harmony.PatchAll(typeof(AudioSourcePatch));
            //harmony.PatchAll(typeof(NetworkSceneManagerPatch));
            if (configUseNetworking.Value)
            {
                harmony.PatchAll(typeof(GameNetworkManagerPatch));
                harmony.PatchAll(typeof(StartOfRoundPatch));
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Update()
        {
            if (configUseNetworking.Value)
            {
                if (!networkingInitialized)
                {
                    if (NetworkHandler.Instance != null)
                    {
                        networkingInitialized = true;
                    }
                }
                else
                {
                    if (NetworkHandler.Instance == null)
                    {
                        networkingInitialized = false;
                    }
                }
            }
            else
            {
                networkingInitialized = false;
            }

            if (printAllSoundsDebugLog.IsDown() && !wasKeyDown4)
            {
                wasKeyDown4 = true;
            }
            if (printAllSoundsDebugLog.IsUp() && wasKeyDown4)
            {
                wasKeyDown4 = false;

                Instance.logger.LogDebug($"Printing all currently replaced sounds...");
                Instance.logger.LogDebug($" ");

                string[] keys = replacedClips.Keys.ToArray();

                for (int i = 0; i < replacedClips.Count; i++)
                {
                    ReplacementAudioClip rClip = replacedClips[keys[i]];

                    Instance.logger.LogDebug($"Clip named {keys[i]} with {rClip.clips.Count} replacement clip(s)");
                    Instance.logger.LogDebug($"- Clip can play? {rClip.canPlay}");
                    Instance.logger.LogDebug($"- Clip audio source(s)? {rClip.source}");
                    Instance.logger.LogDebug($"- All {rClip.clips.Count} clip(s):");
                    for (int k = 0; k < rClip.clips.Count; k++)
                    {
                        Instance.logger.LogDebug($"-- Clip {k + 1} - {rClip.clips[k].clip.GetName()} with chance of {Mathf.RoundToInt(rClip.clips[k].chance * 100f)}%");
                    }
                }
                Instance.logger.LogDebug($" ");
                Instance.logger.LogDebug($"Finished printing all currently replaced sounds!");
            }

            if (toggleInformationalDebugLog.IsDown() && !wasKeyDown3)
            {
                wasKeyDown3 = true;
                wasKeyDown2 = false;
                wasKeyDown = false;
            }
            if (toggleInformationalDebugLog.IsUp() && wasKeyDown3)
            {
                wasKeyDown3 = false;
                wasKeyDown2 = false;
                wasKeyDown = false;
                infoDebugging = !infoDebugging;
                Instance.logger.LogDebug($"Toggling informational debug logs {infoDebugging}!");
                return;
            }

            if (toggleIndepthDebugLog.IsDown() && !wasKeyDown2)
            {
                wasKeyDown2 = true;
                wasKeyDown = false;
            }
            if (toggleIndepthDebugLog.IsUp() && wasKeyDown2)
            {
                wasKeyDown2 = false;
                wasKeyDown = false;
                debugAudioSources = !debugAudioSources;
                indepthDebugging = debugAudioSources;
                infoDebugging = debugAudioSources;
                Instance.logger.LogDebug($"Toggling in-depth AudioSource debug logs {debugAudioSources}!");
                return;
            }

            if (!wasKeyDown2 && !toggleIndepthDebugLog.IsDown() && toggleAudioSourceDebugLog.IsDown() && !wasKeyDown)
            {
                wasKeyDown = true;
                wasKeyDown2 = false;
            }
            if (toggleAudioSourceDebugLog.IsUp() && wasKeyDown)
            {
                wasKeyDown = false;
                wasKeyDown2 = false;
                debugAudioSources = !debugAudioSources;
                if (indepthDebugging && !debugAudioSources)
                    indepthDebugging = false;
                Instance.logger.LogDebug($"Toggling AudioSource debug logs {debugAudioSources}!");
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion

        #region SCENE LOAD SETUP METHODS
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance == null)
                return;

            PatchPlayOnAwakeAudio(scene);

            OnSceneLoadedNetworking();

            if (scene.name.ToLower().Contains("level"))
            {
                StopAllCoroutines();
                StartCoroutine(PatchPlayOnAwakeDelayed(scene, 1f));
            }
        }

        private IEnumerator PatchPlayOnAwakeDelayed(Scene scene, float wait)
        {
            if (infoDebugging)
                logger.LogDebug($"Started playOnAwake patch coroutine with delay of {wait} seconds");
            yield return new WaitForSecondsRealtime(wait);
            if (infoDebugging)
                logger.LogDebug($"Running playOnAwake patch coroutine!");

            PatchPlayOnAwakeAudio(scene);

            float repeatWait = configPlayOnAwakePatchRepeatDelay.Value;

            if (repeatWait != 0f)
            {
                if (repeatWait < 10f)
                    repeatWait = 10f;
                if (repeatWait > 600f)
                    repeatWait = 600f;

                StartCoroutine(PatchPlayOnAwakeDelayed(scene, repeatWait));
            }
        }

        private void PatchPlayOnAwakeAudio(Scene scene)
        {
            if (infoDebugging)
                Instance.logger.LogDebug($"Grabbing all playOnAwake AudioSources for loaded scene {scene.name}");

            AudioSource[] sources = GetAllPlayOnAwakeAudioSources();

            if (infoDebugging)
            {
                Instance.logger.LogDebug($"Found a total of {sources.Length} playOnAwake AudioSource(s)!");
                Instance.logger.LogDebug($"Starting setup on {sources.Length} playOnAwake AudioSource(s)...");
            }

            foreach (AudioSource s in sources)
            {
                s.Stop();

                if (s.transform.TryGetComponent(out AudioSourceExtension sExt))
                {
                    sExt.audioSource = s;
                    sExt.playOnAwake = true;
                    sExt.loop = s.loop;
                    s.playOnAwake = false;
                    if (infoDebugging)
                        Instance.logger.LogDebug($"-Set- {System.Array.IndexOf(sources, s) + 1} {s} done!");
                }
                else
                {
                    AudioSourceExtension sExtNew = s.gameObject.AddComponent<AudioSourceExtension>();
                    sExtNew.audioSource = s;
                    sExtNew.playOnAwake = true;
                    sExtNew.loop = s.loop;
                    s.playOnAwake = false;
                    if (infoDebugging)
                        Instance.logger.LogDebug($"-Add- {System.Array.IndexOf(sources, s) + 1} {s} done!");
                }
            }

            if (infoDebugging)
                Instance.logger.LogDebug($"Done setting up {sources.Length} playOnAwake AudioSources!");
        }

        private void OnSceneLoadedNetworking()
        {
            if (networkingAvailable && networkingInitialized)
            {
                if (configSyncRandomSeed.Value == true)
                {
                    int newSeed = (int)DateTime.Now.Ticks;

                    UnityEngine.Random.InitState(newSeed);

                    SendUnityRandomSeed(newSeed);
                }
            }
        }

        public AudioSource[] GetAllPlayOnAwakeAudioSources()
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
            List<AudioSource> results = new List<AudioSource>();

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].playOnAwake)
                {
                    results.Add(sources[i]);
                }
            }

            return results.ToArray();
        }
        #endregion

        #region REPLACEMENT METHODS
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

            string finalName = originalName;
            string clipName = newClip.GetName();
            string source = string.Empty;
            string tag = string.Empty;
            float chance = 100f;
            
            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
            string[] parts = clipName.Split('-');

            if (infoDebugging)
                Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

            for (int i = 0; i < parts.Length; i++)
            {
                bool isNumeric = int.TryParse(parts[i], out _);

                if (infoDebugging)
                    Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                if (!isNumeric && !parts[i].Contains('_'))
                {
                    tag = parts[i];
                }
            }

            if (parts.Length > 1)
            {
                if (parts.Length > 2)
                {
                    string secondLastPart = parts[parts.Length - 2];
                    if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                    {
                        source = secondLastPart.Substring(1);
                    }
                }

                string lastPart = parts[parts.Length - 1];

                if (int.TryParse(lastPart, out int parsedChance))
                {
                    chance = parsedChance * 0.01f;
                }
                else
                {
                    if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                    {
                        source = lastPart.Substring(1);
                    }
                }

                clipName = string.Join('-', parts, 0, parts.Length - 1);

                if (infoDebugging)
                    Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
            }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(source))
            {
                finalName = $"{originalName}#{source}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, source, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100%. Currently {finalTotalChance}% (at least yet?)");
            } else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
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

            ReplaceAudioClip(originalClip.GetName(), newClip);
        }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip, float chance)
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

            string finalName = originalName;
            string clipName = newClip.GetName();
            string source = string.Empty;
            string tag = string.Empty;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
                string[] parts = clipName.Split('-');

                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isNumeric = int.TryParse(parts[i], out _);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                    if (!isNumeric && !parts[i].Contains('_'))
                    {
                        tag = parts[i];
                    }
                }

                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            source = secondLastPart.Substring(1);
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        Instance.logger.LogDebug($"Clip {clipName} contains random chance specified in it's name ({parsedChance}%) but you're using the manually function to assign it's chance. ({Mathf.RoundToInt(chance * 100f)}%) The file name will be ignored.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            source = lastPart.Substring(1);
                        }
                    }

                    clipName = string.Join('-', parts, 0, parts.Length - 1);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(source))
            {
                finalName = $"{originalName}#{source}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, source, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100% (at least yet?)");
            }
            else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip, float chance)
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

            ReplaceAudioClip(originalClip.GetName(), newClip, chance);
        }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip, string source)
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
            if (string.IsNullOrEmpty(source))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without allowed audio source specified! This is not allowed.");
                return;
            }

            string finalName = originalName;
            string clipName = newClip.GetName();
            string tag = string.Empty;
            float chance = 100f;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
                string[] parts = clipName.Split('-');

                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isNumeric = int.TryParse(parts[i], out _);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                    if (!isNumeric && !parts[i].Contains('_'))
                    {
                        tag = parts[i];
                    }
                }

                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({secondLastPart.Substring(1)}) but you're using the manual function to assign it's source. ({source}) The file name will be ignored.");
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        chance = parsedChance * 0.01f;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({lastPart.Substring(1)}) but you're using the manual function to assign it's source. ({source}) The file name will be ignored.");
                        }
                    }

                    clipName = string.Join('-', parts, 0, parts.Length - 1);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(source))
            {
                finalName = $"{originalName}#{source}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, source, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100%. Currently {finalTotalChance}% (at least yet?)");
            }
            else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip, string source)
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

            ReplaceAudioClip(originalClip.GetName(), newClip, source);
        }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip, string[] source)
        {
            string finalSource = string.Empty;

            if (source != null && source.Length > 0)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!string.IsNullOrEmpty(source[i]))
                        finalSource = $"{finalSource},{source[i]}";
                }
            }

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
            if (string.IsNullOrEmpty(finalSource))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without allowed audio source specified! This is not allowed.");
                return;
            }

            string finalName = originalName;
            string clipName = newClip.GetName();
            string tag = string.Empty;
            float chance = 100f;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
                string[] parts = clipName.Split('-');

                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isNumeric = int.TryParse(parts[i], out _);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                    if (!isNumeric && !parts[i].Contains('_'))
                    {
                        tag = parts[i];
                    }
                }

                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({secondLastPart.Substring(1)}) but you're using the manual function to assign it's source. ({finalSource}) The file name will be ignored.");
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        chance = parsedChance * 0.01f;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({lastPart.Substring(1)}) but you're using the manual function to assign it's source. ({finalSource}) The file name will be ignored.");
                        }
                    }

                    clipName = string.Join('-', parts, 0, parts.Length - 1);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(finalSource))
            {
                finalName = $"{originalName}#{finalSource}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, finalSource, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100%. Currently {finalTotalChance}% (at least yet?)");
            }
            else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip, string[] source)
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

            ReplaceAudioClip(originalClip.GetName(), newClip, source);
        }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip, float chance, string source)
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
            if (string.IsNullOrEmpty(source))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without allowed audio source specified! This is not allowed.");
                return;
            }

            string finalName = originalName;
            string clipName = newClip.GetName();
            string tag = string.Empty;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
                string[] parts = clipName.Split('-');

                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isNumeric = int.TryParse(parts[i], out _);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                    if (!isNumeric && !parts[i].Contains('_'))
                    {
                        tag = parts[i];
                    }
                }

                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({secondLastPart.Substring(1)}) but you're using the manual function to assign it's source. ({source}) The file name will be ignored.");
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        Instance.logger.LogDebug($"Clip {clipName} contains random chance specified in it's name ({parsedChance}%) but you're using the manual function to assign it's chance. ({Mathf.RoundToInt(chance * 100f)}%) The file name will be ignored.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({lastPart.Substring(1)}) but you're using the manual function to assign it's source. ({source}) The file name will be ignored.");
                        }
                    }

                    clipName = string.Join('-', parts, 0, parts.Length - 1);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(source))
            {
                finalName = $"{originalName}#{source}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, source, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100%. Currently {finalTotalChance}% (at least yet?)");
            }
            else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip, float chance, string source)
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

            ReplaceAudioClip(originalClip.GetName(), newClip, chance, source);
        }

        public static void ReplaceAudioClip(string originalName, AudioClip newClip, float chance, string[] source)
        {
            string finalSource = string.Empty;

            if (source != null && source.Length > 0)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!string.IsNullOrEmpty(source[i]))
                        finalSource = $"{finalSource},{source[i]}";
                }
            }

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
            if (string.IsNullOrEmpty(finalSource))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip without allowed audio source specified! This is not allowed.");
                return;
            }

            string finalName = originalName;
            string clipName = newClip.GetName();
            string tag = string.Empty;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains('-'))
            {
                string[] parts = clipName.Split('-');

                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} contains {parts.Length} parts in it's name.");

                for (int i = 0; i < parts.Length; i++)
                {
                    bool isNumeric = int.TryParse(parts[i], out _);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"-- (Part {i + 1}) string {parts[i]}, isNumeric {isNumeric}, isSourceName {parts[i].Contains('_')}");

                    if (!isNumeric && !parts[i].Contains('_'))
                    {
                        tag = parts[i];
                    }
                }

                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({secondLastPart.Substring(1)}) but you're using the manual function to assign it's source. ({finalSource}) The file name will be ignored.");
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        Instance.logger.LogDebug($"Clip {clipName} contains random chance specified in it's name ({parsedChance}%) but you're using the manual function to assign it's chance. ({Mathf.RoundToInt(chance * 100f)}%) The file name will be ignored.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            Instance.logger.LogDebug($"Clip {clipName} contains source specified in it's name ({lastPart.Substring(1)}) but you're using the manual function to assign it's source. ({finalSource}) The file name will be ignored.");
                        }
                    }

                    clipName = string.Join('-', parts, 0, parts.Length - 1);

                    if (infoDebugging)
                        Instance.logger.LogDebug($"ClipName after reparsing {clipName}");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
            }

            if (!string.IsNullOrEmpty(finalSource))
            {
                finalName = $"{originalName}#{finalSource}";
            }

            if (replacedClips.ContainsKey(finalName) && chance >= 100f)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to replace an audio clip that already has been replaced with new clip that has 100% chance of playback! This is not allowed.");
                return;
            }

            // Ensure the chance is within the valid range
            chance = Mathf.Clamp01(chance);

            // If the clipName already exists in the dictionary, add the new audio clip with its chance
            if (replacedClips.ContainsKey(finalName))
            {
                replacedClips[finalName].AddClip(newClip, chance, tag);
            }
            // If the clipName doesn't exist, create a new entry in the dictionary
            else
            {
                replacedClips.Add(finalName, new ReplacementAudioClip(newClip, chance, finalSource, tag));
            }

            float totalChance = 0;

            for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
            {
                totalChance += replacedClips[finalName].clips[i].chance;
            }

            int finalTotalChance = Mathf.RoundToInt(totalChance * 100f);

            if ((finalTotalChance < 100 || finalTotalChance > 100) && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} does not equal 100%. Currently {finalTotalChance}% (at least yet?)");
            }
            else if (finalTotalChance == 100 && replacedClips[finalName].clips.Count() > 1)
            {
                if (infoDebugging)
                    Instance.logger.LogDebug($"The current total combined chance for replaced {replacedClips[finalName].clips.Count()} random audio clips for audio clip {finalName} is equal to 100%");
            }

            //replacedClips.Add(originalName, newClip);
        }

        public static void ReplaceAudioClip(AudioClip originalClip, AudioClip newClip, float chance, string[] source)
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

            ReplaceAudioClip(originalClip.GetName(), newClip, chance, source);
        }

        public static void RemoveRandomAudioClip(string name, float chance)
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

            if (chance > 0f)
            {
                for (int i = 0; i < replacedClips[name].clips.Count(); i++)
                {
                    if (replacedClips[name].clips[i].chance == chance)
                    {
                        replacedClips[name].clips.RemoveAt(i);
                        
                        if (replacedClips[name].clips.Count <= 0)
                        {
                            Instance.logger.LogDebug($"Removed replaced AudioClip {name} completely as all of it's random clips have been removed.");
                            replacedClips.Remove(name);
                        }
                        break;
                    }
                }
            }
        }

        public static void RemoveRandomAudioClip(string name, string source, float chance)
        {
            string finalName = name;

            if (!string.IsNullOrEmpty(finalName))
            {
                finalName = $"{name}#{source}";
            }

            if (string.IsNullOrEmpty(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            if (!replacedClips.ContainsKey(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            if (chance > 0f)
            {
                for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
                {
                    if (replacedClips[finalName].clips[i].chance == chance)
                    {
                        replacedClips[finalName].clips.RemoveAt(i);

                        if (replacedClips[finalName].clips.Count <= 0)
                        {
                            Instance.logger.LogDebug($"Removed replaced AudioClip {finalName} completely as all of it's random clips have been removed.");
                            replacedClips.Remove(finalName);
                        }
                        break;
                    }
                }
            }
        }

        public static void RemoveRandomAudioClip(string name, string[] source, float chance)
        {
            string finalName = name;
            string finalSource = string.Empty;

            if (source != null && source.Length > 0)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!string.IsNullOrEmpty(source[i]))
                        finalSource = $"{finalSource},{source[i]}";
                }
            }

            if (!string.IsNullOrEmpty(finalSource))
            {
                finalName = $"{name}#{finalSource}";
            }

            if (string.IsNullOrEmpty(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            if (!replacedClips.ContainsKey(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            if (chance > 0f)
            {
                for (int i = 0; i < replacedClips[finalName].clips.Count(); i++)
                {
                    if (replacedClips[finalName].clips[i].chance == chance)
                    {
                        replacedClips[finalName].clips.RemoveAt(i);

                        if (replacedClips[finalName].clips.Count <= 0)
                        {
                            Instance.logger.LogDebug($"Removed replaced AudioClip {finalName} completely as all of it's random clips have been removed.");
                            replacedClips.Remove(finalName);
                        }
                        break;
                    }
                }
            }
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

        public static void RestoreAudioClip(string name, string source)
        {
            string finalName = name;

            if (!string.IsNullOrEmpty(source))
            {
                finalName = $"{name}#{source}";
            }

            if (string.IsNullOrEmpty(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            if (!replacedClips.ContainsKey(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            replacedClips.Remove(finalName);
        }

        public static void RestoreAudioClip(string name, string[] source)
        {
            string finalName = name;
            string finalSource = string.Empty;

            if (source != null && source.Length > 0)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!string.IsNullOrEmpty(source[i]))
                        finalSource = $"{finalSource},{source[i]}";
                }
            }

            if (!string.IsNullOrEmpty(finalSource))
            {
                finalName = $"{name}#{finalSource}";
            }

            if (string.IsNullOrEmpty(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without original clip specified! This is not allowed.");
                return;
            }

            if (!replacedClips.ContainsKey(finalName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            replacedClips.Remove(finalName);
        }

        public static void RestoreAudioClip(AudioClip clip)
        {
            if (clip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without vanilla clip name specified! This is not allowed.");
                return;
            }

            RestoreAudioClip(clip.GetName());
        }

        public static void RestoreAudioClip(string name, AudioClip replacementClip)
        {
            if (replacementClip == null)
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without replacement clip specified! This is not allowed.");
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip without vanilla clip name specified! This is not allowed.");
                return;
            }

            // -V- This section is just copied from the clip replacement function -V-

            string clipName = replacementClip.GetName();
            string source = string.Empty;
            float chance = 0f;

            // If clipName contains "-number" or/and "-source", parse the chance and audio source name
            if (clipName.Contains("-"))
            {
                string[] parts = clipName.Split('-');
                if (parts.Length > 1)
                {
                    if (parts.Length > 2)
                    {
                        string secondLastPart = parts[parts.Length - 2];
                        if (!string.IsNullOrEmpty(secondLastPart) && secondLastPart.StartsWith("_"))
                        {
                            source = secondLastPart.Substring(1);
                        }
                    }

                    string lastPart = parts[parts.Length - 1];

                    if (int.TryParse(lastPart, out int parsedChance))
                    {
                        chance = parsedChance * 0.01f;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(lastPart) && lastPart.StartsWith("_"))
                        {
                            source = lastPart.Substring(1);
                        }
                    }

                    clipName = string.Join("-", parts, 0, parts.Length - 1);
                }
                else
                {
                    if (infoDebugging)
                        Instance.logger.LogDebug($"Clip {clipName} does not contain a '-' character for source name or chance");
                }
            }

            // -/\- This section is just copied from the clip replacement function -/\-

            string constructedName = $"{name}#{source}";

            if (!replacedClips.ContainsKey(constructedName))
            {
                Instance.logger.LogWarning($"Plugin {PLUGIN_GUID} is trying to restore an audio clip that does not exist! This is not allowed.");
                return;
            }

            RestoreAudioClip(constructedName);
        }
        #endregion

        #region CLIP LOADING METHODS
        public static AudioClip GetAudioClip(string modFolder, string soundName)
        {
            return GetAudioClip(modFolder, string.Empty, soundName);
        }

        public static AudioClip GetAudioClip(string modFolder, string subFolder, string soundName)
        {
            AudioType audioType = AudioType.wav;
            bool tryLoading = true;
            bool skipLegacyCheck = false;
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
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
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
                    if (infoDebugging)
                        Instance.logger.LogDebug("Skipping legacy path check...");
                    skipLegacyCheck = true;
                }
                else
                {
                    Instance.logger.LogWarning($"Requested audio file does not exist at mod root path {pathOmitSubDir}!");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
            }
            if (Directory.Exists(pathDirLegacy) && !skipLegacyCheck)
            {
                if (!string.IsNullOrEmpty(subFolder))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins/{subFolder} found!");
                else if (!modFolder.Contains("-"))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins found!");
            }
            if (File.Exists(pathLegacy) && !skipLegacyCheck)
            {
                Instance.logger.LogWarning($"Legacy path contains the requested audio file at path {pathLegacy}!");
                legacy = " legacy ";
                path = pathLegacy;
                tryLoading = true;
            }

            string[] parts = soundName.Split('.');
            if (parts[parts.Length - 1].ToLower().Contains("wav"))
            {
                audioType = AudioType.wav;
                if (infoDebugging)
                    Instance.logger.LogDebug($"File detected as a PCM WAVE file!");
            }
            else if (parts[parts.Length - 1].ToLower().Contains("ogg"))
            {
                audioType = AudioType.ogg;
                if (infoDebugging)
                    Instance.logger.LogDebug($"File detected as an Ogg Vorbis file!");
            }
            else if (parts[parts.Length - 1].ToLower().Contains("mp3"))
            {
                audioType = AudioType.mp3;
                if (infoDebugging)
                    Instance.logger.LogDebug($"File detected as a MPEG MP3 file!");
            }
            else
            {
                audioType = AudioType.wav;
                Instance.logger.LogWarning($"Failed to detect file type of a sound file! This may cause issues with other mod functionality. Sound defaulted to WAV. Sound: {soundName}");
            }

            AudioClip result = null;

            if (tryLoading)
            {
                Instance.logger.LogDebug($"Loading AudioClip {soundName} from{legacy}path: {path}");

                switch (audioType)
                {
                    case AudioType.wav:
                        result = WavUtility.LoadFromDiskToAudioClip(path);
                        break;
                    case AudioType.ogg:
                        result = OggUtility.LoadFromDiskToAudioClip(path);
                        break;
                    case AudioType.mp3:
                        result = Mp3Utility.LoadFromDiskToAudioClip(path);
                        break;
                }

                Instance.logger.LogDebug($"Finished loading AudioClip {soundName} with length of {result.length}!");
            }
            else
            {
                Instance.logger.LogWarning($"Failed to load AudioClip {soundName} from invalid{legacy}path at {path}!");
            }

            // Workaround to ensure the clip always gets named because for some reason Unity doesn't always get the name and leaves it blank sometimes???
            if (string.IsNullOrEmpty(result.GetName()))
            {
                string finalName = string.Empty;
                string[] nameParts = new string[0];

                switch (audioType)
                {
                    case AudioType.wav:

                        finalName = soundName.Replace(".wav", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"finalName from nameParts array {finalName}");

                        result.name = finalName;
                        break;
                    case AudioType.ogg:
                        finalName = soundName.Replace(".ogg", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"finalName from nameParts array {finalName}");

                        result.name = finalName;
                        break;
                    case AudioType.mp3:
                        finalName = soundName.Replace(".mp3", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        result.name = finalName;
                        break;
                }
            }

            if (result != null)
            {
                string clipName = result.GetName();

                if (clipTypes.ContainsKey(clipName))
                {
                    clipTypes[clipName] = audioType;
                }
                else
                {
                    clipTypes.Add(clipName, audioType);
                }
            }

            // return the clip we got
            return result;
        }

        public static AudioClip GetAudioClip(string modFolder, string soundName, AudioType audioType)
        {
            return GetAudioClip(modFolder, string.Empty, soundName, audioType);
        }

        public static AudioClip GetAudioClip(string modFolder, string subFolder, string soundName, AudioType audioType)
        {
            bool tryLoading = true;
            bool skipLegacyCheck = false;
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
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
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
                    if (infoDebugging)
                        Instance.logger.LogDebug("Skipping legacy path check...");
                    skipLegacyCheck = true;
                }
                else
                {
                    Instance.logger.LogWarning($"Requested audio file does not exist at mod root path {pathOmitSubDir}!");
                }
            }
            else
            {
                if (infoDebugging)
                    Instance.logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
            }
            if (Directory.Exists(pathDirLegacy) && !skipLegacyCheck)
            {
                if (!string.IsNullOrEmpty(subFolder))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins/{subFolder} found!");
                else if (!modFolder.Contains("-"))
                    Instance.logger.LogWarning($"Legacy directory location at BepInEx/Plugins found!");
            }
            if (File.Exists(pathLegacy) && !skipLegacyCheck)
            {
                Instance.logger.LogWarning($"Legacy path contains the requested audio file at path {pathLegacy}!");
                legacy = " legacy ";
                path = pathLegacy;
                tryLoading = true;
            }

            switch (audioType)
            {
                case AudioType.wav:
                    if (infoDebugging)
                        Instance.logger.LogDebug($"File defined as a WAV file!");
                    break;
                case AudioType.ogg:
                    if (infoDebugging)
                        Instance.logger.LogDebug($"File defined as an Ogg Vorbis file!");
                    break;
                case AudioType.mp3:
                    if (infoDebugging)
                        Instance.logger.LogDebug($"File defined as a MPEG MP3 file!");
                    break;
                default:
                    if (infoDebugging)
                        Instance.logger.LogDebug($"File type not defined and was defaulted to WAV file!");
                    break;
            }

            AudioClip result = null;

            if (tryLoading)
            {
                Instance.logger.LogDebug($"Loading AudioClip {soundName} from{legacy}path: {path}");

                switch (audioType)
                {
                    case AudioType.wav:
                        result = WavUtility.LoadFromDiskToAudioClip(path);
                        break;
                    case AudioType.ogg:
                        result = OggUtility.LoadFromDiskToAudioClip(path);
                        break;
                    case AudioType.mp3:
                        result = Mp3Utility.LoadFromDiskToAudioClip(path);
                        break;
                }

                Instance.logger.LogDebug($"Finished loading AudioClip {soundName} with length of {result.length}!");
            }
            else
            {
                Instance.logger.LogWarning($"Failed to load AudioClip {soundName} from invalid{legacy}path at {path}!");
            }

            // Workaround to ensure the clip always gets named because for some reason Unity doesn't always get the name and leaves it blank sometimes???
            if (string.IsNullOrEmpty(result.GetName()))
            {
                string finalName = string.Empty;
                string[] nameParts = new string[0];

                switch (audioType)
                {
                    case AudioType.wav:

                        finalName = soundName.Replace(".wav", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"finalName from nameParts array {finalName}");

                        result.name = finalName;
                        break;
                    case AudioType.ogg:
                        finalName = soundName.Replace(".ogg", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"finalName from nameParts array {finalName}");

                        result.name = finalName;
                        break;
                    case AudioType.mp3:
                        finalName = soundName.Replace(".mp3", "");

                        if (infoDebugging)
                            Instance.logger.LogDebug($"soundName {soundName}, finalName {finalName}");

                        nameParts = finalName.Split('/');

                        if (infoDebugging)
                            Instance.logger.LogDebug($"nameParts length {nameParts.Length}");

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[nameParts.Length - 1];

                        if (infoDebugging)
                            Instance.logger.LogDebug($"finalName from nameParts array {finalName}");

                        result.name = finalName;
                        break;
                }
            }

            if (result != null)
            {
                string clipName = result.GetName();

                if (clipTypes.ContainsKey(clipName))
                {
                    clipTypes[clipName] = audioType;
                }
                else
                {
                    clipTypes.Add(clipName, audioType);
                }
            }

            // return the clip we got
            return result;
        }
        #endregion

        #region AUDIO NETWORKING METHODS
        public static void SendNetworkedAudioClip(AudioClip audioClip)
        {
            if (!Instance.configUseNetworking.Value)
            {
                Instance.logger.LogWarning($"Networking disabled! Failed to send {audioClip}!");
                return;
            }

            if (audioClip == null)
            {
                Instance.logger.LogWarning($"audioClip variable of SendAudioClip not assigned! Failed to send {audioClip}!");
                return;
            }

            if (Instance == null || GameNetworkManagerPatch.networkHandlerHost == null || NetworkHandler.Instance == null)
            {
                Instance.logger.LogWarning($"Instance of SoundTool not found or networking has not finished initializing. Failed to send {audioClip}! If you're sending things in Awake or in a scene such as the main menu it might be too early, please try some of the other built-in Unity methods and make sure your networked audio runs only after the player setups a networked connection!");
                return;
            }

            string clipName = audioClip.GetName();

            if (clipTypes.ContainsKey(clipName))
            {
                if (clipTypes[clipName] == AudioType.ogg)
                {
                    NetworkHandler.Instance.SendAudioClipServerRpc(clipName, OggUtility.AudioClipToByteArray(audioClip, out float[] samplesOgg));
                    return;
                }
                else if (clipTypes[clipName] == AudioType.mp3)
                {
                    NetworkHandler.Instance.SendAudioClipServerRpc(clipName, Mp3Utility.AudioClipToByteArray(audioClip, out float[] samplesMp3));
                    return;
                }
            }

            NetworkHandler.Instance.SendAudioClipServerRpc(clipName, WavUtility.AudioClipToByteArray(audioClip, out float[] samplesWav));
        }

        public static void RemoveNetworkedAudioClip(AudioClip audioClip)
        {
            RemoveNetworkedAudioClip(audioClip.GetName());
        }

        public static void RemoveNetworkedAudioClip(string audioClip)
        {
            if (!Instance.configUseNetworking.Value)
            {
                Instance.logger.LogWarning($"Networking disabled! Failed to remove {audioClip}!");
                return;
            }

            if (string.IsNullOrEmpty(audioClip))
            {
                Instance.logger.LogWarning($"audioClip variable of RemoveAudioClip not assigned! Failed to remove {audioClip}!");
                return;
            }

            if (Instance == null || GameNetworkManagerPatch.networkHandlerHost == null || NetworkHandler.Instance == null)
            {
                Instance.logger.LogWarning($"Instance of SoundTool not found or networking has not finished initializing. Failed to remove {audioClip}! If you're removing things in Awake or in a scene such as the main menu it might be too early, please try some of the other built-in Unity methods and make sure your networked audio runs only after the player setups a networked connection!");
                return;
            }

            NetworkHandler.Instance.RemoveAudioClipServerRpc(audioClip);
        }

        public static void SyncNetworkedAudioClips()
        {
            if (!Instance.configUseNetworking.Value)
            {
                Instance.logger.LogWarning($"Networking disabled! Failed to sync audio clips!");
                return;
            }

            if (Instance == null || GameNetworkManagerPatch.networkHandlerHost == null || NetworkHandler.Instance == null)
            {
                Instance.logger.LogWarning($"Instance of SoundTool not found or networking has not finished initializing. Failed to sync networked audio! If you're syncing things in Awake or in a scene such as the main menu it might be too early, please try some of the other built-in Unity methods and make sure your networked audio runs only after the player setups a networked connection!");
                return;
            }

            NetworkHandler.Instance.SyncAudioClipsServerRpc();
        }

        public static void SendUnityRandomSeed(int seed)
        {
            if (!Instance.configUseNetworking.Value)
            {
                Instance.logger.LogWarning($"Networking disabled! Failed to send Unity random seed!");
                return;
            }

            if (Instance == null || GameNetworkManagerPatch.networkHandlerHost == null || NetworkHandler.Instance == null)
            {
                Instance.logger.LogWarning($"Instance of SoundTool not found or networking has not finished initializing. Failed to send Unity Random seed! If you're sending the seed in Awake or in a scene such as the main menu it might be too early, please try some of the other built-in Unity methods and make sure your networked methods run only after the player setups a networked connection!");
                return;
            }

            NetworkHandler.Instance.SendSeedToClientsServerRpc(seed);
        }
        #endregion
    }
}
