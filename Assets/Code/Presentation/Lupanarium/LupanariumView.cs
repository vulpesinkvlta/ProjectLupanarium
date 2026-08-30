using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран гладиаторской школы.
    /// </summary>
    public sealed class LupanariumView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _denariiLabel;

        [Header("Buildings")]
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private SchoolBuildingRowView _rowPrefab;

        [Header("Armory (необязательно)")]
        [SerializeField] private Transform _itemRowsRoot;
        [SerializeField] private ItemRowView _itemRowPrefab;

        [Header("Actions")]
        [SerializeField] private Button _startRunButton;

        private readonly List<SchoolBuildingRowView> _rows = new(16);
        private readonly List<ItemRowView> _itemRows = new(16);

        public event Action<SchoolBuildingConfig> UpgradeRequested;
        public event Action StartRunRequested;

        /// <summary>Нажата кнопка предмета: купить либо надеть.</summary>
        public event Action<ItemConfig> ItemActionRequested;

        public int RowCount => _rows.Count;

        /// <summary>
        /// Создаёт строки под список построек. Вызывается один раз:
        /// набор построек за игру не меняется, меняются только уровни.
        /// </summary>
        public void BuildRows(IReadOnlyList<SchoolBuildingConfig> buildings)
        {
            if (buildings == null)
                throw new ArgumentNullException(nameof(buildings));

            ClearRows();

            if (_rowPrefab == null || _rowsRoot == null)
            {
                Debug.LogError(
                    "[LupanariumView] Не назначен префаб строки " +
                    "или корень списка.",
                    this);

                return;
            }

            for (var i = 0; i < buildings.Count; i++)
            {
                SchoolBuildingConfig building = buildings[i];

                if (building == null)
                    continue;

                SchoolBuildingRowView row =
                    Instantiate(_rowPrefab, _rowsRoot);

                row.Bind(building);
                row.UpgradeRequested += OnRowUpgradeRequested;

                _rows.Add(row);
            }
        }

        /// <summary>
        /// Создаёт строки арсенала. Если корень или префаб не назначены,
        /// арсенал просто не показывается — остальной экран работает.
        /// </summary>
        public void BuildItemRows(IReadOnlyList<ItemConfig> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            ClearItemRows();

            if (_itemRowPrefab == null || _itemRowsRoot == null)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                ItemConfig item = items[i];

                if (item == null)
                    continue;

                ItemRowView row = Instantiate(_itemRowPrefab, _itemRowsRoot);

                row.Bind(item);
                row.ActionRequested += OnItemActionRequested;

                _itemRows.Add(row);
            }
        }

        public void RefreshItemRow(int index, ItemRowData data)
        {
            if (index < 0 || index >= _itemRows.Count)
                return;

            _itemRows[index].Refresh(data);
        }

        public void RefreshRow(int index, BuildingRowData data)
        {
            if (index < 0 || index >= _rows.Count)
                return;

            _rows[index].Refresh(data);
        }

        public void SetDenarii(int denarii)
        {
            if (_denariiLabel != null)
                _denariiLabel.text = $"Денарии: {denarii}";
        }

        private void Awake()
        {
            if (_startRunButton != null)
                _startRunButton.onClick.AddListener(OnStartRunClicked);
        }

        private void OnDestroy()
        {
            if (_startRunButton != null)
                _startRunButton.onClick.RemoveListener(OnStartRunClicked);

            ClearRows();
            ClearItemRows();
        }

        private void ClearItemRows()
        {
            for (var i = 0; i < _itemRows.Count; i++)
            {
                ItemRowView row = _itemRows[i];

                if (row == null)
                    continue;

                row.ActionRequested -= OnItemActionRequested;
                Destroy(row.gameObject);
            }

            _itemRows.Clear();
        }

        private void OnItemActionRequested(ItemConfig item)
        {
            ItemActionRequested?.Invoke(item);
        }

        private void ClearRows()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                SchoolBuildingRowView row = _rows[i];

                if (row == null)
                    continue;

                row.UpgradeRequested -= OnRowUpgradeRequested;
                Destroy(row.gameObject);
            }

            _rows.Clear();
        }

        private void OnRowUpgradeRequested(SchoolBuildingConfig building)
        {
            UpgradeRequested?.Invoke(building);
        }

        private void OnStartRunClicked()
        {
            StartRunRequested?.Invoke();
        }
    }
}
