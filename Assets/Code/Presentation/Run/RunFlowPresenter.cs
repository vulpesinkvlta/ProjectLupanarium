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
        private const string NoFormationName = "Без строя";

        private const string NoFormationDescription =
            "Отряд идёт врассыпную и сходится с врагом сразу, " +
            "без бонусов и без ожидания.";

        private static readonly UnitClassId[] TrackedClasses =
            BuildTrackedClasses();

        private readonly BattleFlowController _flowController;
        private readonly RunState _runState;
        private readonly LupanariumState _lupanarium;
        private readonly FormationCatalog _formationCatalog;
        private readonly BattleStatistics _statistics;
        private readonly UnitClassHudCatalog _classCatalog;
        private readonly UnitDefinitionResolver _definitionResolver;

        private readonly RunHudView _hudView;
        private readonly RewardScreenView _rewardView;
        private readonly ContractSelectionView _contractView;
        private readonly BattleSummaryView _summaryView;
        private readonly SquadSelectionView _squadView;
        private readonly FormationSelectionView _formationView;
        private readonly BattleFeedbackView _feedbackView;

        private readonly List<string> _enemyTexts = new(4);
        private readonly List<BattleSummaryRowData> _summaryRows = new(8);
        private readonly List<SquadOptionData> _squadOptions = new(8);
        private readonly List<FormationOptionData> _formationOptions = new(8);
        private readonly StringBuilder _builder = new(256);

        public RunFlowPresenter(
            BattleFlowController flowController,
            RunState runState,
            BattleStatistics statistics,
            UnitClassHudCatalog classCatalog,
            UnitDefinitionResolver definitionResolver,
            RunHudView hudView,
            RewardScreenView rewardView,
            ContractSelectionView contractView,
            BattleSummaryView summaryView,
            SquadSelectionView squadView,
            FormationSelectionView formationView,
            BattleFeedbackView feedbackView,
            LupanariumState lupanarium,
            FormationCatalog formationCatalog)
        {
            _flowController = flowController ??
                throw new ArgumentNullException(nameof(flowController));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));

            _definitionResolver = definitionResolver ??
                throw new ArgumentNullException(nameof(definitionResolver));

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

            _formationView = formationView ??
                throw new ArgumentNullException(nameof(formationView));

            _feedbackView = feedbackView ??
                throw new ArgumentNullException(nameof(feedbackView));

            _classCatalog = classCatalog;
            _lupanarium = lupanarium ?? throw new ArgumentNullException(nameof(lupanarium));
            _formationCatalog = formationCatalog ?? throw new ArgumentNullException(nameof(formationCatalog));
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
            _formationView.OptionSelected += OnFormationOptionSelected;
            _formationView.PurchaseRequested += _flowController.PurchaseFormationOption;
            _formationView.ReturnToBaseRequested += OnReturnRequested;
            _hudView.FormationSelectionRequested += _flowController.ReopenFormationSelection;

            _flowController.StateChanged += OnStateChanged;
            _flowController.RewardOffered += OnRewardOffered;
            _flowController.ContractsOffered += OnContractsOffered;
            _flowController.SquadOptionsOffered += OnSquadOptionsOffered;
            _flowController.FormationsOffered += OnFormationsOffered;

            // BattleFlowController.Start() мог отработать раньше нашего
            // и выставить состояние до того, как мы подписались.
            OnStateChanged(_flowController.State);

            if (_flowController.State == BattleFlowState.ContractSelection)
                OnContractsOffered(_flowController.ContractOffers);
            else if (_flowController.State == BattleFlowState.SquadSelection)
                OnSquadOptionsOffered(_flowController.SquadOptions);
            else if (_flowController.State == BattleFlowState.FormationSelection)
                OnFormationsOffered(_flowController.FormationOptions);
            else if (_flowController.State == BattleFlowState.Reward)
                OnRewardOffered(_flowController.CurrentChoices);
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
            _formationView.OptionSelected -= OnFormationOptionSelected;
            _formationView.PurchaseRequested -= _flowController.PurchaseFormationOption;
            _formationView.ReturnToBaseRequested -= OnReturnRequested;
            _hudView.FormationSelectionRequested -= _flowController.ReopenFormationSelection;

            _flowController.StateChanged -= OnStateChanged;
            _flowController.RewardOffered -= OnRewardOffered;
            _flowController.ContractsOffered -= OnContractsOffered;
            _flowController.SquadOptionsOffered -= OnSquadOptionsOffered;
            _flowController.FormationsOffered -= OnFormationsOffered;
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

        private void OnFormationOptionSelected(int index) =>
            _flowController.SelectFormationOption(index);

        private void OnFormationCycleRequested(int direction) =>
            _flowController.CycleFormation(direction);

        private void OnSquadOptionsOffered(IReadOnlyList<RosterEntry> options)
        {
            _squadOptions.Clear();

            for (var i = 0; i < options.Count; i++)
            {
                RosterEntry entry = options[i];
                UnitConfig unit = entry.Unit;

                UnitClassId classId = unit != null
                    ? unit.ClassId
                    : UnitClassId.None;

                // Статы берём из того же резолвера, что и бой, а не из
                // конфига напрямую: игрок должен видеть числа с бонусами
                // школы и снаряжения, иначе карточка врала бы после
                // первой же покупки в арсенале.
                UnitStats stats = unit != null
                    ? _definitionResolver.Resolve(unit, TeamId.Player).Stats
                    : default;

                _squadOptions.Add(
                    new SquadOptionData(
                        GetClassName(classId),
                        GetClassDescription(classId),
                        GetClassIcon(classId),
                        entry.StartingCount,
                        stats.MaxHealth,
                        stats.AttackDamage,
                        stats.MoveSpeed));
            }

            _squadView.Show(_squadOptions);
        }

        private void OnFormationsOffered(
            IReadOnlyList<FormationConfig> options)
        {
            _formationOptions.Clear();

            for (var i = 0; i < options.Count; i++)
            {
                FormationConfig formation = options[i];
                FormationEntry entry = _formationCatalog.FindByFormation(formation);
                bool owned = formation == null || (entry != null && _lupanarium.IsFormationUnlocked(entry));
                bool meetsRound = entry == null || _lupanarium.MeetsRoundRequirement(entry);
                bool canBuy = entry != null && _lupanarium.CanUnlockFormation(entry);
                string status = formation == null ? "Всегда доступно · бесплатно"
                    : owned ? "Куплено навсегда"
                    : !meetsRound ? $"Достигните раунда {entry.RequiredBestRound} · {entry.Price} ден."
                    : canBuy ? $"Доступно для покупки · {entry.Price} ден."
                    : $"Нужно {entry.Price} ден. · не хватает {entry.Price - _lupanarium.Denarii}";
                string action = owned ? "Выбрать" : !meetsRound ? "Закрыто" : $"Купить · {entry.Price} ден.";

                _formationOptions.Add(
                    new FormationOptionData(
                        GetFormationName(formation),
                        GetFormationDescription(formation),
                        formation != null ? formation.Icon : null,
                        formation == _runState.SelectedFormation,
                        owned, canBuy, status, action));
            }

            _formationView.SetBalance(_lupanarium.Denarii, _lupanarium.BestRoundReached);
            _formationView.Show(_formationOptions);
        }

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

            if (state != BattleFlowState.FormationSelection)
                _formationView.Hide();

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

        private static string GetFormationName(FormationConfig formation)
        {
            // null — это законный вариант «без строя», а не отсутствие
            // данных: в начале игры не открыт ни один строй.
            return formation != null
                ? formation.DisplayName
                : NoFormationName;
        }

        private static string GetFormationDescription(FormationConfig formation)
        {
            return formation != null
                ? formation.Description
                : NoFormationDescription;
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

        private string GetClassDescription(UnitClassId classId)
        {
            if (_classCatalog != null &&
                _classCatalog.TryGet(classId, out UnitClassHudCatalog.Entry entry))
            {
                return entry.Description;
            }

            return string.Empty;
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

            // Раньше ярлык строя не обновлялся ни разу: SetFormation
            // не вызывался вообще, и в HUD висел текст из инспектора.
            _hudView.SetFormation(
                GetFormationName(_runState.SelectedFormation));
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
