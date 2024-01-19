using System.Collections.Generic;
using UnityEngine;

namespace LCSoundTool
{
    public class RandomAudioClip
    {
        public AudioClip clip;
        [Range(0, 1)]
        public float chance = 1f; // Default chance is 100%
        public string tag;

        public RandomAudioClip(AudioClip clip, float chance, string tag)
        {
            this.clip = clip;
            this.chance = chance;
            this.tag = tag;
        }
    }
}
