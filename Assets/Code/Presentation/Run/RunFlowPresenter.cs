using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает машину состояний забега с интерфейсом.
    ///
    /// Вьюхи знают только про кнопки и текст, BattleFlowController —
    /// только про состояния и RunState. Всё, что между ними, живёт здесь.
    /// </summary>
    public sealed class RunFlowPresenter : IStartable, IDisposable
    {
        private readonly BattleFlowController _flowController;
        private readonly RunState _runState;
        private readonly RunHudView _hudView;
        private readonly RewardScreenView _rewardView;

        public RunFlowPresenter(
            BattleFlowController flowController,
            RunState runState,
            RunHudView hudView,
            RewardScreenView rewardView)
        {
            _flowController = flowController ??
                throw new ArgumentNullException(nameof(flowController));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _hudView = hudView ??
                throw new ArgumentNullException(nameof(hudView));

            _rewardView = rewardView ??
                throw new ArgumentNullException(nameof(rewardView));
        }

        public void Start()
        {
            _hudView.FightRequested += OnFightRequested;
            _hudView.RestartRequested += OnRestartRequested;

            _rewardView.UpgradeSelected += OnUpgradeSelected;

            _flowController.StateChanged += OnStateChanged;
            _flowController.RewardOffered += OnRewardOffered;

            // BattleFlowController.Start() мог отработать раньше нашего
            // и выставить Preparation до того, как мы подписались.
            OnStateChanged(_flowController.State);
        }

        public void Dispose()
        {
            _hudView.FightRequested -= OnFightRequested;
            _hudView.RestartRequested -= OnRestartRequested;

            _rewardView.UpgradeSelected -= OnUpgradeSelected;

            _flowController.StateChanged -= OnStateChanged;
            _flowController.RewardOffered -= OnRewardOffered;
        }

        private void OnFightRequested()
        {
            _flowController.StartWave();
        }

        private void OnRestartRequested()
        {
            _flowController.StartRun();
        }

        private void OnUpgradeSelected(int index)
        {
            _flowController.SelectUpgrade(index);
        }

        private void OnRewardOffered(
            IReadOnlyList<UpgradeConfig> choices)
        {
            _rewardView.Show(choices);
        }

        private void OnStateChanged(BattleFlowState state)
        {
            RefreshRunInfo();

            _hudView.SetState(state);

            if (state != BattleFlowState.Reward)
                _rewardView.Hide();

            if (state == BattleFlowState.Defeat)
                _hudView.SetDefeatText(_runState.WaveNumber);
        }

        private void RefreshRunInfo()
        {
            _hudView.SetRunInfo(
                _runState.WaveNumber,
                _runState.Gold,
                _runState.TotalUnitCount);
        }
    }
}
