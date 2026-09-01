using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Цикл забега: выбор контракта — подготовка — бой — итоги —
    /// улучшение — снова выбор контракта.
    ///
    /// Состояний шесть, переходов около десяти, и всё ещё влезает
    /// в один enum со switch. Паттерн State с классами тут дал бы
    /// шесть файлов и ни одной новой возможности.
    /// </summary>
    public sealed class BattleFlowController : IStartable, IDisposable
    {
        private readonly VictorySystem _victorySystem;
        private readonly BattleController _battleController;
        private readonly RunState _runState;
        private readonly UpgradeDrafter _upgradeDrafter;
        private readonly ContractDrafter _contractDrafter;
        private readonly RunConfig _runConfig;
        private readonly FormationCatalog _formationCatalog;
        private readonly LupanariumState _lupanarium;
        private readonly GameSceneLoader _sceneLoader;
        private readonly BattleStatistics _statistics;
        private readonly RosterCatalog _rosterCatalog;

        private readonly List<UpgradeConfig> _currentChoices = new(8);
        private readonly List<ContractOffer> _contractOffers = new(4);
        private readonly List<RosterEntry> _squadOptions = new(8);

        public BattleFlowState State { get; private set; } =
            BattleFlowState.None;

        /// <summary>Состояние сменилось. На это подписан UI.</summary>
        public event Action<BattleFlowState> StateChanged;

        /// <summary>Выданы карточки улучшений на выбор.</summary>
        public event Action<IReadOnlyList<UpgradeConfig>> RewardOffered;

        /// <summary>Выданы контракты на выбор.</summary>
        public event Action<IReadOnlyList<ContractOffer>> ContractsOffered;

        /// <summary>Выданы варианты стартового отряда.</summary>
        public event Action<IReadOnlyList<RosterEntry>> SquadOptionsOffered;

        public IReadOnlyList<UpgradeConfig> CurrentChoices => _currentChoices;
        public IReadOnlyList<ContractOffer> ContractOffers => _contractOffers;
        public IReadOnlyList<RosterEntry> SquadOptions => _squadOptions;

        public BattleFlowController(
            VictorySystem victorySystem,
            BattleController battleController,
            RunState runState,
            UpgradeDrafter upgradeDrafter,
            ContractDrafter contractDrafter,
            RunConfig runConfig,
            FormationCatalog formationCatalog,
            LupanariumState lupanarium,
            GameSceneLoader sceneLoader,
            BattleStatistics statistics,
            RosterCatalog rosterCatalog)
        {
            _victorySystem = victorySystem ??
                throw new ArgumentNullException(nameof(victorySystem));

            _battleController = battleController ??
                throw new ArgumentNullException(nameof(battleController));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _upgradeDrafter = upgradeDrafter ??
                throw new ArgumentNullException(nameof(upgradeDrafter));

            _contractDrafter = contractDrafter ??
                throw new ArgumentNullException(nameof(contractDrafter));

            _runConfig = runConfig ??
                throw new ArgumentNullException(nameof(runConfig));

            _formationCatalog = formationCatalog ??
                throw new ArgumentNullException(nameof(formationCatalog));

            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));

            _sceneLoader = sceneLoader ??
                throw new ArgumentNullException(nameof(sceneLoader));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));

            _rosterCatalog = rosterCatalog ??
                throw new ArgumentNullException(nameof(rosterCatalog));
        }

        public void Start()
        {
            _victorySystem.BattleCompleted += OnBattleCompleted;

            // Забег обнуляет LupanariumController перед уходом на арену.
            // Здесь сбрасывать нечего: RunState живёт в корневом скоупе
            // и уже содержит отряд, с которым игрок вышел из школы.
            _battleController.ClearArena();

            BeginRun();
        }

        public void Dispose()
        {
            _victorySystem.BattleCompleted -= OnBattleCompleted;
        }

        /// <summary>
        /// Возврат в школу. Заработанное золото уходит в денарии здесь же.
        /// </summary>
        public void ReturnToLupanarium()
        {
            if (State == BattleFlowState.Fighting)
            {
                Debug.LogWarning(
                    "[BattleFlow] Уход в школу посреди боя запрещён.");

                return;
            }

            BankRunGold();

            if (!_sceneLoader.TryLoad(GameScene.Base))
                StartRun();
        }

        /// <summary>Новый забег с нуля, не покидая арену.</summary>
        public void StartRun()
        {
            _runState.Reset();
            _battleController.ClearArena();

            BeginRun();
        }

        /// <summary>
        /// Забег начинается со сбора отряда. Если ростер ещё не настроен
        /// или в нём никого не открыто, берём состав из RunConfig —
        /// игра должна запускаться и без настроенного ростера.
        /// </summary>
        private void BeginRun()
        {
            BuildSquadOptions();

            if (_squadOptions.Count == 0)
            {
                Debug.LogWarning(
                    "[BattleFlow] В ростере нет открытых бойцов, " +
                    "отряд взят из RunConfig.");

                _runState.ApplyFallbackSquad();
                OfferContracts();

                return;
            }

            SetState(BattleFlowState.SquadSelection);
            SquadOptionsOffered?.Invoke(_squadOptions);
        }

        private void BuildSquadOptions()
        {
            _squadOptions.Clear();

            IReadOnlyList<RosterEntry> entries = _rosterCatalog.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                RosterEntry entry = entries[i];

                if (entry == null || entry.Unit == null)
                    continue;

                if (!_lupanarium.IsUnitUnlocked(entry))
                    continue;

                _squadOptions.Add(entry);
            }
        }

        /// <summary>Игрок выбрал, с каким отрядом выходить на арену.</summary>
        public void SelectStartingSquad(int optionIndex)
        {
            if (State != BattleFlowState.SquadSelection)
                return;

            if (optionIndex < 0 || optionIndex >= _squadOptions.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(optionIndex),
                    optionIndex,
                    "Нет варианта отряда с таким индексом.");
            }

            RosterEntry entry = _squadOptions[optionIndex];

            _runState.SetStartingSquad(entry.Unit, entry.StartingCount);

            OfferContracts();
        }

        /// <summary>Игрок выбрал контракт из предложенных.</summary>
        public void SelectContract(int offerIndex)
        {
            if (State != BattleFlowState.ContractSelection)
                return;

            if (offerIndex < 0 || offerIndex >= _contractOffers.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offerIndex),
                    offerIndex,
                    "Нет контракта с таким индексом.");
            }

            ContractOffer offer = _contractOffers[offerIndex];

            if (offer.Source == null)
                return;

            _runState.AcceptContract(offer);

            SetState(BattleFlowState.Preparation);
        }

        /// <summary>Игрок нажал «В бой».</summary>
        public void StartWave()
        {
            if (State != BattleFlowState.Preparation)
                return;

            if (!_runState.HasActiveContract)
            {
                Debug.LogWarning(
                    "[BattleFlow] Контракт не выбран, драться не с кем.");

                return;
            }

            _battleController.StartWave();

            SetState(BattleFlowState.Fighting);
        }

        /// <summary>
        /// Листает строй. Разрешено только в подготовке: менять строй
        /// посреди боя нельзя — статы юнитов уже посчитаны и запечены.
        /// </summary>
        public void CycleFormation(int direction)
        {
            if (State != BattleFlowState.Preparation)
                return;

            FormationConfig next = _formationCatalog.GetNext(
                _runState.SelectedFormation,
                direction);

            if (next == _runState.SelectedFormation)
                return;

            _runState.SelectFormation(next);

            // Состояние не поменялось, но UI должен перерисоваться.
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Игрок закрыл экран итогов боя и переходит к выбору улучшения.
        /// </summary>
        public void ClaimReward()
        {
            if (State != BattleFlowState.BattleSummary)
                return;

            OfferUpgrades();
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

            AdvanceToNextContract();
        }

        /// <summary>
        /// Пропустить награду. Нужен на случай, когда каталог улучшений
        /// пуст — иначе забег застрянет навсегда.
        /// </summary>
        public void SkipReward()
        {
            if (State != BattleFlowState.Reward)
                return;

            _currentChoices.Clear();

            AdvanceToNextContract();
        }

        private void AdvanceToNextContract()
        {
            _runState.AdvanceWave();
            _runState.ClearContract();

            OfferContracts();
        }

        private void OfferContracts()
        {
            _contractDrafter.Draw(
                _runState.WaveNumber,
                _runConfig.ContractChoiceCount,
                _contractOffers);

            int available = CountAvailableOffers();

            if (available == 0)
            {
                Debug.LogError(
                    "[BattleFlow] В каталоге нет ни одного контракта, " +
                    "доступного на раунде " +
                    $"{_runState.WaveNumber}. Забег продолжать нечем.");

                return;
            }

            SetState(BattleFlowState.ContractSelection);
            ContractsOffered?.Invoke(_contractOffers);
        }

        private int CountAvailableOffers()
        {
            var available = 0;

            for (var i = 0; i < _contractOffers.Count; i++)
            {
                if (_contractOffers[i].Source != null)
                    available++;
            }

            return available;
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
                    HandleDefeat();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result),
                        result,
                        "Неизвестный исход боя.");
            }
        }

        /// <summary>
        /// Забег окончен. Золото забега превращается в денарии школы —
        /// это единственный способ получить постоянный прогресс, поэтому
        /// проигранный забег всё равно продвигает игрока вперёд.
        /// </summary>
        private void HandleDefeat()
        {
            BankRunGold();

            SetState(BattleFlowState.Defeat);
        }

        private void HandleVictory()
        {
            // Награда за контракт начисляется до показа итогов,
            // чтобы на экране была финальная сумма.
            int reward = _runState.ActiveContract.GoldReward;

            _runState.AddGold(reward);
            _statistics.AddGold(reward);

            SetState(BattleFlowState.BattleSummary);
        }

        private void OfferUpgrades()
        {
            int desired = _runConfig.RewardChoiceCount;

            _upgradeDrafter.Draw(desired, _currentChoices);

            if (_currentChoices.Count == 0)
            {
                Debug.LogWarning(
                    "[BattleFlow] Каталог улучшений пуст, " +
                    "награда пропущена.");

                AdvanceToNextContract();
                return;
            }

            if (_currentChoices.Count < desired)
            {
                Debug.LogWarning(
                    $"[BattleFlow] Запрошено {desired} карточек, " +
                    $"а в каталоге нашлось {_currentChoices.Count}. " +
                    $"Добавьте улучшений в UpgradeCatalog.");
            }

            SetState(BattleFlowState.Reward);
            RewardOffered?.Invoke(_currentChoices);
        }

        private void BankRunGold()
        {
            // Рекорд по раундам открывает новых бойцов в школе,
            // поэтому фиксируем его на каждом выходе из забега.
            _lupanarium.RegisterRoundReached(_runState.WaveNumber);

            int earned = _runState.TakeAllGold();

            if (earned <= 0)
                return;

            _lupanarium.AddDenarii(earned);

            Debug.Log(
                $"[BattleFlow] Забег принёс {earned} денариев, " +
                $"в школе стало {_lupanarium.Denarii}.");
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
