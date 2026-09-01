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
        private readonly BattleFeedbackQueue _feedbackQueue;

        private readonly FormationRegistry _formationRegistry;

        private readonly FormationSystem _formationSystem;
        private readonly TargetingSystem _targetingSystem;
        private readonly MovementSystem _movementSystem;
        private readonly SeparationSystem _separationSystem;
        private readonly AbilitySystem _abilitySystem;
        private readonly AttackSystem _attackSystem;
        private readonly DamageSystem _damageSystem;
        private readonly DeathSystem _deathSystem;
        private readonly VictorySystem _victorySystem;
        private readonly UnitCleanupSystem _cleanupSystem;
        private readonly ArenaBoundsSystem _boundsSystem;

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
            BattleFeedbackQueue feedbackQueue,
            FormationRegistry formationRegistry,
            FormationSystem formationSystem,
            TargetingSystem targetingSystem,
            MovementSystem movementSystem,
            SeparationSystem separationSystem,
            AbilitySystem abilitySystem,
            AttackSystem attackSystem,
            DamageSystem damageSystem,
            DeathSystem deathSystem,
            VictorySystem victorySystem,
            UnitCleanupSystem cleanupSystem,
            ArenaBoundsSystem boundsSystem)
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

            _feedbackQueue = feedbackQueue ??
                throw new ArgumentNullException(nameof(feedbackQueue));

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

            _abilitySystem = abilitySystem ??
                throw new ArgumentNullException(nameof(abilitySystem));

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

            _boundsSystem = boundsSystem ??
                throw new ArgumentNullException(nameof(boundsSystem));
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

            // Грид строится первым: с него читает и таргетинг, и
            // расталкивание. Раньше он перестраивался между ними, и
            // таргетингу приходилось перебирать всю вражескую армию.
            //
            // Расталкивание теперь работает по позициям начала тика,
            // то есть отстаёт на один шаг движения. Это допустимо:
            // расталкивание — мягкое ограничение, оно доисправляет
            // перекрытие на следующем тике, а перестраивать грид дважды
            // ради одного шага в 0.1 юнита не стоит.
            _spatialGrid.Rebuild(
                _context.AllUnits);

            // Движет якоря строёв, на которые опирается движение.
            _formationSystem.Tick(deltaTime);

            _targetingSystem.Tick();
            _movementSystem.Tick(deltaTime);

            _separationSystem.Tick(deltaTime);

            // После всех, кто двигает юнитов: расталкивание в плотной
            // свалке толкает крайних наружу, и проверять границу
            // до него бессмысленно.
            _boundsSystem.Tick();

            // Способности до атак: оглушение, наложенное в этом тике,
            // должно сорвать замах цели уже сейчас, а не через тик.
            _abilitySystem.Tick();

            _attackSystem.Tick(deltaTime);
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
            _feedbackQueue.Clear();
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
