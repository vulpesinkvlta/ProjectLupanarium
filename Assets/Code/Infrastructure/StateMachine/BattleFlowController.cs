using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Цикл забега: контракт — строй — подготовка — бой — итоги —
    /// улучшение — снова контракт.
    ///
    /// Состояний восемь, переходов около десятка, и всё ещё влезает
    /// в один enum со switch. Паттерн State с классами тут дал бы
    /// восемь файлов и ни одной новой возможности.
    /// </summary>
    public sealed class BattleFlowController : IStartable, ILateTickable, IDisposable
    {
        private readonly VictorySystem _victorySystem;
        private readonly BattleController _battleController;
        private readonly RunState _runState;
        private readonly UpgradeDrafter _upgradeDrafter;
        private readonly ContractDrafter _contractDrafter;
        private readonly RunConfig _runConfig;
        private readonly FormationCatalog _formationCatalog;
        private readonly LupanariumState _lupanarium;
        private readonly IGameSceneLoader _sceneLoader;
        private readonly BattleStatistics _statistics;
        private readonly RosterCatalog _rosterCatalog;
        private BattleResult _pendingResult;

        private readonly List<UpgradeConfig> _currentChoices = new(8);
        private readonly List<ContractOffer> _contractOffers = new(4);
        private readonly List<RosterEntry> _squadOptions = new(8);

        // null в списке означает «без строя»: это всегда доступный
        // вариант, и хранить под него отдельный флаг незачем.
        private readonly List<FormationConfig> _formationOptions = new(8);

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

        /// <summary>Выданы строи на выбор. null в списке — бой без строя.</summary>
        public event Action<IReadOnlyList<FormationConfig>> FormationsOffered;

        public IReadOnlyList<UpgradeConfig> CurrentChoices => _currentChoices;
        public IReadOnlyList<ContractOffer> ContractOffers => _contractOffers;
        public IReadOnlyList<RosterEntry> SquadOptions => _squadOptions;
        public IReadOnlyList<FormationConfig> FormationOptions => _formationOptions;

        public BattleFlowController(
            VictorySystem victorySystem,
            BattleController battleController,
            RunState runState,
            UpgradeDrafter upgradeDrafter,
            ContractDrafter contractDrafter,
            RunConfig runConfig,
            FormationCatalog formationCatalog,
            LupanariumState lupanarium,
            IGameSceneLoader sceneLoader,
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

            _battleController.ClearArena();

            ResumeOrBeginRun();
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

            _sceneLoader.TryLoad(GameScene.Base);
        }

        /// <summary>Новый забег с нуля, не покидая арену.</summary>
        public void StartRun()
        {
            // Кнопка перезапуска не может стереть незавершённый забег.
            if (_runState.IsActive)
                return;

            _runState.Reset();
            _battleController.ClearArena();
            _currentChoices.Clear();
            _contractOffers.Clear();
            _pendingResult = BattleResult.None;

            BeginRun();
        }

        private void ResumeOrBeginRun()
        {
            if (!_runState.IsActive)
            {
                StartRun();
                return;
            }

            _currentChoices.AddRange(_runState.RewardChoices);
            _contractOffers.AddRange(_runState.ContractOffers);

            switch (_runState.Phase)
            {
                case BattleFlowState.SquadSelection:
                    BeginRun();
                    break;
                case BattleFlowState.ContractSelection:
                    if (CountAvailableOffers() == 0)
                        OfferContracts();
                    else
                    {
                        SetState(BattleFlowState.ContractSelection);
                        ContractsOffered?.Invoke(_contractOffers);
                    }
                    break;
                case BattleFlowState.FormationSelection:
                    OfferFormations();
                    break;
                case BattleFlowState.Preparation:
                case BattleFlowState.Fighting:
                    BuildFormationOptions();
                    SetState(BattleFlowState.Preparation);
                    break;
                case BattleFlowState.BattleSummary:
                case BattleFlowState.Reward:
                    // Бой уже оплачен. Статистика принадлежала старой
                    // сцене, поэтому продолжаем с его невыбранной награды.
                    OfferUpgrades();
                    break;
            }
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

            OfferFormations();
        }

        /// <summary>
        /// Предлагает строй на этот бой.
        ///
        /// Строй выбирается каждый раунд, а не один раз за забег:
        /// контракт уже известен, и решение «встать в черепаху против
        /// лучников или разбежаться» — это и есть тактика, ради которой
        /// строи вообще существуют.
        /// </summary>
        private void OfferFormations()
        {
            BuildFormationOptions();

            // Один вариант — это только «без строя»: выбирать не из чего,
            // экран показывать не за чем.
            if (_formationOptions.Count <= 1)
            {
                _runState.SelectFormation(
                    _formationOptions.Count == 1
                        ? _formationOptions[0]
                        : null);

                SetState(BattleFlowState.Preparation);
                return;
            }

            SetState(BattleFlowState.FormationSelection);
            FormationsOffered?.Invoke(_formationOptions);
        }

        private void BuildFormationOptions()
        {
            _formationOptions.Clear();

            // Бой без строя доступен всегда и идёт первым вариантом.
            // Это не заглушка: свободный отряд сходится с врагом сразу,
            // а строй наступает медленно — иногда это выгоднее.
            _formationOptions.Add(null);

            IReadOnlyList<FormationEntry> entries = _formationCatalog.Entries;

            if (entries.Count == 0)
            {
                // Каталог не настроен — оставляем старое поведение
                // на строе из RunConfig, чтобы проект запускался.
                if (_runConfig.DefaultFormation != null)
                    _formationOptions.Add(_runConfig.DefaultFormation);

                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                FormationEntry entry = entries[i];

                if (entry == null || entry.Formation == null)
                    continue;

                if (!_lupanarium.IsFormationUnlocked(entry))
                    continue;

                _formationOptions.Add(entry.Formation);
            }
        }

        /// <summary>Игрок выбрал строй на этот бой.</summary>
        public void SelectFormationOption(int optionIndex)
        {
            if (State != BattleFlowState.FormationSelection)
                return;

            if (optionIndex < 0 || optionIndex >= _formationOptions.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(optionIndex),
                    optionIndex,
                    "Нет строя с таким индексом.");
            }

            _runState.SelectFormation(_formationOptions[optionIndex]);

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
        /// Листает строй стрелками HUD. Разрешено только в подготовке:
        /// менять строй посреди боя нельзя — статы юнитов уже посчитаны
        /// и запечены.
        ///
        /// Листает по тому же списку, что и экран выбора, а не по всему
        /// каталогу: иначе стрелки давали бы бесплатный доступ к строям,
        /// за которые игрок ещё не заплатил.
        /// </summary>
        public void CycleFormation(int direction)
        {
            if (State != BattleFlowState.Preparation)
                return;

            if (_formationOptions.Count <= 1)
                return;

            int currentIndex =
                _formationOptions.IndexOf(_runState.SelectedFormation);

            if (currentIndex < 0)
                currentIndex = 0;

            int step = direction >= 0 ? 1 : -1;

            int nextIndex =
                (currentIndex + step + _formationOptions.Count) %
                _formationOptions.Count;

            _runState.SelectFormation(_formationOptions[nextIndex]);
            SaveProgress();

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

            _pendingResult = result;
        }

        public void LateTick()
        {
            if (_pendingResult == BattleResult.None)
                return;

            // VictorySystem вызывается до UnitCleanupSystem. Ждём конца
            // тика, чтобы в награду и сейв вошли последние убийства.
            BattleResult result = _pendingResult;
            _pendingResult = BattleResult.None;

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
            _runState.EndRun();
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
            _upgradeDrafter.Draw(_runConfig.RewardChoiceCount, _currentChoices);

            SetState(BattleFlowState.BattleSummary);
        }

        private void OfferUpgrades()
        {
            int desired = _runConfig.RewardChoiceCount;

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
            int earned = _runState.TakeAllGold();
            _lupanarium.AddDenarii(earned);
            _lupanarium.RegisterRoundReached(_runState.WaveNumber);
            SaveProgress();

            Debug.Log(
                $"[BattleFlow] Забег принёс {earned} денариев, " +
                $"в школе стало {_lupanarium.Denarii}.");
        }

        private void SetState(BattleFlowState state)
        {
            if (State == state)
                return;

            State = state;
            SaveProgress();
            StateChanged?.Invoke(state);
        }

        private void SaveProgress()
        {
            // EndRun выставляет Defeat до банковского перевода, пока UI
            // ещё в Fighting. Не возвращаем проигранному забегу активность.
            BattleFlowState phase = _runState.Phase == BattleFlowState.Defeat
                ? BattleFlowState.Defeat : State;
            _runState.SetProgress(phase, _currentChoices, _contractOffers);
        }
    }
}
