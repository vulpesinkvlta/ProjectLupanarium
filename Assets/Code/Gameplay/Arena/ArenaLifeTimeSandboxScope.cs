using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class ArenaLifeTimeSandboxScope : LifetimeScope
    {
        [Header("Meta configuration")]
        [SerializeField] private RunConfig _runConfig;
        [SerializeField] private WaveCatalog _waveCatalog;
        [SerializeField] private UpgradeCatalog _upgradeCatalog;

        protected override void Configure(
            IContainerBuilder builder)
        {
            RegisterConfiguration(builder);
            RegisterSceneComponents(builder);
            RegisterArenaData(builder);
            RegisterSpatialServices(builder);
            RegisterCombatData(builder);
            RegisterHud(builder);
            RegisterSimulationSystems(builder);
            RegisterUnitPresentation(builder);
            RegisterMeta(builder);
            RegisterApplicationServices(builder);
            RegisterEntryPoints(builder);
            RegisterDeveloperTools(builder);
        }

        /// <summary>
        /// Ассеты с балансом. Лежат полями на самом скоупе, чтобы их
        /// можно было перетащить в инспекторе и не заводить ради трёх
        /// ссылок отдельный MonoBehaviour.
        /// </summary>
        private void RegisterConfiguration(
            IContainerBuilder builder)
        {
            builder.RegisterInstance(_runConfig);
            builder.RegisterInstance(_waveCatalog);
            builder.RegisterInstance(_upgradeCatalog);
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
                    BattleHealthHudView>();

            builder
                .RegisterComponentInHierarchy<RunHudView>();

            builder
                .RegisterComponentInHierarchy<RewardScreenView>();
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

        /// <summary>
        /// Мета-слой забега. В Фазе 4 переедет в корневой скоуп,
        /// чтобы пережить переход в лупанарий.
        /// </summary>
        private static void RegisterMeta(
            IContainerBuilder builder)
        {
            builder.Register<RunState>(
                Lifetime.Scoped);

            builder.Register<UpgradeDrafter>(
                Lifetime.Scoped);

            builder.Register<IUnitModifierSource, RunModifierSource>(
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_runConfig == null)
                Debug.LogWarning("Scope: не назначен RunConfig.", this);

            if (_waveCatalog == null)
                Debug.LogWarning("Scope: не назначен WaveCatalog.", this);

            if (_upgradeCatalog == null)
                Debug.LogWarning("Scope: не назначен UpgradeCatalog.", this);
        }
#endif
    }
}
