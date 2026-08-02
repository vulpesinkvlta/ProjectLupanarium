using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class VictorySystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Victory");

        private readonly ArenaContext _context;

        public BattleResult Result { get; private set; }

        public int AlivePlayerUnits { get; private set; }
        public int AliveEnemyUnits { get; private set; }

        public bool IsBattleCompleted =>
            Result != BattleResult.None;

        public VictorySystem(ArenaContext context)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        public void Tick()
        {
            if (IsBattleCompleted)
                return;

            using (TickMarker.Auto())
            {
                CountAliveUnits();

                // Пустая sandbox-сцена ещё не является боем.
                if (_context.PlayerUnits.Count == 0 &&
                    _context.EnemyUnits.Count == 0)
                {
                    return;
                }

                if (AlivePlayerUnits > 0 &&
                    AliveEnemyUnits > 0)
                {
                    return;
                }

                if (AlivePlayerUnits > 0)
                {
                    Result =
                        BattleResult.PlayerVictory;

                    return;
                }

                if (AliveEnemyUnits > 0)
                {
                    Result =
                        BattleResult.EnemyVictory;

                    return;
                }

                Result = BattleResult.Draw;
            }
        }

        public void Reset()
        {
            Result = BattleResult.None;
            AlivePlayerUnits = 0;
            AliveEnemyUnits = 0;
        }

        private void CountAliveUnits()
        {
            AlivePlayerUnits =
                CountAlive(_context.PlayerUnits);

            AliveEnemyUnits =
                CountAlive(_context.EnemyUnits);
        }

        private static int CountAlive(
            System.Collections.Generic.IReadOnlyList<
                UnitRuntime> units)
        {
            var aliveCount = 0;

            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive)
                    aliveCount++;
            }

            return aliveCount;
        }
    }
}
