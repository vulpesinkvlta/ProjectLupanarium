using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Карточка контракта на экране выбора.
    /// </summary>
    public sealed class ContractCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _enemiesLabel;
        [SerializeField] private TMP_Text _rewardLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        private int _index = -1;

        public event Action<int> Clicked;

        public void Bind(int index, ContractOffer offer, string enemiesText)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            _index = index;

            if (_nameLabel != null)
                _nameLabel.text = offer.DisplayName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = offer.Description;

            if (_enemiesLabel != null)
                _enemiesLabel.text = enemiesText;

            if (_rewardLabel != null)
                _rewardLabel.text = $"{offer.GoldReward} золота";

            if (_icon != null)
            {
                Sprite sprite = offer.Source != null
                    ? offer.Source.Icon
                    : null;

                _icon.sprite = sprite;
                _icon.enabled = sprite != null;
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
