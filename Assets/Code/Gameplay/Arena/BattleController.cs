using System;

namespace Code.Gameplay
{
    /// <summary>
    /// Механика арены: собрать бой, запустить, остановить, зачистить.
    ///
    /// Про волны, награды и состояния забега не знает — этим занимается
    /// BattleFlowController. Здесь только «поставить юнитов и включить
    /// симуляцию».
    /// </summary>
    public sealed class BattleController
    {

        private readonly ArenaSimulation _simulation;
        private readonly UnitSpawner _unitSpawner;
        private readonly UnitDefinitionResolver _definitionResolver;
        private readonly RunState _runState;
        private readonly BattleStatistics _statistics;
        private readonly ArenaContext _context;
        private readonly FormationRegistry _formationRegistry;


        /// <summary>Награда за волну, которая идёт прямо сейчас.</summary>
        public int CurrentWaveGoldReward { get; private set; }

        public BattleController(
            ArenaSimulation simulation,
            UnitSpawner unitSpawner,
            UnitDefinitionResolver definitionResolver,
            RunState runState,
            BattleStatistics statistics,
            ArenaContext context,
            FormationRegistry formationRegistry)
        {
            _simulation = simulation ??
                throw new ArgumentNullException(nameof(simulation));

            _unitSpawner = unitSpawner ??
                throw new ArgumentNullException(nameof(unitSpawner));

            _definitionResolver = definitionResolver ??
                throw new ArgumentNullException(nameof(definitionResolver));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));
            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));

            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _formationRegistry = formationRegistry ??
                throw new ArgumentNullException(nameof(formationRegistry));
        }

        public void StartWave()
        {
            _simulation.Stop();

            _unitSpawner.ClearAll();
            _simulation.Reset();

            // Между волнами игрок мог взять улучшение или сменить строй.
            // Ни то, ни другое не входит в ключ кэша дефиниций, поэтому
            // сбрасывать его обязаны мы — сам он устаревание не заметит.
            _definitionResolver.ClearCache();

            ContractOffer contract = _runState.ActiveContract;

            CurrentWaveGoldReward = contract.GoldReward;

            _statistics.Reset();

            _unitSpawner.SpawnSquads(
                _runState.Squad,
                _runState.SelectedFormation,
                contract.Enemies,
                contract.EnemyFormation);

            // Состав фиксируем после спавна: в итогах боя нужно знать,
            // сколько бойцов вышло на песок, чтобы посчитать потери.
            _statistics.CaptureDeployed(_context.PlayerUnits);

            _simulation.Start();
        }

        public void ClearArena()
        {
            _simulation.Stop();

            _unitSpawner.ClearAll();
            _simulation.Reset();

            // Строй прошлого боя гасим здесь: иначе его бонусы попадут
            // в предпросмотр статов на экране выбора отряда при рестарте
            // забега — реестр живёт весь скоуп сцены, а не один бой.
            _formationRegistry.Clear();
        }
    }
}
