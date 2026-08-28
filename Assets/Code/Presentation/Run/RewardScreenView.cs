using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран выбора улучшения после победы.
    /// </summary>
    public sealed class RewardScreenView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _root;

        [Header("Cards")]
        [SerializeField] private UpgradeCardView[] _cards;

        /// <summary>Выбран вариант с этим индексом.</summary>
        public event Action<int> UpgradeSelected;

        public void Show(IReadOnlyList<UpgradeConfig> upgrades)
        {
            if (upgrades == null)
                throw new ArgumentNullException(nameof(upgrades));

            for (var i = 0; i < _cards.Length; i++)
            {
                UpgradeCardView card = _cards[i];

                if (card == null)
                    continue;

                if (i < upgrades.Count)
                    card.Bind(i, upgrades[i]);
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
                _cards = Array.Empty<UpgradeCardView>();

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
            UpgradeSelected?.Invoke(index);
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
                _root.SetActive(isActive);
        }
    }
}
