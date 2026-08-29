using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Данные одной строки постройки. Презентер считает их из состояния,
    /// вьюха только показывает — про LupanariumState она не знает.
    /// </summary>
    public readonly struct BuildingRowData
    {
        public int Level { get; }
        public int MaxLevel { get; }
        public int Cost { get; }
        public bool HasNextLevel { get; }
        public bool CanAfford { get; }

        public BuildingRowData(
            int level,
            int maxLevel,
            int cost,
            bool hasNextLevel,
            bool canAfford)
        {
            Level = level;
            MaxLevel = maxLevel;
            Cost = cost;
            HasNextLevel = hasNextLevel;
            CanAfford = canAfford;
        }
    }

    public sealed class SchoolBuildingRowView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _costLabel;
        [SerializeField] private Button _upgradeButton;

        private SchoolBuildingConfig _building;

        public event Action<SchoolBuildingConfig> UpgradeRequested;

        public void Bind(SchoolBuildingConfig building)
        {
            _building = building ??
                throw new ArgumentNullException(nameof(building));

            gameObject.name = $"BuildingRow_{building.Id}";

            if (_nameLabel != null)
                _nameLabel.text = building.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = building.Description;

            if (_icon != null)
            {
                _icon.sprite = building.Icon;
                _icon.enabled = building.Icon != null;
            }
        }

        public void Refresh(BuildingRowData data)
        {
            if (_levelLabel != null)
                _levelLabel.text = $"{data.Level}/{data.MaxLevel}";

            if (_costLabel != null)
            {
                _costLabel.text = data.HasNextLevel
                    ? $"{data.Cost} ден."
                    : "максимум";
            }

            if (_upgradeButton != null)
                _upgradeButton.interactable = data.HasNextLevel && data.CanAfford;
        }

        private void Awake()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }

        private void OnUpgradeClicked()
        {
            if (_building == null)
                return;

            UpgradeRequested?.Invoke(_building);
        }
    }
}
