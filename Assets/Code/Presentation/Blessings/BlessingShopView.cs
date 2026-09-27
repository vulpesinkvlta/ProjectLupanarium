using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class BlessingShopView : MonoBehaviour
    {
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private BlessingCardView _cardPrefab;
        private readonly List<BlessingCardView> _cards = new();
        public event Action<BlessingConfig> BuyRequested;
        public void Build(BlessingCatalog catalog)
        {
            if (_cards.Count > 0) return;
            foreach (var config in catalog.Blessings)
            {
                if (config == null) continue;
                var card = Instantiate(_cardPrefab, _cardsRoot);
                card.Bind(config);
                card.Clicked += Buy;
                _cards.Add(card);
            }
        }
        public void Refresh(LupanariumState school)
        {
            foreach (var card in _cards) card.Refresh(school, true, true);
        }
        private void Buy(BlessingConfig config) => BuyRequested?.Invoke(config);
        private void OnDestroy()
        {
            foreach (var card in _cards) if (card != null) card.Clicked -= Buy;
        }
    }
}
