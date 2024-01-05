using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
