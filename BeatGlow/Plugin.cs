using System;
using BeatGlow.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Logging;
using SiraUtil.Zenject;

namespace BeatGlow
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public sealed class Plugin
    {
        internal static Logger Log { get; private set; }

        [Init]
        public Plugin(Logger logger, Config config, Zenjector zenjector)
        {
            Log = logger;
            PluginConfig.Instance = config.Generated<PluginConfig>();

            zenjector.UseLogger(logger);
            zenjector.Install<BeatGlowMenuInstaller>(Location.Menu);
            Log.Info("BeatGlow menu installer registered");
        }

        [OnStart]
        public void OnStart()
        {
            try
            {
                BeatGlowCoroutineHost.Ensure();
                BS_Utils.Utilities.BSEvents.gameSceneLoaded += Gameplay.LogoPlacementController.OnGameSceneLoaded;
                BS_Utils.Utilities.BSEvents.menuSceneLoaded += Gameplay.LogoPlacementController.OnMenuSceneLoaded;
                string saved = PluginConfig.Instance != null ? PluginConfig.Instance.ActiveImage : "";
                Log.Info("BeatGlow started. Saved logo: " + (string.IsNullOrEmpty(saved) ? "(none)" : saved));
            }
            catch (Exception ex)
            {
                Log.Error("BeatGlow failed to start: " + ex);
            }
        }

        [OnExit]
        public void OnExit()
        {
            BS_Utils.Utilities.BSEvents.gameSceneLoaded -= Gameplay.LogoPlacementController.OnGameSceneLoaded;
            BS_Utils.Utilities.BSEvents.menuSceneLoaded -= Gameplay.LogoPlacementController.OnMenuSceneLoaded;
        }
    }
}
