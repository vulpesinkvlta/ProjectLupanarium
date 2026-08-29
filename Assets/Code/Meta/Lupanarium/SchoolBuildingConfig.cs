using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Один уровень постройки: сколько стоит и что даёт.
    /// </summary>
    [Serializable]
    public sealed class SchoolBuildingLevel
    {
        [SerializeField, Min(0)] private int _cost = 100;
        [SerializeField] private StatModifier[] _modifiers;

        public int Cost => _cost;

        public IReadOnlyList<StatModifier> Modifiers =>
            _modifiers ?? Array.Empty<StatModifier>();
    }

    /// <summary>
    /// Постройка гладиаторской школы.
    ///
    /// Бонусы накапливаются: на третьем уровне действуют модификаторы
    /// первого, второго и третьего. Так дизайнеру проще — он пишет прирост
    /// за уровень, а не пересчитывает итог каждый раз заново.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SchoolBuildingConfig",
        menuName = "Gladiator Game/Lupanarium/Building")]
    public sealed class SchoolBuildingConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Effect")]
        [Tooltip("None — бонус получают все классы гладиаторов.")]
        [SerializeField] private UnitClassId _targetClass = UnitClassId.None;

        [SerializeField] private SchoolBuildingLevel[] _levels;

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Description => _description;
        public UnitClassId TargetClass => _targetClass;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

        public int MaxLevel =>
            _levels?.Length ?? 0;

        public bool AppliesTo(UnitClassId classId)
        {
            return _targetClass == UnitClassId.None ||
                   _targetClass == classId;
        }

        /// <summary>
        /// Цена перехода с currentLevel на следующий.
        /// Возвращает false, если постройка уже максимального уровня.
        /// </summary>
        public bool TryGetUpgradeCost(int currentLevel, out int cost)
        {
            cost = 0;

            if (currentLevel < 0 || currentLevel >= MaxLevel)
                return false;

            cost = _levels[currentLevel].Cost;
            return true;
        }

        /// <summary>
        /// Складывает в destination модификаторы всех уровней вплоть
        /// до currentLevel включительно.
        /// </summary>
        public void CollectModifiers(int currentLevel, ModifierSet destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            int levels = Mathf.Clamp(currentLevel, 0, MaxLevel);

            for (var i = 0; i < levels; i++)
                destination.AddRange(_levels[i].Modifiers);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"SchoolBuildingConfig '{name}' has an empty ID.",
                    this);
            }

            if (MaxLevel == 0)
            {
                Debug.LogWarning(
                    $"SchoolBuildingConfig '{name}': не задано ни одного уровня.",
                    this);
            }
        }
#endif
    }
}
