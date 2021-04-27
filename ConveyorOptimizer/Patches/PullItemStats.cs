using System;
using System.Runtime.CompilerServices;
using ConveyorOptimizer.Utils;

namespace ConveyorOptimizer.Patches
{
    public class PullItemStats
    {
        public long FrameCount;
        public long PullItemCount;
        public long PullItemMuted;
        public long PullItemsCount;
        public long PullItemsMuted;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            FrameCount = 0;
            PullItemCount = 0;
            PullItemMuted = 0;
            PullItemsCount = 0;
            PullItemsMuted = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrintStats(Action<string> print)
        {
            print($"Statistics from the latest {FrameCount} simulation frames:");

            var pullItemMutedPercentage = PullItemCount > 0 ? 100.0 * PullItemMuted / PullItemCount : 0;
            var pullItemsMutedPercentage = PullItemsCount > 0 ? 100.0 * PullItemsMuted / PullItemsCount : 0;
            print($"{pullItemMutedPercentage:0.00}% of the {PullItemCount} PullItem calls");
            print($"{pullItemsMutedPercentage:0.00}% of the {PullItemsCount} PullItems calls");
        }
    }
}