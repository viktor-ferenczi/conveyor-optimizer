using ConveyorOptimizer.Patches;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ConveyorOptimizer
{
    [Category("conveyor")]
    public class ConveyorOptimizerCommands : CommandModule
    {
        private void Respond(string message)
        {
            Context?.Respond(message);
        }

        private void RespondWithInfo()
        {
            var config = ConveyorOptimizerConfig.Instance;
            Respond(config.Enabled ? $"Conveyor Optimizer plugin is enabled" : $"Conveyor Optimizer plugin is disabled");
            Respond($"MinMute = {config.MinMute}");
            Respond($"MaxMute = {config.MaxMute}");
        }

        [Command("info", "Prints the current settings")]
        [Permission(MyPromoteLevel.None)]
        // ReSharper disable once UnusedMember.Global
        public void Info()
        {
            RespondWithInfo();
        }

        [Command("stat", "Prints statistics")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void Stat()
        {
            MyGridConveyorSystemPatch.PrintStats(Respond);
        }

        [Command("on", "Enables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void On()
        {
            var config = ConveyorOptimizerConfig.Instance;
            config.Enabled = true;
            config.Save();

            MyGridConveyorSystemPatch.Reset();

            RespondWithInfo();
        }

        [Command("off", "Disables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void Off()
        {
            var config = ConveyorOptimizerConfig.Instance;
            config.Enabled = false;
            config.Save();

            MyGridConveyorSystemPatch.Reset();

            RespondWithInfo();
        }

        [Command("minmute", "Sets the minimum time conveyor system pull requests are muted after they first end up empty handed in simulation frames (1/60 seconds) [1..30000]")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void MinMute(int minMute)
        {
            if (minMute < 1 || minMute > 30000)
            {
                Respond("Minimum mute time must be between 1 and 30000 simulation frames (1/60 seconds)");
                return;
            }

            var config = ConveyorOptimizerConfig.Instance;
            config.MinMute = minMute;
            config.Save();

            MyGridConveyorSystemPatch.Reset();

            RespondWithInfo();
        }

        [Command("maxmute", "Sets the maximum time conveyor system pull requests can be muted in simulation frames (1/60 seconds) [1..30000]")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void MaxMute(int maxMute)
        {
            if (maxMute < 2 || maxMute > 30000)
            {
                Respond("Maximum mute time must be between 2 and 30000 simulation frames (1/60 seconds)");
                return;
            }

            var config = ConveyorOptimizerConfig.Instance;
            config.MaxMute = maxMute;
            config.Save();

            MyGridConveyorSystemPatch.Reset();

            RespondWithInfo();
        }
    }
}