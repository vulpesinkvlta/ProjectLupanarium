using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Дебаг-панель: счётчики симуляции и быстрые команды забега.
    ///
    /// Кнопки панели переехали на управление циклом:
    /// любая кнопка спавна запускает текущую волну, Clear перезапускает забег.
    /// Инструмент разработчика, в релизной сборке не регистрируется.
    /// </summary>
    public sealed class ArenaDebugPresenter : IStartable, ITickable, IDisposable
    {
        private const float StatisticsRefreshInterval = 0.25f;

        private readonly BattleFlowController _flowController;
        private readonly SimulationDiagnostics _diagnostics;
        private readonly ArenaDebugPanel _panel;

        private float _remainingRefreshTime;

        public ArenaDebugPresenter(
            BattleFlowController flowController,
            SimulationDiagnostics diagnostics,
            ArenaDebugPanel panel)
        {
            _flowController = flowController ??
                throw new ArgumentNullException(nameof(flowController));

            _diagnostics = diagnostics ??
                throw new ArgumentNullException(nameof(diagnostics));

            _panel = panel ??
                throw new ArgumentNullException(nameof(panel));
        }

        public void Start()
        {
            _panel.SpawnRequested += OnSpawnRequested;
            _panel.ClearRequested += OnClearRequested;

            _flowController.StateChanged += OnStateChanged;

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

            _flowController.StateChanged -= OnStateChanged;
        }

        private void OnSpawnRequested(int _)
        {
            _flowController.StartWave();
        }

        private void OnClearRequested()
        {
            _flowController.StartRun();
        }

        private static void OnStateChanged(BattleFlowState state)
        {
            Debug.Log($"[BattleFlow] {state}");
        }

        private void RefreshStatistics()
        {
            _panel.SetStatistics(
                _diagnostics.BuildReport());
        }
    }
}
