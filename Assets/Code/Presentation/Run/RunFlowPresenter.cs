using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает машину состояний забега с интерфейсом.
    ///
    /// Вьюхи знают только про кнопки и текст, BattleFlowController —
    /// только про состояния и RunState. Всё, что между ними, живёт здесь:
    /// какой экран показать и какими числами его заполнить.
    /// </summary>
    public sealed class RunFlowPresenter : IStartable, IDisposable
    {
        private static readonly UnitClassId[] TrackedClasses =
            BuildTrackedClasses();

        private readonly BattleFlowController _flowController;
        private readonly RunState _runState;
        private readonly BattleStatistics _statistics;
        private readonly UnitClassHudCatalog _classCatalog;

        private readonly RunHudView _hudView;
        private readonly RewardScreenView _rewardView;
        private readonly ContractSelectionView _contractView;
        private readonly BattleSummaryView _summaryView;
        private readonly SquadSelectionView _squadView;
        private readonly BattleFeedbackView _feedbackView;

        private readonly List<string> _enemyTexts = new(4);
        private readonly List<BattleSummaryRowData> _summaryRows = new(8);
        private readonly List<SquadOptionData> _squadOptions = new(8);
        private readonly StringBuilder _builder = new(256);

        public RunFlowPresenter(
            BattleFlowController flowController,
            RunState runState,
            BattleStatistics statistics,
            UnitClassHudCatalog classCatalog,
            RunHudView hudView,
            RewardScreenView rewardView,
            ContractSelectionView contractView,
            BattleSummaryView summaryView,
            SquadSelectionView squadView,
            BattleFeedbackView feedbackView)
        {
            _flowController = flowController ??
                throw new ArgumentNullException(nameof(flowController));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));

            _hudView = hudView ??
                throw new ArgumentNullException(nameof(hudView));

            _rewardView = rewardView ??
                throw new ArgumentNullException(nameof(rewardView));

            _contractView = contractView ??
                throw new ArgumentNullException(nameof(contractView));

            _summaryView = summaryView ??
                throw new ArgumentNullException(nameof(summaryView));

            _squadView = squadView ??
                throw new ArgumentNullException(nameof(squadView));

            _feedbackView = feedbackView ??
                throw new ArgumentNullException(nameof(feedbackView));

            _classCatalog = classCatalog;
        }

        public void Start()
        {
            _hudView.FightRequested += OnFightRequested;
            _hudView.RestartRequested += OnRestartRequested;
            _hudView.FormationCycleRequested += OnFormationCycleRequested;
            _hudView.ReturnToLupanariumRequested += OnReturnRequested;

            _rewardView.UpgradeSelected += OnUpgradeSelected;
            _contractView.ContractSelected += OnContractSelected;
            _summaryView.ClaimRequested += OnClaimRequested;
            _squadView.OptionSelected += OnSquadOptionSelected;

            _flowController.StateChanged += OnStateChanged;
            _flowController.RewardOffered += OnRewardOffered;
            _flowController.ContractsOffered += OnContractsOffered;
            _flowController.SquadOptionsOffered += OnSquadOptionsOffered;

            // BattleFlowController.Start() мог отработать раньше нашего
            // и выставить состояние до того, как мы подписались.
            OnStateChanged(_flowController.State);

            if (_flowController.State == BattleFlowState.ContractSelection)
                OnContractsOffered(_flowController.ContractOffers);
            else if (_flowController.State == BattleFlowState.SquadSelection)
                OnSquadOptionsOffered(_flowController.SquadOptions);
        }

        public void Dispose()
        {
            _hudView.FightRequested -= OnFightRequested;
            _hudView.RestartRequested -= OnRestartRequested;
            _hudView.FormationCycleRequested -= OnFormationCycleRequested;
            _hudView.ReturnToLupanariumRequested -= OnReturnRequested;

            _rewardView.UpgradeSelected -= OnUpgradeSelected;
            _contractView.ContractSelected -= OnContractSelected;
            _summaryView.ClaimRequested -= OnClaimRequested;
            _squadView.OptionSelected -= OnSquadOptionSelected;

            _flowController.StateChanged -= OnStateChanged;
            _flowController.RewardOffered -= OnRewardOffered;
            _flowController.ContractsOffered -= OnContractsOffered;
            _flowController.SquadOptionsOffered -= OnSquadOptionsOffered;
        }

        private void OnFightRequested() => _flowController.StartWave();
        private void OnRestartRequested() => _flowController.StartRun();
        private void OnReturnRequested() => _flowController.ReturnToLupanarium();
        private void OnClaimRequested() => _flowController.ClaimReward();

        private void OnUpgradeSelected(int index) =>
            _flowController.SelectUpgrade(index);

        private void OnContractSelected(int index) =>
            _flowController.SelectContract(index);

        private void OnSquadOptionSelected(int index) =>
            _flowController.SelectStartingSquad(index);

        private void OnSquadOptionsOffered(IReadOnlyList<RosterEntry> options)
        {
            _squadOptions.Clear();

            for (var i = 0; i < options.Count; i++)
            {
                RosterEntry entry = options[i];

                UnitClassId classId = entry.Unit != null
                    ? entry.Unit.ClassId
                    : UnitClassId.None;

                _squadOptions.Add(
                    new SquadOptionData(
                        GetClassName(classId),
                        GetClassIcon(classId),
                        entry.StartingCount));
            }

            _squadView.Show(_squadOptions);
        }

        private void OnFormationCycleRequested(int direction) =>
            _flowController.CycleFormation(direction);

        private void OnRewardOffered(IReadOnlyList<UpgradeConfig> choices)
        {
            _rewardView.Show(choices);
        }

        private void OnContractsOffered(IReadOnlyList<ContractOffer> offers)
        {
            BuildEnemyTexts(offers);

            _contractView.Show(
                _runState.WaveNumber,
                offers,
                _enemyTexts);
        }

        private void OnStateChanged(BattleFlowState state)
        {
            RefreshRunInfo();

            _hudView.SetState(state);

            if (state != BattleFlowState.Reward)
                _rewardView.Hide();

            if (state != BattleFlowState.ContractSelection)
                _contractView.Hide();

            if (state != BattleFlowState.SquadSelection)
                _squadView.Hide();

            if (state == BattleFlowState.BattleSummary)
                ShowSummary();
            else
                _summaryView.Hide();

            // Победа — это вход в состояние итогов, поражение —
            // отдельное состояние. Оба звучат один раз на переходе.
            if (state == BattleFlowState.BattleSummary)
                _feedbackView.PlayOutcome(isVictory: true);
            else if (state == BattleFlowState.Defeat)
                _feedbackView.PlayOutcome(isVictory: false);

            if (state == BattleFlowState.Defeat)
                _hudView.SetDefeatText(_runState.WaveNumber);
        }

        private void ShowSummary()
        {
            BuildSummaryRows();

            _summaryView.Show(
                _runState.WaveNumber,
                _statistics.GoldEarned,
                BuildKillsText(),
                _summaryRows);
        }

        private void BuildSummaryRows()
        {
            _summaryRows.Clear();

            for (var i = 0; i < TrackedClasses.Length; i++)
            {
                UnitClassId classId = TrackedClasses[i];

                ClassBattleStats stats =
                    _statistics.GetPlayerStats(classId);

                // Классы, которых не было в отряде, в таблицу не попадают.
                if (stats.Deployed == 0)
                    continue;

                _summaryRows.Add(
                    new BattleSummaryRowData(
                        GetClassName(classId),
                        GetClassIcon(classId),
                        stats.Deployed - stats.Lost,
                        stats.Deployed,
                        stats.Kills,
                        stats.DamageDealt,
                        stats.DamagePrevented,
                        stats.DamageTaken));
            }
        }

        private string BuildKillsText()
        {
            _builder.Clear();

            for (var i = 0; i < TrackedClasses.Length; i++)
            {
                UnitClassId classId = TrackedClasses[i];
                int kills = _statistics.GetEnemyKills(classId);

                if (kills == 0)
                    continue;

                if (_builder.Length > 0)
                    _builder.AppendLine();

                _builder
                    .Append(kills)
                    .Append(' ')
                    .Append(GetClassName(classId));
            }

            return _builder.Length > 0
                ? _builder.ToString()
                : "никого";
        }

        private void BuildEnemyTexts(IReadOnlyList<ContractOffer> offers)
        {
            _enemyTexts.Clear();

            for (var i = 0; i < offers.Count; i++)
            {
                ContractOffer offer = offers[i];

                if (offer.Source == null)
                {
                    _enemyTexts.Add(string.Empty);
                    continue;
                }

                _builder.Clear();

                IReadOnlyList<SquadEntry> enemies = offer.Enemies;

                for (var e = 0; e < enemies.Count; e++)
                {
                    SquadEntry entry = enemies[e];

                    if (entry == null || entry.Config == null)
                        continue;

                    if (_builder.Length > 0)
                        _builder.AppendLine();

                    _builder
                        .Append(entry.Count)
                        .Append(' ')
                        .Append(GetClassName(entry.Config.ClassId));
                }

                _enemyTexts.Add(_builder.ToString());
            }
        }

        private string GetClassName(UnitClassId classId)
        {
            if (_classCatalog != null &&
                _classCatalog.TryGet(classId, out UnitClassHudCatalog.Entry entry))
            {
                return entry.DisplayName;
            }

            return classId.ToString();
        }

        private Sprite GetClassIcon(UnitClassId classId)
        {
            if (_classCatalog != null &&
                _classCatalog.TryGet(classId, out UnitClassHudCatalog.Entry entry))
            {
                return entry.Icon;
            }

            return null;
        }

        private void RefreshRunInfo()
        {
            _hudView.SetRunInfo(
                _runState.WaveNumber,
                _runState.Gold,
                _runState.TotalUnitCount);
        }

        private static UnitClassId[] BuildTrackedClasses()
        {
            var values = (UnitClassId[])Enum.GetValues(typeof(UnitClassId));
            var tracked = new List<UnitClassId>(values.Length);

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] != UnitClassId.None)
                    tracked.Add(values[i]);
            }

            return tracked.ToArray();
        }
    }
}
