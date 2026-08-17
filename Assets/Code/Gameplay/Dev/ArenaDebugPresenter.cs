using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает дебаг-панель песочницы с ареной: кнопки спавна/очистки
    /// и периодическое обновление счётчиков.
    ///
    /// Инструмент разработчика. Будет выброшен, когда появится настоящий
    /// цикл забега (подготовка — бой — награды — следующая волна).
    /// </summary>
    public sealed class ArenaDebugPresenter : IStartable, ITickable, IDisposable
    {
        private const float StatisticsRefreshInterval = 0.25f;

        private readonly ArenaSandboxController _controller;
        private readonly SimulationDiagnostics _diagnostics;
        private readonly VictorySystem _victorySystem;
        private readonly ArenaDebugPanel _panel;

        private float _remainingRefreshTime;

        public ArenaDebugPresenter(
            ArenaSandboxController controller,
            SimulationDiagnostics diagnostics,
            VictorySystem victorySystem,
            ArenaDebugPanel panel)
        {
            _controller = controller ??
                throw new ArgumentNullException(nameof(controller));

            _diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));

            _victorySystem = victorySystem ??
                throw new ArgumentNullException(nameof(victorySystem));

            _panel = panel ??
                throw new ArgumentNullException(nameof(panel));
        }

        public void Start()
        {
            _panel.SpawnRequested += OnSpawnRequested;
            _panel.ClearRequested += OnClearRequested;

            _victorySystem.BattleCompleted += OnBattleCompleted;

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

            _victorySystem.BattleCompleted -= OnBattleCompleted;
        }

        private void OnSpawnRequested(int unitsPerTeam)
        {
            _controller.SpawnBattle(unitsPerTeam);
            RefreshStatistics();
        }

        private void OnClearRequested()
        {
            _controller.ClearBattle();
            RefreshStatistics();
        }

        private static void OnBattleCompleted(BattleResult result)
        {
            Debug.Log($"[ArenaSandbox] Battle completed: {result}.");
        }

        private void RefreshStatistics()
        {
            _panel.SetStatistics(
                _diagnostics.BuildReport());
        }
    }
}
