using ConveyorOptimizer.Patches;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;

namespace ConveyorOptimizer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ConveyorOptimizerPlugin : TorchPluginBase
    {
        public const string PluginName = "ConveyorOptimizer";

        public static Logger Logger;

        public static ConveyorOptimizerPlugin Instance { get; private set; }
        private TorchSessionManager sessionManager;

        // ReSharper disable once UnusedMember.Local
        private readonly ConveyorOptimizerCommands commands = new ConveyorOptimizerCommands();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            Logger = LogManager.GetLogger(PluginName);

            sessionManager = torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += SessionStateChanged;

            Instance = this;

            if (!ConveyorOptimizerConfig.Load())
            {
                Logger.Info($"Saved initial configuration: {ConveyorOptimizerConfig.ConfigFileName}");
                ConveyorOptimizerConfig.Instance.Save();
            }
        }

        public override void Update()
        {
            base.Update();
            MyGridConveyorSystemPatch.Update();
        }

        private void SessionStateChanged(ITorchSession session, TorchSessionState newstate)
        {
            switch (newstate)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    break;
            }
        }

        public override void Dispose()
        {
            Instance = null;

            sessionManager.SessionStateChanged -= SessionStateChanged;
            sessionManager = null;

            Logger = null;

            base.Dispose();
        }
    }
}