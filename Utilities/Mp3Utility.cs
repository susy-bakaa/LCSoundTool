using UnityEngine;
using System;
using UnityEngine.Networking;

namespace LCSoundTool.Utilities
{
    // Currently this class is identical to WavUtility besides the last method and surprisingly it seems to function fine?
    // In theory this byte conversion stuff should be only compatible with WAV audio or specifically PCM type audio.
    // Maybe Unity internally stores MDCT audio loaded with UWR as PCM and thats why it works or something. No idea. >O<
    // But it does work. I decided tp leave it in it's own seperate duplicate class however.
    // Mostly in case I WOULD need to give it it's own functionality later or maybe it doesn't work in all cases or something similar.
    public static class Mp3Utility
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
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
            {
                uwr.SendWebRequest();

                // we have to wrap tasks in try/catch, otherwise it will just fail silently
                try
                {
                    while (!uwr.isDone)
                    {

                    }

                    if (uwr.result != UnityWebRequest.Result.Success)
                        SoundTool.Instance.logger.LogError($"Failed to load MP3 AudioClip from path: {path} Full error: {uwr.error}");
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
