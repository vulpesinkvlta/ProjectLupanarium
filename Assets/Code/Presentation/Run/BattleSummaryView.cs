using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран итогов боя: сколько заработано, кого перебили и как
    /// отработал каждый класс гладиаторов.
    /// </summary>
    public sealed class BattleSummaryView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _root;

        [Header("Header")]
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _roundLabel;
        [SerializeField] private TMP_Text _earnedLabel;

        [Tooltip("Список убитых врагов по типам, многострочный.")]
        [SerializeField] private TMP_Text _killsLabel;

        [Header("Per-class rows")]
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private BattleSummaryRowView _rowPrefab;

        [Header("Actions")]
        [SerializeField] private Button _claimButton;

        private readonly List<BattleSummaryRowView> _rows = new(8);

        public event Action ClaimRequested;

        public void Show(
            int roundNumber,
            int goldEarned,
            string killsText,
            IReadOnlyList<BattleSummaryRowData> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            if (_titleLabel != null)
                _titleLabel.text = "ПОБЕДА!";

            if (_roundLabel != null)
                _roundLabel.text = $"Раунд {roundNumber}";

            if (_earnedLabel != null)
                _earnedLabel.text = $"Заработано {goldEarned}";

            if (_killsLabel != null)
                _killsLabel.text = killsText;

            BuildRows(rows);
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        /// <summary>
        /// Строки создаются под каждый показ: набор классов в отряде
        /// меняется от боя к бою, а их всего единицы — пул тут был бы
        /// сложнее самой задачи.
        /// </summary>
        private void BuildRows(IReadOnlyList<BattleSummaryRowData> rows)
        {
            ClearRows();

            if (_rowPrefab == null || _rowsRoot == null)
                return;

            for (var i = 0; i < rows.Count; i++)
            {
                BattleSummaryRowView row =
                    Instantiate(_rowPrefab, _rowsRoot);

                row.Bind(rows[i]);
                _rows.Add(row);
            }
        }

        private void ClearRows()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }

        private void Awake()
        {
            if (_claimButton != null)
                _claimButton.onClick.AddListener(OnClaimClicked);

            SetRootActive(false);
        }

        private void OnDestroy()
        {
            if (_claimButton != null)
                _claimButton.onClick.RemoveListener(OnClaimClicked);

            ClearRows();
        }

        private void OnClaimClicked()
        {
            ClaimRequested?.Invoke();
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
                _root.SetActive(isActive);
        }
    }
}
