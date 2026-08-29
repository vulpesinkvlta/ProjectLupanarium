using System;
using System.Collections.Generic;
using System.Text;

namespace Code.Gameplay
{
    /// <summary>
    /// Собирает счётчики систем симуляции в текстовый отчёт для дебаг-панели.
    ///
    /// Держит все зависимости на себе, чтобы презентеры не тащили в себя
    /// половину симуляции ради одной строки. Инструмент разработчика:
    /// в релизной сборке не регистрируется.
    /// </summary>
    public sealed class SimulationDiagnostics
    {
        private const int InitialCapacity = 1536;

        private static readonly UnitClassId[] TrackedClasses =
            BuildTrackedClasses();

        private readonly StringBuilder _builder =
            new(InitialCapacity);

        private readonly ArenaContext _context;
        private readonly ArenaSimulation _simulation;
        private readonly SpatialGrid _spatialGrid;

        private readonly TargetingSystem _targetingSystem;
        private readonly MovementSystem _movementSystem;
        private readonly SeparationSystem _separationSystem;
        private readonly AttackSystem _attackSystem;
        private readonly DamageSystem _damageSystem;
        private readonly DeathSystem _deathSystem;
        private readonly UnitCleanupSystem _cleanupSystem;
        private readonly FormationSystem _formationSystem;
        private readonly FormationRegistry _formationRegistry;
        private readonly VictorySystem _victorySystem;

        private readonly UnitViewPool _viewPool;

        public SimulationDiagnostics(
            ArenaContext context,
            ArenaSimulation simulation,
            SpatialGrid spatialGrid,
            TargetingSystem targetingSystem,
            MovementSystem movementSystem,
            SeparationSystem separationSystem,
            AttackSystem attackSystem,
            DamageSystem damageSystem,
            DeathSystem deathSystem,
            UnitCleanupSystem cleanupSystem,
            FormationSystem formationSystem,
            FormationRegistry formationRegistry,
            VictorySystem victorySystem,
            UnitViewPool viewPool)
        {
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

            _cleanupSystem = cleanupSystem ??
                throw new ArgumentNullException(nameof(cleanupSystem));

            _formationSystem = formationSystem ??
                throw new ArgumentNullException(nameof(formationSystem));

            _formationRegistry = formationRegistry ??
                throw new ArgumentNullException(nameof(formationRegistry));

            _victorySystem = victorySystem ??
                throw new ArgumentNullException(nameof(victorySystem));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));
        }

        public string BuildReport()
        {
            _builder.Clear();

            AppendBattle();
            AppendRoster();
            AppendTargeting();
            AppendMovement();
            AppendFormations();
            AppendGrid();
            AppendCombat();
            AppendViews();

            return _builder.ToString();
        }

        private void AppendBattle()
        {
            AppendHeader("Battle");

            AppendLine("Tick", _simulation.TickIndex.ToString());

            AppendLine(
                "Sim time",
                _simulation.ElapsedSimulationTime.ToString("F1"));

            AppendLine(
                "Result",
                _victorySystem.Result.ToString());
        }

        private void AppendRoster()
        {
            AppendHeader("Roster");

            AppendLine("Player", _context.PlayerUnits.Count);
            AppendLine("Enemy", _context.EnemyUnits.Count);
            AppendLine("Total", _context.UnitCount);
            AppendLine("Removed", _cleanupSystem.TotalRemovedUnits);

            for (var i = 0; i < TrackedClasses.Length; i++)
            {
                UnitClassId classId = TrackedClasses[i];

                _builder
                    .Append(classId)
                    .Append(": ")
                    .Append(CountByClass(_context.PlayerUnits, classId))
                    .Append(" vs ")
                    .Append(CountByClass(_context.EnemyUnits, classId))
                    .AppendLine();
            }
        }

        private void AppendTargeting()
        {
            AppendHeader("Targeting");

            AppendLine(
                "Distance checks",
                _targetingSystem.DistanceChecksLastTick);

            AppendLine(
                "Assigned",
                _targetingSystem.AssignedTargetsLastTick);

            AppendLine(
                "Retargeted",
                _targetingSystem.RetargetedUnitsLastTick);

            AppendLine(
                "Full skips",
                _targetingSystem.FullTargetSkipsLastTick);
        }

        private void AppendMovement()
        {
            AppendHeader("Movement");

            AppendLine(
                "Moving",
                _movementSystem.MovingUnitsLastTick);

            AppendLine(
                "In attack range",
                _movementSystem.UnitsInAttackRangeLastTick);

            AppendLine(
                "Pair checks",
                _separationSystem.PairChecksLastTick);

            AppendLine(
                "Overlapping",
                _separationSystem.OverlappingPairsLastTick);

            AppendLine(
                "Corrected",
                _separationSystem.CorrectedUnitsLastTick);
        }

        private void AppendFormations()
        {
            AppendHeader("Formations");

            AppendLine(
                "Holding formation",
                _formationSystem.UnitsHoldingFormation);

            AppendTeamFormation("Player", TeamId.Player);
            AppendTeamFormation("Enemy", TeamId.Enemy);
        }

        private void AppendTeamFormation(string label, TeamId team)
        {
            TeamFormationState state = _formationRegistry.Get(team);

            _builder.Append(label).Append(": ");

            if (!state.IsActive || state.Config == null)
            {
                _builder.AppendLine("none");
                return;
            }

            _builder
                .Append(state.Config.DisplayName)
                .Append(" @ x=")
                .Append(state.Anchor.x.ToString("F2"))
                .AppendLine();
        }

        private void AppendGrid()
        {
            AppendHeader("Spatial grid");

            AppendLine("Active cells", _spatialGrid.ActiveCellCount);
            AppendLine("Created cells", _spatialGrid.CreatedCellCount);
            AppendLine("Units", _spatialGrid.UnitCountLastBuild);

            AppendLine(
                "Max radius",
                _spatialGrid.MaximumUnitRadius.ToString("F2"));
        }

        private void AppendCombat()
        {
            AppendHeader("Combat");

            AppendLine(
                "Attack attempts",
                _attackSystem.AttackAttemptsLastTick);

            AppendLine(
                "Attacks performed",
                _attackSystem.AttacksPerformedLastTick);

            AppendLine(
                "Damage requests",
                _damageSystem.RequestsProcessedLastTick);

            AppendLine(
                "Damage applied",
                _damageSystem.DamageAppliedLastTick.ToString("F0"));

            AppendLine("Deaths", _deathSystem.DeathsLastTick);
        }

        private void AppendViews()
        {
            AppendHeader("Views");

            AppendLine("Active", _viewPool.CountActive);
            AppendLine("Inactive", _viewPool.CountInactive);
            AppendLine("Created", _viewPool.CountAll);
        }

        private void AppendHeader(string title)
        {
            if (_builder.Length > 0)
                _builder.AppendLine();

            _builder
                .Append("== ")
                .Append(title)
                .AppendLine();
        }

        private void AppendLine(string label, int value)
        {
            _builder
                .Append(label)
                .Append(": ")
                .Append(value)
                .AppendLine();
        }

        private void AppendLine(string label, string value)
        {
            _builder
                .Append(label)
                .Append(": ")
                .Append(value)
                .AppendLine();
        }

        private static int CountByClass(
            IReadOnlyList<UnitRuntime> units,
            UnitClassId classId)
        {
            var count = 0;

            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].ClassId == classId)
                    count++;
            }

            return count;
        }

        private static UnitClassId[] BuildTrackedClasses()
        {
            var values =
                (UnitClassId[])Enum.GetValues(typeof(UnitClassId));

            var tracked = new List<UnitClassId>(values.Length);

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] != UnitClassId.None)
                    tracked.Add(values[i]);
            }

            return tracked.ToArray();
        }
    }
}
