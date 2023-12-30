using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LCSoundTool
{
    public class ReplacementAudioClip
    {
        public List<RandomAudioClip> clips;
        public string source;
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

        private void Initialize()
        {
            clips = new List<RandomAudioClip>();
            initialized = true;
        }
    }
}