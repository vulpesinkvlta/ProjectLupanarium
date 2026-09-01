using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Карточка стартового отряда: тип бойца и сколько их даётся.
    /// </summary>
    public sealed class SquadOptionCardView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _countLabel;

        private int _index = -1;

        public event Action<int> Clicked;

        public void Bind(int index, string className, Sprite icon, int count)
        {
            _index = index;

            if (_nameLabel != null)
                _nameLabel.text = className;

            if (_countLabel != null)
                _countLabel.text = $"{count} бойцов";

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
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
