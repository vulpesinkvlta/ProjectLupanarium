using System;
using System.Collections.Generic;

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
        private const int InitialEnemyBufferCapacity = 16;

        private readonly ArenaSimulation _simulation;
        private readonly UnitSpawner _unitSpawner;
        private readonly UnitDefinitionResolver _definitionResolver;
        private readonly RunState _runState;
        private readonly WaveCatalog _waveCatalog;

        private readonly List<SquadEntry> _enemyBuffer =
            new(InitialEnemyBufferCapacity);

        /// <summary>Награда за волну, которая идёт прямо сейчас.</summary>
        public int CurrentWaveGoldReward { get; private set; }

        public BattleController(
            ArenaSimulation simulation,
            UnitSpawner unitSpawner,
            UnitDefinitionResolver definitionResolver,
            RunState runState,
            WaveCatalog waveCatalog)
        {
            _simulation = simulation ??
                throw new ArgumentNullException(nameof(simulation));

            _unitSpawner = unitSpawner ??
                throw new ArgumentNullException(nameof(unitSpawner));

            _definitionResolver = definitionResolver ??
                throw new ArgumentNullException(nameof(definitionResolver));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _waveCatalog = waveCatalog ??
                throw new ArgumentNullException(nameof(waveCatalog));
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

            WaveSetup setup =
                _waveCatalog.GetWave(
                    _runState.WaveIndex,
                    _enemyBuffer);

            CurrentWaveGoldReward = setup.GoldReward;

            _unitSpawner.SpawnSquads(
                _runState.Squad,
                _runState.SelectedFormation,
                _enemyBuffer,
                setup.Formation);

            _simulation.Start();
        }

        public void ClearArena()
        {
            _simulation.Stop();

            _unitSpawner.ClearAll();
            _simulation.Reset();
        }
    }
}
