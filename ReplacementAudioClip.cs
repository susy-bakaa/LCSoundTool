using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LCSoundTool
{
    public class ReplacementAudioClip
    {
        public List<RandomAudioClip> clips;
        public string source = string.Empty;
        public bool canPlay = true;

        private bool initialized = false;

        public ReplacementAudioClip(AudioClip clip, float chance, string source)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance);

            if (!clips.Contains(rClip) && this.source != source)
            {
                clips.Add(rClip);
                this.source = source;
            }
            else
            {
                SoundTool.Instance.logger.LogDebug($"RandomAudioClip {rClip.clip.GetName()} ({chance}%) already exists withing this AudioClipContainer!");
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

        public void AddClip(AudioClip clip, float chance)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance);

            if (!clips.Contains(rClip))
            {
                clips.Add(rClip);
            }
            else
            {
                SoundTool.Instance.logger.LogDebug($"RandomAudioClip {rClip.clip.GetName()} ({chance}%) already exists withing this AudioClipContainer!");
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

        public bool ContainsClip(AudioClip clip, float chance)
        {
            if (!initialized)
                Initialize();

            RandomAudioClip rClip = new RandomAudioClip(clip, chance);

            return clips.Contains(rClip);
        }

        private void Initialize()
        {
            clips = new List<RandomAudioClip>();
            initialized = true;
        }
    }
}