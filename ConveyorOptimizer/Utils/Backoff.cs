using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ConveyorOptimizer.Utils
{
    public class Backoff
    {
        private readonly Dictionary<long, Tracker> trackers = new Dictionary<long, Tracker>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            lock (trackers)
            {
                trackers.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tracker GetOrCreateTracker(long key, int time)
        {
            lock (trackers)
            {
                if (trackers.TryGetValue(key, out var tracker))
                    return tracker;

                var newTracker = new Tracker(time);
                trackers[key] = newTracker;
                return newTracker;
            }
        }

        public void Cleanup(int cutoff, int maxTrackersToRemove=100)
        {
            lock (trackers)
            {
                var keys = new long[maxTrackersToRemove];
                var next = 0;

                foreach (var (key, tracker) in trackers)
                {
                    if(tracker.LastTime >= cutoff)
                        continue;

                    keys[next++] = key;
                    if(next == maxTrackersToRemove)
                        break;
                }

                for (var index = 0; index < next; index++)
                    trackers.Remove(keys[index]);
            }
        }

        public void PrintStats(int time, Action<string> print)
        {
            int count;
            double averageDuration;

            lock (trackers)
            {
                count = trackers.Values.Count(t => t.IsCanceling(time));
                averageDuration = count > 0 ? trackers.Values.Where(t => t.IsCanceling(time)).Average(t => (double)t.Duration) : 0;
            }

            print($"Canceling {count} sources of conveyor pull requests for an average of {averageDuration:0.00} simulation frames");
        }
    }
}