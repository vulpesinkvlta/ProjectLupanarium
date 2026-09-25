using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Корневой скоуп приложения.
    ///
    /// Живёт префабом, назначенным в VContainerSettings, поэтому создаётся
    /// автоматически при первом обращении из любой сцены. Благодаря этому
    /// вход в Play прямо из сцены арены продолжает работать — сцену
    /// Bootstrap проходить необязательно.
    ///
    /// Здесь всё, что обязано пережить смену сцены: состояние школы,
    /// состояние забега и ассеты с балансом.
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Run")]
        [SerializeField] private RunConfig _runConfig;
        [SerializeField] private ContractCatalog _contractCatalog;
        [SerializeField] private UpgradeCatalog _upgradeCatalog;

        [Header("Formations")]
        [SerializeField] private FormationCatalog _formationCatalog;

        [Header("Lupanarium")]
        [SerializeField] private SchoolCatalog _schoolCatalog;
        [SerializeField] private ItemCatalog _itemCatalog;

        [Header("Roster")]
        [SerializeField] private RosterCatalog _rosterCatalog;

        [Tooltip("Названия и иконки классов. Нужен и в бою, и в школе, " +
                 "поэтому живёт в корне, а не на HUD арены.")]
        [SerializeField] private UnitClassHudCatalog _classCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterCatalogs(builder);
            RegisterPersistentState(builder);
            RegisterServices(builder);
        }

        private void RegisterCatalogs(IContainerBuilder builder)
        {
            builder.RegisterInstance(_runConfig);
            builder.RegisterInstance(_contractCatalog);
            builder.RegisterInstance(_upgradeCatalog);
            builder.RegisterInstance(_formationCatalog);
            builder.RegisterInstance(_schoolCatalog);
            builder.RegisterInstance(_itemCatalog);
            builder.RegisterInstance(_rosterCatalog);
            builder.RegisterInstance(_classCatalog);
        }

        private static void RegisterPersistentState(
            IContainerBuilder builder)
        {
            builder.Register<LupanariumState>(
                Lifetime.Singleton);

            builder.Register<RunState>(
                Lifetime.Singleton);
        }

        private static void RegisterServices(
            IContainerBuilder builder)
        {
            builder.Register<GameSceneLoader>(
                Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

            builder.Register<LupanariumController>(
                Lifetime.Singleton);

            builder.Register<ISaveStorage, PlayerPrefsSaveStorage>(
                Lifetime.Singleton);

            builder.Register<SaveService>(
                Lifetime.Singleton);

            builder.Register<RunSaveCodec>(Lifetime.Singleton);

            // Загружает прогресс на старте и пишет его при изменениях.
            builder.RegisterEntryPoint<SaveRunner>();

            // Частота кадров и поведение экрана под конкретной платформой.
            builder.RegisterEntryPoint<PlatformBootstrap>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnIfMissing(_runConfig, nameof(RunConfig));
            WarnIfMissing(_contractCatalog, nameof(ContractCatalog));
            WarnIfMissing(_upgradeCatalog, nameof(UpgradeCatalog));
            WarnIfMissing(_formationCatalog, nameof(FormationCatalog));
            WarnIfMissing(_schoolCatalog, nameof(SchoolCatalog));
            WarnIfMissing(_itemCatalog, nameof(ItemCatalog));
            WarnIfMissing(_rosterCatalog, nameof(RosterCatalog));
            WarnIfMissing(_classCatalog, nameof(UnitClassHudCatalog));
        }

        private void WarnIfMissing(Object asset, string label)
        {
            if (asset == null)
            {
                Debug.LogWarning(
                    $"ProjectLifetimeScope: не назначен {label}.",
                    this);
            }
        }
#endif
    }
}
