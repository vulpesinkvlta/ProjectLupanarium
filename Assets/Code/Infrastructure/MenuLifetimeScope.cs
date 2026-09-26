using VContainer;
using VContainer.Unity;
using Code.Gameplay;

namespace Code
{
    public class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>();
            builder.Register<MainMenuController>(Lifetime.Scoped);
            builder.RegisterEntryPoint<MainMenuPresenter>();
        }
    }
}
