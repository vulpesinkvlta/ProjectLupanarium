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

        [Header("Roster (необязательно)")]
        [SerializeField] private Transform _rosterRowsRoot;
        [SerializeField] private RosterRowView _rosterRowPrefab;

        [Header("Actions")]
        [SerializeField] private Button _startRunButton;

        private readonly List<SchoolBuildingRowView> _rows = new(16);
        private readonly List<ItemRowView> _itemRows = new(16);
        private readonly List<RosterRowView> _rosterRows = new(16);

        public event Action<SchoolBuildingConfig> UpgradeRequested;
        public event Action StartRunRequested;

        /// <summary>Нажата кнопка предмета: купить либо надеть.</summary>
        public event Action<ItemConfig> ItemActionRequested;

        /// <summary>Нажата кнопка открытия нового типа гладиаторов.</summary>
        public event Action<RosterEntry> RosterUnlockRequested;

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

        /// <summary>
        /// Создаёт строки ростера. Как и арсенал, секция необязательна:
        /// не назначили корень — школа работает без неё.
        /// </summary>
        public void BuildRosterRows(
            IReadOnlyList<RosterEntry> entries,
            IReadOnlyList<string> names,
            IReadOnlyList<Sprite> icons)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            ClearRosterRows();

            if (_rosterRowPrefab == null || _rosterRowsRoot == null)
                return;

            for (var i = 0; i < entries.Count; i++)
            {
                RosterEntry entry = entries[i];

                if (entry == null || entry.Unit == null)
                    continue;

                RosterRowView row =
                    Instantiate(_rosterRowPrefab, _rosterRowsRoot);

                row.Bind(entry, names[i], icons[i]);
                row.UnlockRequested += OnRosterUnlockRequested;

                _rosterRows.Add(row);
            }
        }

        public void RefreshRosterRow(int index, RosterRowData data)
        {
            if (index < 0 || index >= _rosterRows.Count)
                return;

            _rosterRows[index].Refresh(data);
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
            ClearRosterRows();
        }

        private void ClearRosterRows()
        {
            for (var i = 0; i < _rosterRows.Count; i++)
            {
                RosterRowView row = _rosterRows[i];

                if (row == null)
                    continue;

                row.UnlockRequested -= OnRosterUnlockRequested;
                Destroy(row.gameObject);
            }

            _rosterRows.Clear();
        }

        private void OnRosterUnlockRequested(RosterEntry entry)
        {
            RosterUnlockRequested?.Invoke(entry);
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
