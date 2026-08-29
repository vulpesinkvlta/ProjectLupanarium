using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class ArenaSimulation
    {
        private readonly ArenaContext _context;
        private readonly SpatialGrid _spatialGrid;

        private readonly DamageBuffer _damageBuffer;
        private readonly UnitDeathBuffer _deathBuffer;
        private readonly DeadViewQueue _deadViewQueue;

        private readonly FormationRegistry _formationRegistry;

        private readonly FormationSystem _formationSystem;
        private readonly TargetingSystem _targetingSystem;
        private readonly MovementSystem _movementSystem;
        private readonly SeparationSystem _separationSystem;
        private readonly AttackSystem _attackSystem;
        private readonly DamageSystem _damageSystem;
        private readonly DeathSystem _deathSystem;
        private readonly VictorySystem _victorySystem;
        private readonly UnitCleanupSystem _cleanupSystem;

        public bool IsRunning { get; private set; }

        public ulong TickIndex { get; private set; }
        public float ElapsedSimulationTime { get; private set; }

        public BattleResult Result =>
            _victorySystem.Result;

        public ArenaSimulation(
            ArenaContext context,
            SpatialGrid spatialGrid,
            DamageBuffer damageBuffer,
            UnitDeathBuffer deathBuffer,
            DeadViewQueue deadViewQueue,
            FormationRegistry formationRegistry,
            FormationSystem formationSystem,
            TargetingSystem targetingSystem,
            MovementSystem movementSystem,
            SeparationSystem separationSystem,
            AttackSystem attackSystem,
            DamageSystem damageSystem,
            DeathSystem deathSystem,
            VictorySystem victorySystem,
            UnitCleanupSystem cleanupSystem)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _spatialGrid = spatialGrid ??
                throw new ArgumentNullException(nameof(spatialGrid));

            _damageBuffer = damageBuffer ??
                throw new ArgumentNullException(nameof(damageBuffer));

            _deathBuffer = deathBuffer ??
                throw new ArgumentNullException(nameof(deathBuffer));

            _deadViewQueue = deadViewQueue ??
                throw new ArgumentNullException(nameof(deadViewQueue));

            _formationRegistry = formationRegistry ??
                throw new ArgumentNullException(nameof(formationRegistry));

            _formationSystem = formationSystem ??
                throw new ArgumentNullException(nameof(formationSystem));

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

            _cleanupSystem = cleanupSystem ??
                throw new ArgumentNullException(nameof(cleanupSystem));
        }

        public void Start()
        {
            if (_victorySystem.IsBattleCompleted)
                return;

            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
                return;

            if (deltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime));
            }

            PrepareUnitsForTick(deltaTime);

            // Первым: движет якоря строёв, на которые опирается движение.
            _formationSystem.Tick(deltaTime);

            _targetingSystem.Tick();
            _movementSystem.Tick(deltaTime);

            _spatialGrid.Rebuild(
                _context.AllUnits);

            _separationSystem.Tick(deltaTime);

            _attackSystem.Tick();
            _damageSystem.Tick();
            _deathSystem.Tick();
            _victorySystem.Tick();

            // Строго последней: убирает погибших из контекста, чем сдвигает
            // индексы, на которые опираются системы выше.
            _cleanupSystem.Tick();

            TickIndex++;
            ElapsedSimulationTime += deltaTime;

            if (_victorySystem.IsBattleCompleted)
                Stop();
        }

        public void Reset()
        {
            IsRunning = false;

            _context.Clear();
            _spatialGrid.Clear();

            _damageBuffer.Clear();
            _deathBuffer.Clear();

            // Обязательно до следующего спавна: UnitSpawner начинает
            // нумерацию юнитов заново с нуля, и оставшийся в очереди
            // старый id снял бы вью у нового юнита.
            _deadViewQueue.Clear();
            _formationRegistry.Clear();

            _targetingSystem.Reset();
            _victorySystem.Reset();
            _cleanupSystem.Reset();

            TickIndex = 0;
            ElapsedSimulationTime = 0f;
        }

        private void PrepareUnitsForTick(
            float deltaTime)
        {
            var units = _context.AllUnits;

            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                if (!unit.IsAlive)
                    continue;

                unit.BeginSimulationTick(deltaTime);
            }
        }
    }
}
