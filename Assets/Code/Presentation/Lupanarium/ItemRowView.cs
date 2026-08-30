using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Состояние строки предмета, посчитанное презентером.
    /// </summary>
    public readonly struct ItemRowData
    {
        public bool IsOwned { get; }
        public bool IsEquipped { get; }
        public bool CanAfford { get; }
        public int Price { get; }

        public ItemRowData(
            bool isOwned,
            bool isEquipped,
            bool canAfford,
            int price)
        {
            IsOwned = isOwned;
            IsEquipped = isEquipped;
            CanAfford = canAfford;
            Price = price;
        }
    }

    public sealed class ItemRowView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        private ItemConfig _item;

        /// <summary>Нажата кнопка действия: купить либо надеть.</summary>
        public event Action<ItemConfig> ActionRequested;

        public void Bind(ItemConfig item)
        {
            _item = item ??
                throw new ArgumentNullException(nameof(item));

            gameObject.name = $"ItemRow_{item.Id}";

            if (_nameLabel != null)
                _nameLabel.text = item.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = item.Description;

            if (_icon != null)
            {
                _icon.sprite = item.Icon;
                _icon.enabled = item.Icon != null;
            }
        }

        public void Refresh(ItemRowData data)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = data.IsEquipped
                    ? "надет"
                    : data.IsOwned
                        ? "в сундуке"
                        : $"{data.Price} ден.";
            }

            if (_actionLabel != null)
            {
                _actionLabel.text = data.IsOwned
                    ? "Надеть"
                    : "Купить";
            }

            if (_actionButton != null)
            {
                _actionButton.interactable = data.IsEquipped
                    ? false
                    : data.IsOwned || data.CanAfford;
            }
        }

        private void Awake()
        {
            if (_actionButton != null)
                _actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnDestroy()
        {
            if (_actionButton != null)
                _actionButton.onClick.RemoveListener(OnActionClicked);
        }

        private void OnActionClicked()
        {
            if (_item == null)
                return;

            ActionRequested?.Invoke(_item);
        }
    }
}
