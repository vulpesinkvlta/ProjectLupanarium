using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace Code.Gameplay
{
    /// <summary>
    /// Завершает жизненный цикл погибшего юнита: рассылает событие смерти,
    /// ставит его вью в очередь на возврат в пул и убирает юнит из контекста.
    ///
    /// Работает последней системой в тике. MovementSystem, SeparationSystem
    /// и SpatialGrid адресуют юнитов по индексу в ArenaContext.AllUnits,
    /// поэтому удалять их в середине тика нельзя — поедут индексы.
    /// </summary>
    public sealed class UnitCleanupSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Cleanup");

        private readonly ArenaContext _context;
        private readonly UnitDeathBuffer _deathBuffer;
        private readonly DeadViewQueue _deadViewQueue;
        private readonly BattleFeedbackQueue _feedback;
        private readonly BattleStatistics _statistics;

        /// <summary>
        /// Юнит погиб и уже исключён из симуляции.
        /// Точка расширения для золота, опыта, статистики и лута.
        /// </summary>
        public event Action<UnitRuntime> UnitDied;

        public int RemovedUnitsLastTick { get; private set; }
        public int TotalRemovedUnits { get; private set; }

        public UnitCleanupSystem(
            ArenaContext context,
            UnitDeathBuffer deathBuffer,
            DeadViewQueue deadViewQueue,
            BattleFeedbackQueue feedback,
            BattleStatistics statistics)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _deathBuffer = deathBuffer ??
                throw new ArgumentNullException(nameof(deathBuffer));

            _deadViewQueue = deadViewQueue ??
                throw new ArgumentNullException(
                    nameof(deadViewQueue));

            _feedback = feedback ??
                throw new ArgumentNullException(nameof(feedback));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                IReadOnlyList<UnitRuntime> deadUnits =
                    _deathBuffer.DeadUnits;

                RemovedUnitsLastTick = deadUnits.Count;

                if (RemovedUnitsLastTick == 0)
                    return;

                for (var i = 0; i < deadUnits.Count; i++)
                {
                    UnitRuntime unit = deadUnits[i];

                    // Позицию берём до удаления из контекста — после
                    // юнит уже нигде не числится, а эффекту смерти
                    // нужно знать, где её проигрывать.
                    _feedback.Push(
                        BattleFeedbackKind.Death,
                        unit.Id,
                        unit.Position,
                        0f);

                    _statistics.RegisterDeath(unit);

                    _deadViewQueue.Enqueue(unit.Id);
                    _context.RemoveUnit(unit);

                    UnitDied?.Invoke(unit);
                }

                TotalRemovedUnits += RemovedUnitsLastTick;

                _deathBuffer.Clear();
            }
        }

        public void Reset()
        {
            RemovedUnitsLastTick = 0;
            TotalRemovedUnits = 0;
        }
    }
}
