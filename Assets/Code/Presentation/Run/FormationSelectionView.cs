using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран выбора строя перед боем.
    ///
    /// Карточки создаются под список, как и на выборе отряда: строи
    /// открываются по ходу игры, и нарезать их в сцене заранее нельзя.
    /// </summary>
    public sealed class FormationSelectionView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleLabel;

        [Header("Cards")]
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private FormationOptionCardView _cardPrefab;

        private readonly List<FormationOptionCardView> _cards = new(8);

        public event Action<int> OptionSelected;

        public void Show(IReadOnlyList<FormationOptionData> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (_titleLabel != null)
                _titleLabel.text = "Каким строем выходим?";

            BuildCards(options);
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void BuildCards(IReadOnlyList<FormationOptionData> options)
        {
            ClearCards();

            if (_cardPrefab == null || _cardsRoot == null)
            {
                Debug.LogError(
                    "[FormationSelectionView] Не назначен префаб карточки " +
                    "или её корень — выбрать строй будет нечем.",
                    this);

                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                FormationOptionCardView card =
                    Instantiate(_cardPrefab, _cardsRoot);

                card.Bind(i, options[i]);

                card.Clicked += OnCardClicked;
                _cards.Add(card);
            }
        }

        private void ClearCards()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null)
                    continue;

                _cards[i].Clicked -= OnCardClicked;
                Destroy(_cards[i].gameObject);
            }

            _cards.Clear();
        }

        private void Awake()
        {
            SetRootActive(false);
        }

        private void OnDestroy()
        {
            ClearCards();
        }

        private void OnCardClicked(int index)
        {
            OptionSelected?.Invoke(index);
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
                _root.SetActive(isActive);
        }
    }
}
