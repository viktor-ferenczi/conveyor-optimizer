using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ConveyorOptimizer.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.ModAPI.Ingame;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.Game;

namespace ConveyorOptimizer.Patches
{
    [PatchShim]
    // ReSharper disable once UnusedType.Global
    public static class MyGridConveyorSystemPatch
    {
        private static Logger Logger => ConveyorOptimizerPlugin.Logger;
        private static ConveyorOptimizerConfig Config => ConveyorOptimizerConfig.Instance;

        private static readonly Random Rng = new Random();

        private static readonly Backoff Backoff = new Backoff();

        private static readonly PullItemStats Stats = new PullItemStats();

        // Cleanup period to remove unused trackers
        private const int UpdatePeriod = 60 * 60; // frames

        // Log statistics once this many simulation frames (1/60 seconds)
        private const int PrintStatsPeriod = 10 * 60 * 60; // frames

        // Time as the number of simulation frames passed, reset to zero on reaching 2 billion (after 385 days)
        private static int time;

        // Destination inventory volume fill factor stored before invoking PullItems for change detection
        private static readonly ThreadLocal<float> VolumeFillFactor = new ThreadLocal<float>(() => -1f);

        #region Patching

        // Original methods

        private static readonly MethodInfo PullItemTargetInfo =
            typeof(MyGridConveyorSystem).GetMethod("PullItem", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find original method");

        private static readonly MethodInfo PullItemsTargetInfo =
            typeof(MyGridConveyorSystem).GetMethod("PullItems", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find original method");

        // Patch methods

        private static readonly MethodInfo PullItemPrefixInfo =
            typeof(MyGridConveyorSystemPatch).GetMethod(nameof(PullItemPrefix), BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        private static readonly MethodInfo PullItemSuffixInfo =
            typeof(MyGridConveyorSystemPatch).GetMethod(nameof(PullItemSuffix), BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        private static readonly MethodInfo PullItemsPrefixInfo =
            typeof(MyGridConveyorSystemPatch).GetMethod(nameof(PullItemsPrefix), BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        private static readonly MethodInfo PullItemsSuffixInfo =
            typeof(MyGridConveyorSystemPatch).GetMethod(nameof(PullItemsSuffix), BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        // Apply patch

        // ReSharper disable once UnusedMember.Global
        public static void Patch(PatchContext ctx)
        {
            ReflectedManager.Process(typeof(MyGridConveyorSystemPatch));

            ctx.GetPattern(PullItemTargetInfo).Prefixes.Add(PullItemPrefixInfo);
            ctx.GetPattern(PullItemTargetInfo).Suffixes.Add(PullItemSuffixInfo);

            ctx.GetPattern(PullItemsTargetInfo).Prefixes.Add(PullItemsPrefixInfo);
            ctx.GetPattern(PullItemsTargetInfo).Suffixes.Add(PullItemsSuffixInfo);
        }

        #endregion

        #region Mute logic

        public static void Reset()
        {
            Stats.Clear();
            Backoff.Clear();
            time = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update()
        {
            if (!Config.Enabled)
                return;

            time++;
            Stats.FrameCount++;

            if (time % UpdatePeriod == 0)
                Backoff.Cleanup(time - UpdatePeriod - Config.MaxMute);

            if (time % PrintStatsPeriod == 0)
            {
                PrintStats(Logger.Info);
                Stats.Clear();
            }

            if (time >= 2000000000)
                Reset();
        }

        public static void PrintStats(Action<string> print)
        {
            Stats.PrintStats(print);
            Backoff.PrintStats(time, print);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Tracker GetPullItemTracker(IMyConveyorEndpointBlock start, MyDefinitionId itemId)
        {
            var entityId = (start as IMyFunctionalBlock)?.EntityId ?? 0;
            var itemHash = itemId.GetHashCode();
            var key = entityId ^ itemHash;
            return Backoff.GetOrCreateTracker(key, time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Tracker GetPullItemsTracker(IMyConveyorEndpointBlock start, MyInventoryConstraint inventoryConstraint)
        {
            var entityId = (start as IMyFunctionalBlock)?.EntityId ?? 0;
            var constraintHash = inventoryConstraint.Description.GetHashCode();
            var key = entityId ^ constraintHash;
            return Backoff.GetOrCreateTracker(key, time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FinishCall(Tracker tracker, bool emptyHanded)
        {
            if (emptyHanded)
                tracker.Backoff(time, Config.MinMute, Config.MaxMute - (Rng.Next() & 1));
            else
                tracker.Reset(time);
        }

        #endregion

        #region Patches

        private static bool PullItemPrefix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Local
            MyGridConveyorSystem __instance,
            MyDefinitionId itemId,
            IMyConveyorEndpointBlock start,
            // ReSharper disable once InconsistentNaming
            ref MyFixedPoint __result)
        {
            if (!Config.Enabled)
                return true;

            Stats.PullItemCount++;

            var tracker = GetPullItemTracker(start, itemId);

            if (!tracker.IsCanceling(time))
                return true;

            __result = MyFixedPoint.Zero;
            Stats.PullItemMuted++;
            return false;
        }

        private static void PullItemSuffix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Local
            MyGridConveyorSystem __instance,
            MyDefinitionId itemId,
            IMyConveyorEndpointBlock start,
            // ReSharper disable once InconsistentNaming
            ref MyFixedPoint __result
        )
        {
            if (!Config.Enabled)
                return;

            var tracker = GetPullItemTracker(start, itemId);

            var emptyHanded = __result == MyFixedPoint.Zero;

            FinishCall(tracker, emptyHanded);
        }

        private static bool PullItemsPrefix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Local
            MyGridConveyorSystem __instance,
            MyInventoryConstraint inventoryConstraint,
            IMyConveyorEndpointBlock start,
            MyInventory destinationInventory,
            // ReSharper disable once InconsistentNaming
            ref MyFixedPoint __result)
        {
            if (!Config.Enabled)
                return true;

            Stats.PullItemsCount++;

            var tracker = GetPullItemsTracker(start, inventoryConstraint);

            if (!tracker.IsCanceling(time))
            {
                VolumeFillFactor.Value = destinationInventory.VolumeFillFactor;
                return true;
            }

            __result = MyFixedPoint.Zero;
            Stats.PullItemsMuted++;
            return false;
        }

        private static void PullItemsSuffix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Local
            MyGridConveyorSystem __instance,
            MyInventoryConstraint inventoryConstraint,
            IMyConveyorEndpointBlock start,
            MyInventory destinationInventory)
        {
            if (!Config.Enabled)
                return;

            var tracker = GetPullItemsTracker(start, inventoryConstraint);

            var volumeFillFactor = VolumeFillFactor.Value;
            VolumeFillFactor.Value = float.NaN;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var emptyHanded = destinationInventory.VolumeFillFactor == volumeFillFactor;

            FinishCall(tracker, emptyHanded);
        }

        #endregion
    }
}