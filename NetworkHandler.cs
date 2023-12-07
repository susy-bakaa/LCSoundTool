using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using LCSoundTool.Utilities;
using System.Runtime.CompilerServices;
using System;

namespace LCSoundTool.Networking
{
    public class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance;
        
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
        public void ReceiveAudioClipClientRpc(string clipName, byte[] audioData)
        {
            // Check if the key (clipName) already exists in the dictionary
            if (!networkedAudioClips.ContainsKey(clipName))
            {
                // If it doesn't exist, create a new AudioClip and add it to the dictionary
                AudioClip newAudioClip = WavUtility.ByteArrayToAudioClip(audioData, clipName);
                networkedAudioClips.Add(clipName, newAudioClip);
                ClientNetworkedAudioChanged?.Invoke();
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

        [ServerRpc]
        public void SendAudioClipServerRpc(string clipName, byte[] audioData)
        {
            ReceiveAudioClipClientRpc(clipName, audioData);
            HostNetworkedAudioChanged?.Invoke();
        }

        [ServerRpc]
        public void RemoveAudioClipServerRpc(string clipName)
        {
            RemoveAudioClipClientRpc(clipName);
            HostNetworkedAudioChanged?.Invoke();
        }
    }
}
