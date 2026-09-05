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

        public FormationOptionData(
            string displayName,
            string description,
            Sprite icon,
            bool isSelected)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            IsSelected = isSelected;
        }
    }

    /// <summary>
    /// Карточка строя на экране выбора перед боем.
    ///
    /// Про открытость строя не знает: до экрана выбора доезжают только
    /// открытые варианты. Закрытые строи живут на экране школы.
    /// </summary>
    public sealed class FormationOptionCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        [Tooltip("Необязательно: рамка вокруг уже выбранного строя.")]
        [SerializeField] private GameObject _selectedMark;

        private int _index = -1;

        public event Action<int> Clicked;

        public void Bind(int index, FormationOptionData data)
        {
            _index = index;

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

            Clicked?.Invoke(_index);
        }
    }
}
