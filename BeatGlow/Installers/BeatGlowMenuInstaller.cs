using BeatGlow.UI;
using Zenject;

namespace BeatGlow.Installers
{
    internal sealed class BeatGlowMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<BeatGlowMenuButtonHost>()
                .AsSingle()
                .NonLazy();
        }
    }
}
