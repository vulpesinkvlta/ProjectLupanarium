using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Экран сбора стартового отряда: выбор из открытых типов бойцов.
    ///
    /// Карточки создаются под список: набор открытых бойцов растёт
    /// по ходу игры, и заранее нарезать их в сцене нельзя.
    /// </summary>
    public sealed class SquadSelectionView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleLabel;

        [Header("Cards")]
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private SquadOptionCardView _cardPrefab;

        private readonly List<SquadOptionCardView> _cards = new(8);

        public event Action<int> OptionSelected;

        public void Show(IReadOnlyList<SquadOptionData> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (_titleLabel != null)
                _titleLabel.text = "С кем выходим на песок?";

            BuildCards(options);
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void BuildCards(IReadOnlyList<SquadOptionData> options)
        {
            ClearCards();

            if (_cardPrefab == null || _cardsRoot == null)
            {
                Debug.LogError(
                    "[SquadSelectionView] Не назначен префаб карточки " +
                    "или её корень — выбрать отряд будет нечем.",
                    this);

                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                SquadOptionCardView card =
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

    /// <summary>
    /// Готовые данные одной карточки отряда.
    ///
    /// Статы приходят уже посчитанными, с бонусами школы и снаряжения:
    /// карточка не должна знать ни про модификаторы, ни про то, откуда
    /// они берутся, — её дело нарисовать числа.
    /// </summary>
    public readonly struct SquadOptionData
    {
        public string ClassName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public int Count { get; }

        public float Health { get; }
        public float Damage { get; }
        public float Speed { get; }

        public SquadOptionData(
            string className,
            string description,
            Sprite icon,
            int count,
            float health,
            float damage,
            float speed)
        {
            ClassName = className;
            Description = description;
            Icon = icon;
            Count = count;
            Health = health;
            Damage = damage;
            Speed = speed;
        }
    }
}
