using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>Готовые данные одной карточки строя.</summary>
    public readonly struct FormationOptionData
    {
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }

        /// <summary>Этот строй уже выбран на текущий бой.</summary>
        public bool IsSelected { get; }
        public bool IsUnlocked { get; }
        public bool CanPurchase { get; }
        public string Status { get; }
        public string ActionLabel { get; }

        public FormationOptionData(
            string displayName,
            string description,
            Sprite icon,
            bool isSelected,
            bool isUnlocked,
            bool canPurchase,
            string status,
            string actionLabel)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            IsSelected = isSelected;
            IsUnlocked = isUnlocked;
            CanPurchase = canPurchase;
            Status = status;
            ActionLabel = actionLabel;
        }
    }

    /// <summary>
    /// Карточка строя на экране выбора перед боем.
    ///
    /// Показывает условие открытия, покупку или выбор уже купленного строя.
    /// </summary>
    public sealed class FormationOptionCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private TMP_Text _actionLabel;

        [Tooltip("Необязательно: рамка вокруг уже выбранного строя.")]
        [SerializeField] private GameObject _selectedMark;

        private int _index = -1;
        private bool _isUnlocked;
        private bool _canPurchase;

        public event Action<int> Clicked;
        public event Action<int> PurchaseRequested;

        public void Bind(int index, FormationOptionData data)
        {
            _index = index;
            _isUnlocked = data.IsUnlocked;
            _canPurchase = data.CanPurchase;
            if (_button != null)
                _button.interactable = _isUnlocked || _canPurchase;
            if (_statusLabel != null)
                _statusLabel.text = data.Status;
            if (_actionLabel != null)
                _actionLabel.text = data.ActionLabel;

            if (_nameLabel != null)
                _nameLabel.text = data.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = data.Description;

            if (_selectedMark != null)
                _selectedMark.SetActive(data.IsSelected);

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
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

            if (_isUnlocked)
                Clicked?.Invoke(_index);
            else if (_canPurchase)
                PurchaseRequested?.Invoke(_index);
        }
    }
}
