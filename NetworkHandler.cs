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
        public void SyncAudioClipsClientRpc(Strings clipNames)
        {
            string[] names = clipNames.MyStrings;

            for (int i = 0; i < names.Length; i++)
            {
                if (!networkedAudioClips.ContainsKey(names[i]))
                {
                    SendExistingAudioClipServerRpc(names[i]);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendAudioClipServerRpc(string clipName, byte[] audioData)
        {
            ReceiveAudioClipClientRpc(clipName, audioData);
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
            Strings clipNames = new Strings(networkedAudioClips.Keys.ToArray());

            if (clipNames.MyStrings.Length < 1)
            {
                SoundTool.Instance.logger.LogDebug("No sounds found in networkedClips. Syncing process cancelled!");
                return;
            }

            SyncAudioClipsClientRpc(clipNames);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendExistingAudioClipServerRpc(string clipName)
        {
            if (networkedAudioClips.ContainsKey(clipName))
            {
                ReceiveAudioClipClientRpc(clipName, WavUtility.AudioClipToByteArray(networkedAudioClips[clipName], out float[] samples));
            }
            else
            {
                SoundTool.Instance.logger.LogWarning("Trying to obtain and sync a sound from the host that does not exist in the host's game!");
            }
        }
    }

    // Borrowed from https://forum.unity.com/threads/how-to-send-a-string-array-in-an-rpc.1466021/
    public struct Strings : INetworkSerializable
    {
        string[] myStrings;

        public Strings(string[] myStrings)
        {
            this.myStrings = myStrings;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();
                fastBufferWriter.WriteValueSafe(myStrings.Length);

                for (int i = 0; i < myStrings.Length; i++)
                {
                    fastBufferWriter.WriteValueSafe(myStrings[i]);
                }
            }

            if (serializer.IsReader)
            {
                FastBufferReader fastBufferReader = serializer.GetFastBufferReader();
                fastBufferReader.ReadValueSafe(out int length);

                myStrings = new string[length];

                for (int i = 0; i < length; i++)
                {
                    fastBufferReader.ReadValueSafe(out myStrings[i]);
                }
            }
        }

        public string[] MyStrings { get => myStrings; set => myStrings = value; }
    }
}