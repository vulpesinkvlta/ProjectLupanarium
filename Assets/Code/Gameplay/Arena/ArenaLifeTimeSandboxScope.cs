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
            RegisterFormations(builder);
            RegisterMeta(builder);
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

            builder.Register<BattleFeedbackQueue>(
                Lifetime.Scoped);

            builder.Register<BattleRandom>(
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

            builder.Register<AbilitySystem>(
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

            builder.Register<FormationSystem>(
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
                    BattleHealthHudView>();

            builder
                .RegisterComponentInHierarchy<RunHudView>();

            builder
                .RegisterComponentInHierarchy<RewardScreenView>();

            builder
                .RegisterComponentInHierarchy<BattleFeedbackView>();
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

        private static void RegisterFormations(
            IContainerBuilder builder)
        {
            builder.Register<FormationRegistry>(
                Lifetime.Scoped);

            builder.Register<FormationSolver>(
                Lifetime.Scoped);
        }

        /// <summary>
        /// Мета-слой боя. RunState и каталоги приходят из корневого
        /// скоупа — здесь только то, что живёт ровно один бой.
        /// </summary>
        private static void RegisterMeta(
            IContainerBuilder builder)
        {
            builder.Register<UpgradeDrafter>(
                Lifetime.Scoped);

            // Источники модификаторов регистрируются конкретными типами,
            // а наружу отдаётся composite: резолвер статов знает только
            // про интерфейс и не в курсе, сколько слагаемых внутри.
            builder.Register<RunModifierSource>(
                Lifetime.Scoped);

            builder.Register<FormationModifierSource>(
                Lifetime.Scoped);

            builder.Register<LupanariumModifierSource>(
                Lifetime.Scoped);

            builder.Register<EquipmentModifierSource>(
                Lifetime.Scoped);

            builder.Register<IUnitModifierSource, CompositeUnitModifierSource>(
                Lifetime.Scoped);
        }

        private static void RegisterApplicationServices(
            IContainerBuilder builder)
        {
            builder.Register<UnitStatsBuilder>(
                Lifetime.Scoped);

            builder.Register<UnitDefinitionResolver>(
                Lifetime.Scoped);

            builder.Register<UnitSpawner>(
                Lifetime.Scoped);

            builder.Register<BattleController>(
                Lifetime.Scoped);
        }

        private static void RegisterEntryPoints(
            IContainerBuilder builder)
        {
            builder
                .RegisterEntryPoint<ArenaSimulationClock>()
                .AsSelf();

            builder
                .RegisterEntryPoint<BattleFlowController>()
                .AsSelf();

            builder
                .RegisterEntryPoint<RunFlowPresenter>();

            // Начисляет золото за убитых врагов.
            builder
                .RegisterEntryPoint<BattleRewardCollector>();

            // Вычитывает очередь эффектов раз в кадр.
            builder
                .RegisterEntryPoint<BattleFeedbackPresenter>();
        }

        /// <summary>
        /// Дебаг-панель песочницы и сбор счётчиков.
        /// В релизной сборке не регистрируются.
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
