using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Карточка стартового отряда: тип бойца, сколько их даётся
    /// и его боевые характеристики.
    ///
    /// Все ярлыки необязательны: карточку можно собрать хоть с одним
    /// названием, хоть с полной таблицей статов.
    /// </summary>
    public sealed class SquadOptionCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private TMP_Text _descriptionLabel;

        [Header("Stats (необязательно)")]
        [SerializeField] private TMP_Text _healthLabel;
        [SerializeField] private TMP_Text _damageLabel;
        [SerializeField] private TMP_Text _speedLabel;

        private int _index = -1;

        public event Action<int> Clicked;

        public void Bind(int index, SquadOptionData data)
        {
            _index = index;

            if (_nameLabel != null)
                _nameLabel.text = data.ClassName;

            if (_countLabel != null)
                _countLabel.text = $"{data.Count} бойцов";

            if (_descriptionLabel != null)
                _descriptionLabel.text = data.Description;

            // Здоровье и урон округляем: дробные значения появляются
            // из бонусов школы и на карточке только мешают сравнивать.
            if (_healthLabel != null)
                _healthLabel.text = $"HP {Mathf.RoundToInt(data.Health)}";

            if (_damageLabel != null)
                _damageLabel.text = $"Урон {Mathf.RoundToInt(data.Damage)}";

            // Скорость целыми числами почти не отличается — оставляем
            // один знак после запятой, иначе колонка выглядит одинаковой.
            if (_speedLabel != null)
                _speedLabel.text = $"Скорость {data.Speed:0.0}";

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
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
