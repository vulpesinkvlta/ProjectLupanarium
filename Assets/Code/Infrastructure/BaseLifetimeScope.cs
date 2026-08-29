using VContainer;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Скоуп сцены лупанария.
    ///
    /// Родитель — ProjectLifetimeScope, поэтому LupanariumState,
    /// SchoolCatalog и LupanariumController приходят оттуда и переживают
    /// уход на арену и обратно.
    /// </summary>
    public sealed class BaseLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder
                .RegisterComponentInHierarchy<LupanariumView>();

            builder
                .RegisterEntryPoint<LupanariumPresenter>();
        }
    }
}
