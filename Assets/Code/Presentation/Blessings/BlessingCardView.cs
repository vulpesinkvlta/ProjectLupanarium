using System;
using TMPro;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class BlessingCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private UnityEngine.UI.Button _button;
        public BlessingConfig Config { get; private set; }
        public event Action<BlessingConfig> Clicked;

        private void Awake() => _button.onClick.AddListener(Click);
        private void OnDestroy() => _button.onClick.RemoveListener(Click);
        private void Click() => Clicked?.Invoke(Config);
        public void Bind(BlessingConfig config)
        {
            Config = config;
            _name.text = config.DisplayName;
            _description.text = config.Description;
            _icon.sprite = config.Icon;
            _icon.color = config.Icon != null ? Color.white : config.Color;
        }
        public void Refresh(LupanariumState school, bool shop, bool available)
        {
            int count = school.GetBlessingCharges(Config.Id);
            _actionLabel.text = shop ? L10n.F($"Купить · {Config.Price} ден.\nВ запасе: {count}") : L10n.F($"Использовать · {count}");
            _button.interactable = available && (shop ? school.CanBuyBlessing(Config) : count > 0);
        }
    }
}
