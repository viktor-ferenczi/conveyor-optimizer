using System;
using System.Runtime.CompilerServices;

namespace ConveyorOptimizer.Utils
{
    public class Tracker
    {
        public int LastTime { get; private set; }
        public int Duration { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCanceling(int time) => time < LastTime + Duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tracker(int time)
        {
            LastTime = time;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(int time)
        {
            LastTime = time;
            Duration = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Backoff(int time, int minDuration, int maxDuration)
        {
            if (IsCanceling(time))
                return;

            LastTime = time;
            Duration = Math.Max(minDuration, Math.Min(maxDuration, (Duration << 1) | 1));
        }
    }
}