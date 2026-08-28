using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Одна карточка улучшения на экране награды.
    /// </summary>
    public sealed class UpgradeCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        private int _index = -1;

        /// <summary>Игрок выбрал эту карточку. Аргумент — её индекс в выдаче.</summary>
        public event Action<int> Clicked;

        public void Bind(int index, UpgradeConfig upgrade)
        {
            if (upgrade == null)
                throw new ArgumentNullException(nameof(upgrade));

            _index = index;

            if (_nameLabel != null)
                _nameLabel.text = upgrade.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = upgrade.Description;

            if (_icon != null)
            {
                _icon.sprite = upgrade.Icon;
                _icon.enabled = upgrade.Icon != null;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _index = -1;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (_index < 0)
                return;

            Clicked?.Invoke(_index);
        }
    }
}
