using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using LCSoundTool.Utilities;
using System.Runtime.CompilerServices;
using System;
using System.Linq;

namespace LCSoundTool.Networking
{
    public class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; }
        
        // Dictionary to store audio clips with their names as keys
        public static Dictionary<string, AudioClip> networkedAudioClips { get; private set; }

        public static event Action ClientNetworkedAudioChanged;
        public static event Action HostNetworkedAudioChanged;

        public override void OnNetworkSpawn()
        {
            Debug.Log("LCSoundTool - NetworkHandler created!");
            // As the events are static, ensure that they have no subscribes in case players leave a server and join another server
            ClientNetworkedAudioChanged = null;
            HostNetworkedAudioChanged = null;
            networkedAudioClips = new Dictionary<string, AudioClip>();

            Instance = this;
        }

        [ClientRpc]
        public void ReceiveAudioClipClientRpc(string clipName, byte[] audioData, int channels, int frequency)
        {
            // Check if the key (clipName) already exists in the dictionary
            if (!networkedAudioClips.ContainsKey(clipName))
            {
                AudioClip newAudioClip = null;

                // If it doesn't exist, create a new AudioClip and add it to the dictionary
                newAudioClip = AudioUtility.ByteArrayToAudioClip(audioData, clipName, channels, frequency);

                networkedAudioClips.Add(clipName, newAudioClip);
                ClientNetworkedAudioChanged?.Invoke();
            }
            else
            {
                SoundTool.Instance.logger.LogDebug($"Sound {clipName} already exists for this client! Skipping addition of this sound for this client.");
            }

            // Now you can use networkedAudioClips[clipName] for playback or any other logic and it will be the same for everyone
            // This can also be accessed simply with SoundTool.networkedClips
        }

        [ClientRpc]
        public void RemoveAudioClipClientRpc(string clipName)
        {
            if (networkedAudioClips.ContainsKey(clipName))
            {
                networkedAudioClips.Remove(clipName);
                ClientNetworkedAudioChanged?.Invoke();
            }
        }

        [ClientRpc]
        public void SyncAudioClipsClientRpc(string clipName)
        {
            if (!networkedAudioClips.ContainsKey(clipName))
            {
                SendExistingAudioClipServerRpc(clipName);
            }
        }

        [ClientRpc]
        public void ReceiveSeedClientRpc(int seed)
        {
            UnityEngine.Random.InitState(seed);
            SoundTool.Instance.logger.LogDebug($"Client received a new Unity Random seed of {seed}!");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendAudioClipServerRpc(string clipName, byte[] audioData, int channels, int frequency)
        {
            ReceiveAudioClipClientRpc(clipName, audioData, channels, frequency);
            HostNetworkedAudioChanged?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveAudioClipServerRpc(string clipName)
        {
            RemoveAudioClipClientRpc(clipName);
            HostNetworkedAudioChanged?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncAudioClipsServerRpc()
        {
            string[] clipNames = networkedAudioClips.Keys.ToArray();

            if (clipNames.Length < 1)
            {
                SoundTool.Instance.logger.LogDebug("No sounds found in networkedClips. Syncing process cancelled!");
                return;
            }

            for (int i = 0; i < clipNames.Length; i++)
            {
                SyncAudioClipsClientRpc(clipNames[i]);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendExistingAudioClipServerRpc(string clipName)
        {
            if (networkedAudioClips.ContainsKey(clipName))
            {
                byte[] clipData = null;

                clipData = AudioUtility.AudioClipToByteArray(networkedAudioClips[clipName], out float[] samples);

                ReceiveAudioClipClientRpc(clipName, clipData, networkedAudioClips[clipName].channels, networkedAudioClips[clipName].frequency);
            }
            else
            {
                SoundTool.Instance.logger.LogWarning("Trying to obtain and sync a sound from the host that does not exist in the host's game!");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendSeedToClientsServerRpc(int seed)
        {
            if (!IsHost)
                return;

            ReceiveSeedClientRpc(seed);
            SoundTool.Instance.logger.LogDebug($"Sending a new Unity random seed of {seed} to all clients...");
        }
    }
}