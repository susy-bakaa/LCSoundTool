using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCSoundTool.Utilities
{
    public static class Extensions
    {
        public static bool ContainsThisRandomAudioClip(this List<RandomAudioClip> list, RandomAudioClip thisClip)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].chance == thisClip.chance && list[i].clip.GetName() == thisClip.clip.GetName())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool UsedByAnimationEventScript(this AudioSource source)
        {
            PlayAudioAnimationEvent result = source.transform.root.GetComponentInChildren<PlayAudioAnimationEvent>();

            if (result != null)
            {
                return result.audioToPlay == source || result.audioToPlayB == source;
            }

            return false;
        }
    }
}
