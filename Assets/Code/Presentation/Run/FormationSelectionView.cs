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
        [SerializeField] private TMP_Text _balanceLabel;
        [SerializeField] private UnityEngine.UI.Button _noFormationButton;
        [SerializeField] private UnityEngine.UI.Button _returnToBaseButton;

        [Header("Cards")]
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private FormationOptionCardView _cardPrefab;

        private readonly List<FormationOptionCardView> _cards = new(8);

        public event Action<int> OptionSelected;
        public event Action<int> PurchaseRequested;
        public event Action ReturnToBaseRequested;

        public void SetBalance(int denarii, int bestRound)
        {
            if (_balanceLabel != null)
                _balanceLabel.text = $"Денарии: {denarii}   •   Лучший раунд: {bestRound}\n" +
                    "Покупка навсегда. Денарии пополняются при возврате на базу.";
        }

        public void Show(IReadOnlyList<FormationOptionData> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (_titleLabel != null)
                _titleLabel.text = "Выберите строй перед боем";

            BuildCards(options);
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void BuildCards(IReadOnlyList<FormationOptionData> options)
        {
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
                if (i >= _cards.Count)
                {
                    FormationOptionCardView created = Instantiate(_cardPrefab, _cardsRoot);
                    created.Clicked += OnCardClicked;
                    created.PurchaseRequested += OnPurchaseRequested;
                    _cards.Add(created);
                }
                _cards[i].Bind(i, options[i]);
            }
            for (int i = options.Count; i < _cards.Count; i++)
                _cards[i].Hide();
        }

        private void ClearCards()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null)
                    continue;

                _cards[i].Clicked -= OnCardClicked;
                _cards[i].PurchaseRequested -= OnPurchaseRequested;
                Destroy(_cards[i].gameObject);
            }

            _cards.Clear();
        }

        private void Awake()
        {
            if (_noFormationButton != null)
                _noFormationButton.onClick.AddListener(OnNoFormationClicked);
            if (_returnToBaseButton != null)
                _returnToBaseButton.onClick.AddListener(OnReturnToBaseClicked);
            SetRootActive(false);
        }

        private void OnDestroy()
        {
            if (_noFormationButton != null)
                _noFormationButton.onClick.RemoveListener(OnNoFormationClicked);
            if (_returnToBaseButton != null)
                _returnToBaseButton.onClick.RemoveListener(OnReturnToBaseClicked);
            ClearCards();
        }

        private void OnCardClicked(int index)
        {
            OptionSelected?.Invoke(index);
        }

        private void OnPurchaseRequested(int index) => PurchaseRequested?.Invoke(index);
        private void OnNoFormationClicked() => OptionSelected?.Invoke(0);
        private void OnReturnToBaseClicked() => ReturnToBaseRequested?.Invoke();

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
                _root.SetActive(isActive);
        }
    }
}
