using BeatSaberMarkupLanguage;
using HMUI;

namespace BeatGlow.UI
{
    public sealed class BeatGlowFlowCoordinator : FlowCoordinator
    {
        private BeatGlowImageViewController _viewController;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("BeatGlow");
                showBackButton = true;
                _viewController = BeatSaberUI.CreateViewController<BeatGlowImageViewController>();
            }

            if (addedToHierarchy)
                ProvideInitialViewControllers(_viewController);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}
