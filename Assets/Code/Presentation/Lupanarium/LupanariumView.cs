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

        [Header("Actions")]
        [SerializeField] private Button _startRunButton;

        private readonly List<SchoolBuildingRowView> _rows = new(16);

        public event Action<SchoolBuildingConfig> UpgradeRequested;
        public event Action StartRunRequested;

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
