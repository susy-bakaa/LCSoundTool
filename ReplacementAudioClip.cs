using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LCSoundTool.Utilities.Extensions;

namespace LCSoundTool
{
    public class ReplacementAudioClip
    {
        public List<RandomAudioClip> clips;
        public string source = string.Empty;
        public bool canPlay = true;

        private bool initialized = false;

        public ReplacementAudioClip(AudioClip clip, float chance, string source, string tag)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance, tag);

            if (!clips.ContainsThisRandomAudioClip(rClip)) // && this.source != source
            {
                clips.Add(rClip);
                this.source = source;
            }
            else if (SoundTool.infoDebugging)
            {
                SoundTool.Instance.logger.LogDebug($"RandomAudioClip {rClip.clip.GetName()} ({Mathf.RoundToInt(chance * 100f)}%) already exists withing this AudioClipContainer!");
                SoundTool.Instance.logger.LogDebug($"- This AudioClipContainer contains the following clip(s):");
                for (int i = 0; i < clips.Count; i++)
                {
                    SoundTool.Instance.logger.LogDebug($"-- Clip {i + 1} - {clips[i].clip.GetName()} with a chance of {Mathf.RoundToInt(clips[i].chance * 100f)}% tagged as '{clips[i].tag}'");
                }
            }
        }

        public ReplacementAudioClip(string source)
        {
            Initialize();

            this.source = source;
        }

        public ReplacementAudioClip()
        {
            Initialize();

            source = string.Empty;
        }

        public void AddClip(AudioClip clip, float chance, string tag)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance, tag);

            if (!clips.ContainsThisRandomAudioClip(rClip))
            {
                clips.Add(rClip);
            }
            else if (SoundTool.infoDebugging)
            {
                SoundTool.Instance.logger.LogDebug($"RandomAudioClip {rClip.clip.GetName()} ({Mathf.RoundToInt(chance * 100f)}%) already exists withing this AudioClipContainer!");
                SoundTool.Instance.logger.LogDebug($"- This AudioClipContainer contains the following clip(s):");
                for (int i = 0; i < clips.Count; i++)
                {
                    SoundTool.Instance.logger.LogDebug($"-- Clip {i + 1} - {clips[i].clip.GetName()} with a chance of {Mathf.RoundToInt(clips[i].chance * 100f)}% tagged as '{clips[i].tag}'");
                }
            }
        }

        public bool Full()
        {
            if (!initialized)
                Initialize();

            float chance = 0f;

            for (int i = 0; i < clips.Count; i++)
            {
                chance += clips[i].chance;
            }

            return chance >= 1f;
        }

        public bool ContainsClip(AudioClip clip, float chance, string tag)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance, tag);

            return clips.ContainsThisRandomAudioClip(rClip);
        }

        private void Initialize()
        {
            clips = new List<RandomAudioClip>();
            initialized = true;
        }
    }
}