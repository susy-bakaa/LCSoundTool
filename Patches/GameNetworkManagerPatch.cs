using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.Assertions;
using UnityEngine;
using LCSoundTool.Resources;

namespace LCSoundTool.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        public static GameObject networkPrefab;
        public static GameObject networkHandlerHost;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void Start_Patch()
        {
            if (networkPrefab != null)
                return;

            SoundTool.Instance.logger.LogDebug("Loading NetworkHandler prefab...");

            networkPrefab = (GameObject)Assets.bundle.LoadAsset<GameObject>("SoundToolNetworkHandler.prefab");

            if (networkPrefab == null)
                SoundTool.Instance.logger.LogError("Failed to load NetworkHandler prefab!");

            if (networkPrefab != null)
            {
                NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

                SoundTool.Instance.logger.LogDebug("Registered NetworkHandler prefab!");
            }
            else
            {
                SoundTool.Instance.logger.LogWarning("Failed to registered NetworkHandler prefab! No networking can take place.");
            }
        }

        [HarmonyPatch("StartDisconnect")]
        [HarmonyPostfix]
        private static void StartDisconnect_Patch()
        {
            try
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    SoundTool.Instance.logger.LogDebug("Destroying NetworkHandler prefab!");
                    UnityEngine.Object.Destroy(networkHandlerHost);
                    networkHandlerHost = null;
                }
            }
            catch
            {
                SoundTool.Instance.logger.LogError("Failed to destroy NetworkHandler prefab!");
            }
        }
    }
}
