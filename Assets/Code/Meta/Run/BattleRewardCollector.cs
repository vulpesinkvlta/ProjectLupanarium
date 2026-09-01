using System;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Начисляет золото за убитых врагов.
    ///
    /// Подписывается на UnitCleanupSystem.UnitDied — то самое событие,
    /// ради которого в Фазе 0 буфер смертей отвязывали от слоя рендера.
    /// Симуляция про экономику по-прежнему не знает.
    /// </summary>
    public sealed class BattleRewardCollector : IStartable, IDisposable
    {
        private readonly UnitCleanupSystem _cleanupSystem;
        private readonly RunState _runState;
        private readonly BattleStatistics _statistics;

        public int KillGoldThisBattle { get; private set; }

        public BattleRewardCollector(
            UnitCleanupSystem cleanupSystem,
            RunState runState,
            BattleStatistics statistics)
        {
            _cleanupSystem = cleanupSystem ??
                throw new ArgumentNullException(nameof(cleanupSystem));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));
        }

        public void Start()
        {
            _cleanupSystem.UnitDied += OnUnitDied;
        }

        public void Dispose()
        {
            _cleanupSystem.UnitDied -= OnUnitDied;
        }

        private void OnUnitDied(UnitRuntime unit)
        {
            // Платят только за врагов. За своих павших гладиаторов
            // золото начислять было бы странно.
            if (unit.Team != TeamId.Enemy)
                return;

            if (unit.GoldReward <= 0)
                return;

            _runState.AddGold(unit.GoldReward);
            _statistics.AddGold(unit.GoldReward);

            KillGoldThisBattle += unit.GoldReward;
        }
    }
}
