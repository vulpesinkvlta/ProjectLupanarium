using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class ArenaLifeTimeSandboxScope : LifetimeScope
    {
        protected override void Configure(
            IContainerBuilder builder)
        {
            RegisterSceneComponents(builder);
            RegisterArenaData(builder);
            RegisterSpatialServices(builder);
            RegisterCombatData(builder);
            RegisterSimulationSystems(builder);
            RegisterUnitPresentation(builder);
            RegisterApplicationServices(builder);
            RegisterEntryPoints(builder);
        }

        private static void RegisterCombatData(
            IContainerBuilder builder)
        {
            builder.Register<DamageBuffer>(
                Lifetime.Scoped);

            builder.Register<UnitDeathBuffer>(
                Lifetime.Scoped);

            builder.Register<TargetEngagementRegistry>(
                Lifetime.Scoped);
        }

        private static void RegisterSpatialServices(
            IContainerBuilder builder)
        {
            builder.Register<SpatialGrid>(
                Lifetime.Scoped);
        }

        private static void RegisterSimulationSystems(
            IContainerBuilder builder)
        {
            builder.Register<TargetingSystem>(
                Lifetime.Scoped);

            builder.Register<MovementSystem>(
                Lifetime.Scoped);

            builder.Register<SeparationSystem>(
                Lifetime.Scoped);

            builder.Register<AttackSystem>(
                Lifetime.Scoped);

            builder.Register<DamageSystem>(
                Lifetime.Scoped);

            builder.Register<DeathSystem>(
                Lifetime.Scoped);

            builder.Register<VictorySystem>(
                Lifetime.Scoped);
        }

        private static void RegisterSceneComponents(
            IContainerBuilder builder)
        {
            builder
                .RegisterComponentInHierarchy<
                    ArenaSceneReference>();

            builder
                .RegisterComponentInHierarchy<
                    ArenaSandboxRoster>();

            builder
                .RegisterComponentInHierarchy<
                    ArenaDebugPanel>();
        }

        private static void RegisterArenaData(
            IContainerBuilder builder)
        {
            builder.Register<ArenaContext>(
                Lifetime.Scoped);

            builder.Register<ArenaSimulation>(
                Lifetime.Scoped);
        }

        private static void RegisterUnitPresentation(
            IContainerBuilder builder)
        {
            builder.Register<UnitViewRegistry>(
                Lifetime.Scoped);

            builder.Register<UnitViewPool>(
                Lifetime.Scoped);

            builder.Register<UnitViewSynchronizer>(
                Lifetime.Scoped);
        }

        private static void RegisterApplicationServices(
            IContainerBuilder builder)
        {
            builder.Register<UnitSpawner>(
                Lifetime.Scoped);

            builder.Register<ArenaSandboxController>(
                Lifetime.Scoped);
        }

        private static void RegisterEntryPoints(
            IContainerBuilder builder)
        {
            builder
                .RegisterEntryPoint<ArenaSimulationClock>()
                .AsSelf();

            builder
                .RegisterEntryPoint<ArenaSandBoxPresenter>();
        }
    }
}
