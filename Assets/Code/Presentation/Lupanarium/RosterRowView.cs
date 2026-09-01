using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>Состояние строки ростера, посчитанное презентером.</summary>
    public readonly struct RosterRowData
    {
        public string ClassName { get; }
        public Sprite Icon { get; }

        public bool IsUnlocked { get; }
        public bool MeetsRound { get; }
        public bool CanAfford { get; }

        public int Price { get; }
        public int RequiredRound { get; }

        public RosterRowData(
            string className,
            Sprite icon,
            bool isUnlocked,
            bool meetsRound,
            bool canAfford,
            int price,
            int requiredRound)
        {
            ClassName = className;
            Icon = icon;
            IsUnlocked = isUnlocked;
            MeetsRound = meetsRound;
            CanAfford = canAfford;
            Price = price;
            RequiredRound = requiredRound;
        }
    }

    public sealed class RosterRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private Button _unlockButton;
        [SerializeField] private TMP_Text _unlockLabel;

        private RosterEntry _entry;

        public event Action<RosterEntry> UnlockRequested;

        public void Bind(RosterEntry entry, string className, Sprite icon)
        {
            _entry = entry ??
                throw new ArgumentNullException(nameof(entry));

            gameObject.name = $"RosterRow_{entry.Id}";

            if (_nameLabel != null)
                _nameLabel.text = className;

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }
        }

        public void Refresh(RosterRowData data)
        {
            if (_statusLabel != null)
            {
                // Порог по раунду важнее цены: пока игрок не дошёл,
                // сумма денариев ему ничего не скажет.
                _statusLabel.text = data.IsUnlocked
                    ? "открыт"
                    : !data.MeetsRound
                        ? $"с раунда {data.RequiredRound}"
                        : $"{data.Price} ден.";
            }

            if (_unlockLabel != null)
                _unlockLabel.text = data.IsUnlocked ? "Открыт" : "Открыть";

            if (_unlockButton != null)
            {
                _unlockButton.interactable =
                    !data.IsUnlocked && data.MeetsRound && data.CanAfford;
            }
        }

        private void Awake()
        {
            if (_unlockButton != null)
                _unlockButton.onClick.AddListener(OnUnlockClicked);
        }

        private void OnDestroy()
        {
            if (_unlockButton != null)
                _unlockButton.onClick.RemoveListener(OnUnlockClicked);
        }

        private void OnUnlockClicked()
        {
            if (_entry == null)
                return;

            UnlockRequested?.Invoke(_entry);
        }
    }
}
