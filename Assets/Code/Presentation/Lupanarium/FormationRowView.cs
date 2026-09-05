using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>Состояние строки строя, посчитанное презентером.</summary>
    public readonly struct FormationRowData
    {
        public bool IsUnlocked { get; }
        public bool MeetsRound { get; }
        public bool CanAfford { get; }

        public int Price { get; }
        public int RequiredRound { get; }

        public FormationRowData(
            bool isUnlocked,
            bool meetsRound,
            bool canAfford,
            int price,
            int requiredRound)
        {
            IsUnlocked = isUnlocked;
            MeetsRound = meetsRound;
            CanAfford = canAfford;
            Price = price;
            RequiredRound = requiredRound;
        }
    }

    /// <summary>
    /// Строка строя на экране школы: открыть за денарии.
    /// Устроена так же, как строка ростера — те же правила открытия,
    /// тот же порядок показа порога и цены.
    /// </summary>
    public sealed class FormationRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private Button _unlockButton;
        [SerializeField] private TMP_Text _unlockLabel;

        private FormationEntry _entry;

        public event Action<FormationEntry> UnlockRequested;

        public void Bind(FormationEntry entry)
        {
            _entry = entry ??
                throw new ArgumentNullException(nameof(entry));

            FormationConfig formation = entry.Formation;

            gameObject.name = $"FormationRow_{entry.Id}";

            if (_nameLabel != null)
                _nameLabel.text = formation.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = formation.Description;

            if (_icon != null)
            {
                _icon.sprite = formation.Icon;
                _icon.enabled = formation.Icon != null;
            }
        }

        public void Refresh(FormationRowData data)
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
