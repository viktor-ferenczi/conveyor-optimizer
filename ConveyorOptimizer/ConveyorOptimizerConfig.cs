using System;
using System.IO;
using System.Xml.Serialization;
using NLog;
using Torch;
using Torch.Views;

namespace ConveyorOptimizer
{
    [Serializable]
    public class ConveyorOptimizerConfig : ViewModel
    {
        public const string ConfigFileName = "ConveyorOptimizer.cfg";
        public static string ConfigFilePath => Path.Combine(ConveyorOptimizerPlugin.Instance.StoragePath, ConfigFileName);
        private static XmlSerializer ConfigSerializer => new XmlSerializer(typeof(ConveyorOptimizerConfig));

        private static Logger Logger => ConveyorOptimizerPlugin.Logger;

        private static ConveyorOptimizerConfig instance;
        public static ConveyorOptimizerConfig Instance => instance ?? (instance = new ConveyorOptimizerConfig());

        private object semaphore = new object();

        // Default settings
        private bool enabled = true;
        private int minMute = 60;
        private int maxMute = 300;

        [Display(Description = "Flag to enable/disable the plugin", Name = "Enabled", Order = 1)]
        public bool Enabled
        {
            get
            {
                lock (semaphore)
                {
                    return enabled;
                }
            }
            set
            {
                lock (semaphore)
                {
                    enabled = value;
                }
                OnPropertyChanged(nameof(Enabled));
            }
        }

        [Display(Description = "Minimum time conveyor system pull requests are muted after they first end up empty handed in simulation frames (1/60 seconds) [1..30000]", Name = "MinMute", Order = 2)]
        public int MinMute
        {
            get
            {
                lock (semaphore)
                {
                    return minMute;
                }
            }
            set
            {
                lock (semaphore)
                {
                    minMute = value;
                }
                OnPropertyChanged(nameof(MinMute));
            }
        }

        [Display(Description = "Maximum time conveyor system pull requests can be muted in simulation frames (1/60 seconds) [2..30000]", Name = "MaxMute", Order = 3)]
        public int MaxMute
        {
            get
            {
                lock (semaphore)
                {
                    return maxMute;
                }
            }
            set
            {
                lock (semaphore)
                {
                    maxMute = value;
                }
                OnPropertyChanged(nameof(MaxMute));
            }
        }

        public void Save()
        {
            var path = ConfigFilePath;

            try
            {
                lock (semaphore)
                {
                    using (var streamWriter = new StreamWriter(path))
                    {
                        ConfigSerializer.Serialize(streamWriter, this);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to save configuration file: {path}");
            }
        }

        public static bool Load()
        {
            var path = ConfigFilePath;

            try
            {
                if (!File.Exists(path))
                    return false;

                using (var streamReader = new StreamReader(path))
                {
                    var config = ConfigSerializer.Deserialize(streamReader) as ConveyorOptimizerConfig;
                    if (config == null)
                    {
                        Logger.Error($"Failed to deserialize configuration file: {path}");
                        return false;
                    }

                    instance = config;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to load configuration file: {path}");
                return false;
            }

            return true;
        }
    }
}