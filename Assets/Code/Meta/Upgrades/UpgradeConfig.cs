using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Карточка улучшения, которую игрок выбирает после победы.
    ///
    /// Один тип обслуживает и бонусы к статам, и пополнение отряда:
    /// для игрока это одинаковые карточки на одном экране, разделять их
    /// на две сущности значило бы писать два пула, два UI и две ветки
    /// применения.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UpgradeConfig",
        menuName = "Gladiator Game/Upgrades/Upgrade")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Stat bonuses")]
        [SerializeField] private StatModifier[] _modifiers;

        [Tooltip("None — бонус получают все классы.")]
        [SerializeField] private UnitClassId _targetClass = UnitClassId.None;

        [Header("Squad reinforcement")]
        [SerializeField] private UnitConfig _unitToAdd;
        [SerializeField, Min(0)] private int _unitAddCount;

        [Header("Draft")]
        [SerializeField, Min(1)] private int _weight = 1;

        public string Id => _id;
        public Sprite Icon => _icon;
        public UnitClassId TargetClass => _targetClass;
        public UnitConfig UnitToAdd => _unitToAdd;
        public int UnitAddCount => _unitAddCount;
        public int Weight => _weight;

        public IReadOnlyList<StatModifier> Modifiers =>
            _modifiers ?? System.Array.Empty<StatModifier>();

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

        public string Description => _description;

        public bool AddsUnits =>
            _unitToAdd != null && _unitAddCount > 0;

        /// <summary>
        /// Действует ли улучшение на юнит этого класса.
        /// </summary>
        public bool AppliesTo(UnitClassId classId)
        {
            return _targetClass == UnitClassId.None ||
                   _targetClass == classId;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"UpgradeConfig '{name}' has an empty ID.",
                    this);
            }

            bool hasModifiers =
                _modifiers != null && _modifiers.Length > 0;

            if (!hasModifiers && !AddsUnits)
            {
                Debug.LogWarning(
                    $"UpgradeConfig '{name}' ничего не делает: " +
                    $"ни модификаторов, ни пополнения отряда.",
                    this);
            }
        }
#endif
    }
}
