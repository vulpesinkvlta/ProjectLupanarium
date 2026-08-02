using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class ArenaSandBoxPresenter : IStartable, ITickable, IDisposable
    {
        private const float StatisticsRefreshInterval =
          0.25f;

        private readonly ArenaSandboxController _controller;
        private readonly ArenaContext _context;
        private readonly ArenaSimulation _simulation;
        private readonly TargetingSystem _targetingSystem;
        private readonly MovementSystem _movementSystem;
        private readonly SeparationSystem _separationSystem;
        private readonly UnitViewPool _viewPool;
        private readonly ArenaDebugPanel _panel;
        private readonly SpatialGrid _spatialGrid;

        private readonly AttackSystem _attackSystem;
        private readonly DamageSystem _damageSystem;
        private readonly DeathSystem _deathSystem;
        private readonly VictorySystem _victorySystem;

        private float _remainingRefreshTime;

        public ArenaSandBoxPresenter(
             ArenaSandboxController controller,
             ArenaContext context,
             ArenaSimulation simulation,
             SpatialGrid spatialGrid,
             TargetingSystem targetingSystem,
             MovementSystem movementSystem,
             SeparationSystem separationSystem,
             AttackSystem attackSystem,
             DamageSystem damageSystem,
             DeathSystem deathSystem,
             VictorySystem victorySystem,
             UnitViewPool viewPool,
             ArenaDebugPanel panel)
        {
            _controller = controller ??
                throw new ArgumentNullException(nameof(controller));

            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _simulation = simulation ??
                throw new ArgumentNullException(nameof(simulation));

            _spatialGrid = spatialGrid ??
                throw new ArgumentNullException(nameof(spatialGrid));

            _targetingSystem = targetingSystem ??
                throw new ArgumentNullException(nameof(targetingSystem));

            _movementSystem = movementSystem ??
                throw new ArgumentNullException(nameof(movementSystem));

            _separationSystem = separationSystem ??
                throw new ArgumentNullException(nameof(separationSystem));

            _attackSystem = attackSystem ??
                throw new ArgumentNullException(nameof(attackSystem));

            _damageSystem = damageSystem ??
                throw new ArgumentNullException(nameof(damageSystem));

            _deathSystem = deathSystem ??
                throw new ArgumentNullException(nameof(deathSystem));

            _victorySystem = victorySystem ??
                throw new ArgumentNullException(nameof(victorySystem));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _panel = panel ??
                throw new ArgumentNullException(nameof(panel));
        }

        public void Start()
        {
            _panel.SpawnRequested += OnSpawnRequested;
            _panel.ClearRequested += OnClearRequested;

            RefreshStatistics();
        }

        public void Tick()
        {
            _remainingRefreshTime -=
                Time.unscaledDeltaTime;

            if (_remainingRefreshTime > 0f)
                return;

            _remainingRefreshTime =
                StatisticsRefreshInterval;

            RefreshStatistics();
        }

        public void Dispose()
        {
            _panel.SpawnRequested -= OnSpawnRequested;
            _panel.ClearRequested -= OnClearRequested;
        }

        private static int CountUnitsByClass(
            System.Collections.Generic.IReadOnlyList<
                UnitRuntime> units,
            UnitClassId classId,
            bool aliveOnly)
        {
            var count = 0;

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];

                if (unit.ClassId != classId)
                    continue;

                if (aliveOnly && !unit.IsAlive)
                    continue;

                count++;
            }

            return count;
        }

        private void OnSpawnRequested(
            int unitsPerTeam)
        {
            _controller.SpawnBattle(unitsPerTeam);
            RefreshStatistics();
        }

        private void OnClearRequested()
        {
            _controller.ClearBattle();
            RefreshStatistics();
        }

        private void RefreshStatistics()
        {

            int playerMurmillos =
                CountUnitsByClass(
                    _context.PlayerUnits,
                    UnitClassId.Murmillo,
                    aliveOnly: true);

            int playerRetiarii =
                CountUnitsByClass(
                    _context.PlayerUnits,
                    UnitClassId.Retiarius,
                    aliveOnly: true);

            int enemyMurmillos =
                CountUnitsByClass(
                    _context.EnemyUnits,
                    UnitClassId.Murmillo,
                    aliveOnly: true);

            int enemyRetiarii =
                CountUnitsByClass(
                    _context.EnemyUnits,
                    UnitClassId.Retiarius,
                    aliveOnly: true);

            string statistics =
                $"Player units: {_context.PlayerUnits.Count}\n" +
                $"Enemy units: {_context.EnemyUnits.Count}\n" +
                $"Runtime units: {_context.UnitCount}\n" +
                $"Simulation tick: {_simulation.TickIndex}\n" +
                "\n" +
                $"Target checks: " +
                $"{_targetingSystem.DistanceChecksLastTick}\n" +
                $"Targets assigned: " +
                $"{_targetingSystem.AssignedTargetsLastTick}\n" +
                $"Player Murmillos: {playerMurmillos}\n" +
                $"Player Retiarii: {playerRetiarii}\n" +
                $"Enemy Murmillos: {enemyMurmillos}\n" +
                $"Enemy Retiarii: {enemyRetiarii}\n" +
                $"Moving units: " +
                $"{_movementSystem.MovingUnitsLastTick}\n" +
                $"In attack range: " +
                $"{_movementSystem.UnitsInAttackRangeLastTick}\n" +
                "\n" +
                $"Grid active cells: " +
                $"{_spatialGrid.ActiveCellCount}\n" +
                $"Grid cells created: " +
                $"{_spatialGrid.CreatedCellCount}\n" +
                $"Grid units: " +
                $"{_spatialGrid.UnitCountLastBuild}\n" +
                $"Grid max radius: " +
                $"{_spatialGrid.MaximumUnitRadius:F2}\n" +
                "\n" +
                $"Separation checks: " +
                $"{_separationSystem.PairChecksLastTick}\n" +
                $"Overlapping pairs: " +
                $"{_separationSystem.OverlappingPairsLastTick}\n" +
                $"Corrected units: " +
                $"{_separationSystem.CorrectedUnitsLastTick}\n" +
                "\n" +
                $"Views active: {_viewPool.CountActive}\n" +
                $"Views inactive: {_viewPool.CountInactive}\n" +
                $"Views created: {_viewPool.CountAll}\n" +
                $"Battle result: {_victorySystem.Result}\n" +
                $"Alive player: {_victorySystem.AlivePlayerUnits}\n" +
                $"Alive enemy: {_victorySystem.AliveEnemyUnits}\n" +
                "\n" +
                $"Attack attempts: {_attackSystem.AttackAttemptsLastTick}\n" +
                $"Attacks performed: {_attackSystem.AttacksPerformedLastTick}\n" +
                $"Damage requests: {_damageSystem.RequestsProcessedLastTick}\n" +
                $"Damage applied: {_damageSystem.DamageAppliedLastTick:F0}\n" +
                $"Deaths: {_deathSystem.DeathsLastTick}\n" +
                $"Targets assigned: " +
                $"{_targetingSystem.AssignedTargetsLastTick}\n" +
                $"Units retargeted: " +
                $"{_targetingSystem.RetargetedUnitsLastTick}\n" +
                $"Targets assigned: " +
                $"{_targetingSystem.AssignedTargetsLastTick}\n" +
                $"Units retargeted: " +
                $"{_targetingSystem.RetargetedUnitsLastTick}\n" +
                $"Full target skips: " +
                $"{_targetingSystem.FullTargetSkipsLastTick}\n";


            _panel.SetStatistics(statistics);
        }
    }
}
