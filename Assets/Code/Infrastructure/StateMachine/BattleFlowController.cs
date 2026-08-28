using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Цикл забега: подготовка — бой — награда — следующая волна.
    ///
    /// Состояний четыре, переходов шесть, поэтому машина сделана на enum
    /// и switch. Паттерн State с классами окупается от восьми состояний;
    /// здесь он дал бы пять файлов вместо одного и ноль пользы.
    /// </summary>
    public sealed class BattleFlowController : IStartable, IDisposable
    {
        private const int RewardChoiceCount = 3;

        private readonly VictorySystem _victorySystem;
        private readonly BattleController _battleController;
        private readonly RunState _runState;
        private readonly UpgradeDrafter _upgradeDrafter;

        private readonly List<UpgradeConfig> _currentChoices =
            new(RewardChoiceCount);

        public BattleFlowState State { get; private set; } =
            BattleFlowState.None;

        /// <summary>Состояние сменилось. На это подписан UI.</summary>
        public event Action<BattleFlowState> StateChanged;

        /// <summary>Выданы карточки улучшений на выбор.</summary>
        public event Action<IReadOnlyList<UpgradeConfig>> RewardOffered;

        public IReadOnlyList<UpgradeConfig> CurrentChoices => _currentChoices;

        public BattleFlowController(
            VictorySystem victorySystem,
            BattleController battleController,
            RunState runState,
            UpgradeDrafter upgradeDrafter)
        {
            _victorySystem = victorySystem ??
                throw new ArgumentNullException(nameof(victorySystem));

            _battleController = battleController ??
                throw new ArgumentNullException(nameof(battleController));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _upgradeDrafter = upgradeDrafter ??
                throw new ArgumentNullException(nameof(upgradeDrafter));
        }

        public void Start()
        {
            _victorySystem.BattleCompleted += OnBattleCompleted;

            StartRun();
        }

        public void Dispose()
        {
            _victorySystem.BattleCompleted -= OnBattleCompleted;
        }

        /// <summary>Новый забег с нуля.</summary>
        public void StartRun()
        {
            _runState.Reset();
            _battleController.ClearArena();

            SetState(BattleFlowState.Preparation);
        }

        /// <summary>Игрок нажал «В бой».</summary>
        public void StartWave()
        {
            if (State != BattleFlowState.Preparation)
                return;

            _battleController.StartWave();

            SetState(BattleFlowState.Fighting);
        }

        /// <summary>Игрок выбрал карточку улучшения.</summary>
        public void SelectUpgrade(int choiceIndex)
        {
            if (State != BattleFlowState.Reward)
                return;

            if (choiceIndex < 0 || choiceIndex >= _currentChoices.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(choiceIndex),
                    choiceIndex,
                    "Нет карточки с таким индексом.");
            }

            _runState.AddUpgrade(_currentChoices[choiceIndex]);
            _currentChoices.Clear();

            _runState.AdvanceWave();

            SetState(BattleFlowState.Preparation);
        }

        /// <summary>
        /// Пропустить награду. Нужен на случай, когда каталог улучшений
        /// пуст — иначе забег застрянет в состоянии Reward навсегда.
        /// </summary>
        public void SkipReward()
        {
            if (State != BattleFlowState.Reward)
                return;

            _currentChoices.Clear();
            _runState.AdvanceWave();

            SetState(BattleFlowState.Preparation);
        }

        private void OnBattleCompleted(BattleResult result)
        {
            if (State != BattleFlowState.Fighting)
                return;

            switch (result)
            {
                case BattleResult.PlayerVictory:
                    HandleVictory();
                    break;

                // Ничья означает, что не выжил никто. Для забега это
                // такое же поражение, как и победа врага.
                case BattleResult.EnemyVictory:
                case BattleResult.Draw:
                    SetState(BattleFlowState.Defeat);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result),
                        result,
                        "Неизвестный исход боя.");
            }
        }

        private void HandleVictory()
        {
            _runState.AddGold(_battleController.CurrentWaveGoldReward);

            _upgradeDrafter.Draw(RewardChoiceCount, _currentChoices);

            if (_currentChoices.Count == 0)
            {
                Debug.LogWarning(
                    "[BattleFlow] Каталог улучшений пуст, " +
                    "награда пропущена.");

                _runState.AdvanceWave();
                SetState(BattleFlowState.Preparation);

                return;
            }

            SetState(BattleFlowState.Reward);
            RewardOffered?.Invoke(_currentChoices);
        }

        private void SetState(BattleFlowState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
