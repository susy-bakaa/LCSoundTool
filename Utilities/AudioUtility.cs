using UnityEngine;
using System;
using UnityEngine.Networking;

namespace LCSoundTool.Utilities
{
    public static class AudioUtility
    {
        public static byte[] AudioClipToByteArray(AudioClip audioClip, out float[] samples)
        {
            samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            byte[] byteArray = new byte[samples.Length * 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(samples[i] * rescaleFactor);
                BitConverter.GetBytes(value).CopyTo(byteArray, i * 2);
            }

            return byteArray;
        }

        /// <summary>
        /// Function that let's you load WAV files directly from memory as byte arrays into Unity AudioClips.
        /// </summary>
        /// <param name="fileData">The WAV file in memory, represented as a byte array.</param>
        /// <param name="clipName">The name this AudioClip will be given once loaded and created.</param>
        /// <returns></returns>
        public static AudioClip LoadFromMemory(byte[] fileData, string clipName)
        {
            // Extract relevant information from the WAV file header
            int channels = BitConverter.ToInt16(fileData, 22);
            int frequency = BitConverter.ToInt32(fileData, 24);
            int bitsPerSample = BitConverter.ToInt16(fileData, 34);
            // Remove the WAV header to get only the audio data
            byte[] audioData = new byte[fileData.Length - 44];
            System.Array.Copy(fileData, 44, audioData, 0, audioData.Length);

            return ByteArrayToAudioClip(audioData, clipName, channels, frequency);
        }

        public static AudioClip ByteArrayToAudioClip(byte[] byteArray, string clipName, int channels, int frequency)
        {
            if (frequency < 1 || frequency > 48000)
                frequency = 44100;
            if (channels < 1 || channels > 2)
                channels = 1;

            int bitsPerSample = 16;
            int bytesPerSample = bitsPerSample / 8;

            AudioClip audioClip = AudioClip.Create(clipName, byteArray.Length / bytesPerSample, channels, frequency, false);

            audioClip.SetData(ConvertByteArrayToFloatArray(byteArray), 0);

            return audioClip;
        }

        private static float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < floatArray.Length; i++)
            {
                short value = BitConverter.ToInt16(byteArray, i * 2);
                floatArray[i] = (float)value / rescaleFactor;
            }

            return floatArray;
        }

        public static AudioClip LoadFromDiskToAudioClip(string path, AudioType type)
        {
            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, type))
            {
                uwr.SendWebRequest();

                // we have to wrap tasks in try/catch, otherwise it will just fail silently
                try
                {
                    while (!uwr.isDone)
                    {

                    }

                    if (uwr.result != UnityWebRequest.Result.Success)
                        SoundTool.Instance.logger.LogError($"Failed to load WAV AudioClip from path: {path} Full error: {uwr.error}");
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    SoundTool.Instance.logger.LogError($"{err.Message}, {err.StackTrace}");
                }
            }

            return clip;
        }
    }
}
