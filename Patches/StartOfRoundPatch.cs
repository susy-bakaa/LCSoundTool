using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LCSoundTool;
using LCSoundTool.Patches;
using Unity.Netcode;
using UnityEngine;

namespace LCSoundToolMod.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void SpawnNetworkHandler()
        {
            try
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    SoundTool.Instance.logger.LogDebug("Spawning NetworkHandler prefab!");
                    GameNetworkManagerPatch.networkHandlerHost = GameObject.Instantiate(GameNetworkManagerPatch.networkPrefab, Vector3.zero, Quaternion.identity);
                    GameNetworkManagerPatch.networkHandlerHost.GetComponent<NetworkObject>().Spawn(true);
                }
            }
            catch
            {
                SoundTool.Instance.logger.LogError("Failed to spawn NetworkHandler prefab!");
            }
        }
    }
}
