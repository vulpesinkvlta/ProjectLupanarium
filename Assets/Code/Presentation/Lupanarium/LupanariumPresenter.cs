using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает экран школы с её состоянием: постройки и арсенал.
    /// </summary>
    public sealed class LupanariumPresenter : IStartable, IDisposable
    {
        private readonly LupanariumState _state;
        private readonly RunState _run;
        private readonly SchoolCatalog _schoolCatalog;
        private readonly ItemCatalog _itemCatalog;
        private readonly RosterCatalog _rosterCatalog;
        private readonly FormationCatalog _formationCatalog;
        private readonly UnitClassHudCatalog _classCatalog;

        private readonly List<string> _rosterNames = new(8);
        private readonly List<Sprite> _rosterIcons = new(8);
        private readonly LupanariumController _controller;
        private readonly LupanariumView _view;

        public LupanariumPresenter(
            LupanariumState state,
            SchoolCatalog schoolCatalog,
            ItemCatalog itemCatalog,
            RosterCatalog rosterCatalog,
            FormationCatalog formationCatalog,
            UnitClassHudCatalog classCatalog,
            LupanariumController controller,
            LupanariumView view,
            RunState run)
        {
            _state = state ??
                throw new ArgumentNullException(nameof(state));
            _run = run ?? throw new ArgumentNullException(nameof(run));

            _schoolCatalog = schoolCatalog ??
                throw new ArgumentNullException(nameof(schoolCatalog));

            _itemCatalog = itemCatalog ??
                throw new ArgumentNullException(nameof(itemCatalog));

            _rosterCatalog = rosterCatalog ??
                throw new ArgumentNullException(nameof(rosterCatalog));

            _formationCatalog = formationCatalog ??
                throw new ArgumentNullException(nameof(formationCatalog));

            _classCatalog = classCatalog;

            _controller = controller ??
                throw new ArgumentNullException(nameof(controller));

            _view = view ??
                throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
            _view.ItemActionRequested += OnItemActionRequested;
            _view.RosterUnlockRequested += OnRosterUnlockRequested;
            _view.FormationUnlockRequested += OnFormationUnlockRequested;
            _view.StartRunRequested += OnStartRunRequested;

            _state.Changed += Refresh;

            _view.BuildRows(_schoolCatalog.Buildings);
            _view.BuildItemRows(_itemCatalog.Items);

            BuildRosterLabels();

            _view.BuildRosterRows(
                _rosterCatalog.Entries,
                _rosterNames,
                _rosterIcons);

            _view.BuildFormationRows(_formationCatalog.Entries);

            Refresh();
        }

        public void Dispose()
        {
            _view.UpgradeRequested -= OnUpgradeRequested;
            _view.ItemActionRequested -= OnItemActionRequested;
            _view.RosterUnlockRequested -= OnRosterUnlockRequested;
            _view.FormationUnlockRequested -= OnFormationUnlockRequested;
            _view.StartRunRequested -= OnStartRunRequested;

            _state.Changed -= Refresh;
        }

        private void OnUpgradeRequested(SchoolBuildingConfig building)
        {
            // Состояние само поднимет Changed, если покупка прошла,
            // поэтому перерисовку здесь звать не нужно.
            _controller.TryUpgrade(building);
        }

        private void OnItemActionRequested(ItemConfig item)
        {
            _controller.TryBuyOrEquip(item);
        }

        private void OnRosterUnlockRequested(RosterEntry entry)
        {
            _controller.TryUnlockUnit(entry);
        }

        private void OnFormationUnlockRequested(FormationEntry entry)
        {
            _controller.TryUnlockFormation(entry);
        }

        private void BuildRosterLabels()
        {
            _rosterNames.Clear();
            _rosterIcons.Clear();

            IReadOnlyList<RosterEntry> entries = _rosterCatalog.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                RosterEntry entry = entries[i];

                UnitClassId classId = entry != null && entry.Unit != null
                    ? entry.Unit.ClassId
                    : UnitClassId.None;

                _rosterNames.Add(GetClassName(classId));
                _rosterIcons.Add(GetClassIcon(classId));
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

        private void OnStartRunRequested()
        {
            _controller.StartRun();
        }

        private void Refresh()
        {
            _view.SetDenarii(_state.Denarii);
            _view.SetRunAction(_run.IsActive);

            RefreshBuildings();
            RefreshItems();
            RefreshRoster();
            RefreshFormations();
        }

        private void RefreshFormations()
        {
            IReadOnlyList<FormationEntry> entries = _formationCatalog.Entries;

            var rowIndex = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                FormationEntry entry = entries[i];

                if (entry == null || entry.Formation == null)
                    continue;

                _view.RefreshFormationRow(
                    rowIndex,
                    new FormationRowData(
                        _state.IsFormationUnlocked(entry),
                        _state.MeetsRoundRequirement(entry),
                        _state.Denarii >= entry.Price,
                        entry.Price,
                        entry.RequiredBestRound));

                rowIndex++;
            }
        }

        private void RefreshBuildings()
        {
            IReadOnlyList<SchoolBuildingConfig> buildings =
                _schoolCatalog.Buildings;

            var rowIndex = 0;

            for (var i = 0; i < buildings.Count; i++)
            {
                SchoolBuildingConfig building = buildings[i];

                if (building == null)
                    continue;

                int level = _state.GetLevel(building);

                bool hasNextLevel =
                    building.TryGetUpgradeCost(level, out int cost);

                _view.RefreshRow(
                    rowIndex,
                    new BuildingRowData(
                        level,
                        building.MaxLevel,
                        cost,
                        hasNextLevel,
                        _state.CanUpgrade(building)));

                rowIndex++;
            }
        }

        private void RefreshRoster()
        {
            IReadOnlyList<RosterEntry> entries = _rosterCatalog.Entries;

            var rowIndex = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                RosterEntry entry = entries[i];

                if (entry == null || entry.Unit == null)
                    continue;

                _view.RefreshRosterRow(
                    rowIndex,
                    new RosterRowData(
                        _rosterNames[i],
                        _rosterIcons[i],
                        _state.IsUnitUnlocked(entry),
                        _state.MeetsRoundRequirement(entry),
                        _state.Denarii >= entry.Price,
                        entry.Price,
                        entry.RequiredBestRound));

                rowIndex++;
            }
        }

        private void RefreshItems()
        {
            IReadOnlyList<ItemConfig> items = _itemCatalog.Items;

            var rowIndex = 0;

            for (var i = 0; i < items.Count; i++)
            {
                ItemConfig item = items[i];

                if (item == null)
                    continue;

                _view.RefreshItemRow(
                    rowIndex,
                    new ItemRowData(
                        _state.IsOwned(item),
                        _state.IsEquipped(item),
                        _state.CanBuy(item),
                        item.Price));

                rowIndex++;
            }
        }
    }
}
