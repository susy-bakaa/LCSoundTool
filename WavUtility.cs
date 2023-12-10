using UnityEngine;
using System;
using UnityEngine.Networking;

namespace LCSoundTool.Utilities
{
    public static class WavUtility
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

        public static AudioClip ByteArrayToAudioClip(byte[] byteArray, string clipName)
        {
            int bitsPerSample = 16;
            int bytesPerSample = bitsPerSample / 8;

            AudioClip audioClip = AudioClip.Create(clipName, byteArray.Length / bytesPerSample, 1, 44100, false);

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

        public static AudioClip LoadFromDiskToAudioClip(string path)
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
                        SoundTool.Instance.logger.LogError($"Failed to load AudioClip from path: {path} Full error: {uwr.error}");
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
