using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Связывает экран школы с её состоянием.
    /// </summary>
    public sealed class LupanariumPresenter : IStartable, IDisposable
    {
        private readonly LupanariumState _state;
        private readonly SchoolCatalog _catalog;
        private readonly LupanariumController _controller;
        private readonly LupanariumView _view;

        public LupanariumPresenter(
            LupanariumState state,
            SchoolCatalog catalog,
            LupanariumController controller,
            LupanariumView view)
        {
            _state = state ??
                throw new ArgumentNullException(nameof(state));

            _catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));

            _controller = controller ??
                throw new ArgumentNullException(nameof(controller));

            _view = view ??
                throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
            _view.StartRunRequested += OnStartRunRequested;

            _state.Changed += Refresh;

            _view.BuildRows(_catalog.Buildings);

            Refresh();
        }

        public void Dispose()
        {
            _view.UpgradeRequested -= OnUpgradeRequested;
            _view.StartRunRequested -= OnStartRunRequested;

            _state.Changed -= Refresh;
        }

        private void OnUpgradeRequested(SchoolBuildingConfig building)
        {
            // Состояние само поднимет Changed, если покупка прошла,
            // поэтому перерисовку здесь звать не нужно.
            _controller.TryUpgrade(building);
        }

        private void OnStartRunRequested()
        {
            _controller.StartRun();
        }

        private void Refresh()
        {
            _view.SetDenarii(_state.Denarii);

            IReadOnlyList<SchoolBuildingConfig> buildings =
                _catalog.Buildings;

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
    }
}
