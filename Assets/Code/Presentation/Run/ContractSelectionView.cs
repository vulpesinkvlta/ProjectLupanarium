using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран выбора контракта: с кем драться в этом раунде.
    /// </summary>
    public sealed class ContractSelectionView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _roundLabel;

        [Header("Cards")]
        [SerializeField] private ContractCardView[] _cards;

        /// <summary>Выбран контракт с этим индексом.</summary>
        public event Action<int> ContractSelected;

        public void Show(
            int roundNumber,
            IReadOnlyList<ContractOffer> offers,
            IReadOnlyList<string> enemyTexts)
        {
            if (offers == null)
                throw new ArgumentNullException(nameof(offers));

            if (_roundLabel != null)
                _roundLabel.text = $"Раунд {roundNumber}";

            for (var i = 0; i < _cards.Length; i++)
            {
                ContractCardView card = _cards[i];

                if (card == null)
                    continue;

                // Пустое предложение означает, что подходящих контрактов
                // в каталоге меньше, чем карточек на экране.
                bool hasOffer =
                    i < offers.Count && offers[i].Source != null;

                if (hasOffer)
                    card.Bind(i, offers[i], enemyTexts[i]);
                else
                    card.Hide();
            }

            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void Awake()
        {
            if (_cards == null)
                _cards = Array.Empty<ContractCardView>();

            for (var i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                    _cards[i].Clicked += OnCardClicked;
            }

            SetRootActive(false);
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                    _cards[i].Clicked -= OnCardClicked;
            }
        }

        private void OnCardClicked(int index)
        {
            ContractSelected?.Invoke(index);
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
                _root.SetActive(isActive);
        }
    }
}
