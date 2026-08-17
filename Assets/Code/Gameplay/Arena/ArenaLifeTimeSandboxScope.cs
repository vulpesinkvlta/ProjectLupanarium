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
            RegisterHud(builder);
            RegisterSimulationSystems(builder);
            RegisterUnitPresentation(builder);
            RegisterApplicationServices(builder);
            RegisterEntryPoints(builder);
            RegisterDeveloperTools(builder);
        }

        private static void RegisterHud(
            IContainerBuilder builder)
        {
            builder.Register<BattleHudDirtyTracker>(
                Lifetime.Scoped);

            builder.RegisterEntryPoint<
                BattleHealthHudPresenter>();
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

            builder.Register<UnitCleanupSystem>(
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
                    BattleHealthHudView>();
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

            builder.Register<DeadViewQueue>(
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
        }

        /// <summary>
        /// Дебаг-панель песочницы и сбор счётчиков. В релизной сборке
        /// не регистрируются: панель остаётся в сцене, но её никто
        /// не обновляет и не слушает.
        /// </summary>
        private static void RegisterDeveloperTools(
            IContainerBuilder builder)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder
                .RegisterComponentInHierarchy<ArenaDebugPanel>();

            builder.Register<SimulationDiagnostics>(
                Lifetime.Scoped);

            builder
                .RegisterEntryPoint<ArenaDebugPresenter>();
#endif
        }
    }
}
