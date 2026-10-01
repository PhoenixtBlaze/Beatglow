using System;
using System.Collections;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using UnityEngine;
using Zenject;

namespace BeatGlow.UI
{
    internal sealed class BeatGlowMenuButtonHost : IInitializable, IDisposable
    {
        private readonly DiContainer _container;

        private MenuButton _menuButton;
        private BeatGlowFlowCoordinator _flowCoordinator;
        private Coroutine _registerRoutine;

        public BeatGlowMenuButtonHost(DiContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            Plugin.Log?.Info("BeatGlow menu button registration scheduled");

            if (_menuButton == null)
                _menuButton = new MenuButton("BeatGlow", "Choose a logo or a gamertag", ShowFlow);

            Graphics.LogoMaterialFactory.Warm();
            _registerRoutine = BeatGlowCoroutineHost.Run(RegisterMenuButtonWhenReady());
        }

        public void Dispose()
        {
            if (_registerRoutine != null)
            {
                BeatGlowCoroutineHost.Stop(_registerRoutine);
                _registerRoutine = null;
            }

            try
            {
                if (_menuButton != null && MenuButtons.Instance != null)
                    MenuButtons.Instance.UnregisterButton(_menuButton);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn("BeatGlow could not unregister its menu button: " + ex.Message);
            }
        }

        private IEnumerator RegisterMenuButtonWhenReady()
        {
            const int maxRetries = 600;
            int retries = 0;

            while (retries++ < maxRetries)
            {
                if (MenuButtons.Instance != null)
                {
                    MenuButtons.Instance.RegisterButton(_menuButton);
                    Plugin.Log?.Info("BeatGlow menu button registered");
                    _registerRoutine = null;
                    yield break;
                }

                yield return null;
            }

            Plugin.Log?.Warn("BeatGlow timed out waiting for MenuButtons. The Mods button was not registered.");
            _registerRoutine = null;
        }

        private void ShowFlow()
        {
            try
            {
                if (_flowCoordinator == null)
                {
                    _flowCoordinator = BeatSaberUI.CreateFlowCoordinator<BeatGlowFlowCoordinator>();
                    _container.Inject(_flowCoordinator);
                }

                if (BeatSaberUI.MainFlowCoordinator == null)
                {
                    Plugin.Log?.Warn("BeatGlow: MainFlowCoordinator is not available.");
                    return;
                }

                BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(_flowCoordinator);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("BeatGlow could not open its menu: " + ex);
            }
        }
    }
}
