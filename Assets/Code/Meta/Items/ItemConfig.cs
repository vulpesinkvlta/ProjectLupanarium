using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public enum EquipmentSlot : byte
    {
        Weapon = 0,
        Armor = 1,
        Trinket = 2
    }

    /// <summary>
    /// Предмет снаряжения. Покупается за денарии и надевается на класс
    /// гладиаторов: в один слот одного класса влезает один предмет.
    ///
    /// Никакой новой механики — те же StatModifier, что у улучшений
    /// и построек. Отличается только тем, как приобретается и снимается.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemConfig",
        menuName = "Gladiator Game/Items/Item")]
    public sealed class ItemConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Equipment")]
        [SerializeField] private EquipmentSlot _slot = EquipmentSlot.Weapon;

        [Tooltip("None — предмет подходит гладиаторам любого класса.")]
        [SerializeField] private UnitClassId _targetClass = UnitClassId.None;

        [Header("Economy")]
        [SerializeField, Min(0)] private int _price = 100;

        [Header("Bonuses")]
        [SerializeField] private StatModifier[] _modifiers;

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Description => _description;
        public EquipmentSlot Slot => _slot;
        public UnitClassId TargetClass => _targetClass;
        public int Price => _price;

        public IReadOnlyList<StatModifier> Modifiers =>
            _modifiers ?? System.Array.Empty<StatModifier>();

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

        public bool AppliesTo(UnitClassId classId)
        {
            return _targetClass == UnitClassId.None ||
                   _targetClass == classId;
        }

        /// <summary>
        /// Ключ ячейки снаряжения: класс плюс слот. Хранится строкой,
        /// потому что в сохранение уедет именно строка.
        /// </summary>
        public string SlotKey => BuildSlotKey(_targetClass, _slot);

        public static string BuildSlotKey(
            UnitClassId targetClass,
            EquipmentSlot slot)
        {
            return $"{targetClass}:{slot}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"ItemConfig '{name}' has an empty ID.",
                    this);
            }
        }
#endif
    }
}
