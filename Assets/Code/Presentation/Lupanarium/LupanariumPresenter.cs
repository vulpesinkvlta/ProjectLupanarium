using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает экран школы с её состоянием: постройки и арсенал.
    /// </summary>
    public sealed class LupanariumPresenter : IStartable, IDisposable
    {
        private readonly LupanariumState _state;
        private readonly SchoolCatalog _schoolCatalog;
        private readonly ItemCatalog _itemCatalog;
        private readonly LupanariumController _controller;
        private readonly LupanariumView _view;

        public LupanariumPresenter(
            LupanariumState state,
            SchoolCatalog schoolCatalog,
            ItemCatalog itemCatalog,
            LupanariumController controller,
            LupanariumView view)
        {
            _state = state ??
                throw new ArgumentNullException(nameof(state));

            _schoolCatalog = schoolCatalog ??
                throw new ArgumentNullException(nameof(schoolCatalog));

            _itemCatalog = itemCatalog ??
                throw new ArgumentNullException(nameof(itemCatalog));

            _controller = controller ??
                throw new ArgumentNullException(nameof(controller));

            _view = view ??
                throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
            _view.ItemActionRequested += OnItemActionRequested;
            _view.StartRunRequested += OnStartRunRequested;

            _state.Changed += Refresh;

            _view.BuildRows(_schoolCatalog.Buildings);
            _view.BuildItemRows(_itemCatalog.Items);

            Refresh();
        }

        public void Dispose()
        {
            _view.UpgradeRequested -= OnUpgradeRequested;
            _view.ItemActionRequested -= OnItemActionRequested;
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

        private void OnStartRunRequested()
        {
            _controller.StartRun();
        }

        private void Refresh()
        {
            _view.SetDenarii(_state.Denarii);

            RefreshBuildings();
            RefreshItems();
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
