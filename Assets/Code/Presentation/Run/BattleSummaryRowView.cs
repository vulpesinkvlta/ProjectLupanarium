using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Строка итогов боя по одному классу гладиаторов.
    /// Презентер считает эти числа, вьюха их только показывает.
    /// </summary>
    public readonly struct BattleSummaryRowData
    {
        public string ClassName { get; }
        public Sprite Icon { get; }

        public int Survivors { get; }
        public int Deployed { get; }
        public int Kills { get; }

        public float DamageDealt { get; }
        public float DamagePrevented { get; }
        public float DamageTaken { get; }

        public BattleSummaryRowData(
            string className,
            Sprite icon,
            int survivors,
            int deployed,
            int kills,
            float damageDealt,
            float damagePrevented,
            float damageTaken)
        {
            ClassName = className;
            Icon = icon;
            Survivors = survivors;
            Deployed = deployed;
            Kills = kills;
            DamageDealt = damageDealt;
            DamagePrevented = damagePrevented;
            DamageTaken = damageTaken;
        }
    }

    public sealed class BattleSummaryRowView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _survivorsLabel;
        [SerializeField] private TMP_Text _killsLabel;
        [SerializeField] private TMP_Text _damageDealtLabel;
        [SerializeField] private TMP_Text _damagePreventedLabel;
        [SerializeField] private TMP_Text _damageTakenLabel;

        public void Bind(BattleSummaryRowData data)
        {
            gameObject.name = $"SummaryRow_{data.ClassName}";

            if (_nameLabel != null)
                _nameLabel.text = data.ClassName;

            // Как в референсе: сколько осталось из скольких вышло.
            if (_survivorsLabel != null)
                _survivorsLabel.text = $"{data.Survivors}/{data.Deployed}";

            if (_killsLabel != null)
                _killsLabel.text = data.Kills.ToString();

            if (_damageDealtLabel != null)
                _damageDealtLabel.text = Mathf.RoundToInt(data.DamageDealt).ToString();

            if (_damagePreventedLabel != null)
            {
                _damagePreventedLabel.text =
                    Mathf.RoundToInt(data.DamagePrevented).ToString();
            }

            if (_damageTakenLabel != null)
                _damageTakenLabel.text = Mathf.RoundToInt(data.DamageTaken).ToString();

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.enabled = data.Icon != null;
            }

            gameObject.SetActive(true);
        }
    }
}
