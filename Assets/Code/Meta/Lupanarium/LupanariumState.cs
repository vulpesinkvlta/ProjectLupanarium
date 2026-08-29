using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Постоянное состояние школы: денарии и уровни построек.
    ///
    /// Переживает забеги — в отличие от RunState, который создаётся заново
    /// на каждый выход на арену. Уровни хранятся по строковому id, а не по
    /// ссылке на ScriptableObject: в Фазе 5 это уедет в JSON, а ссылку
    /// на ассет сериализовать нечем.
    /// </summary>
    public sealed class LupanariumState
    {
        private const int InitialBuildingCapacity = 16;

        private readonly Dictionary<string, int> _levels =
            new(InitialBuildingCapacity);

        public int Denarii { get; private set; }

        /// <summary>Уровни построек изменились — UI перерисоваться.</summary>
        public event Action Changed;

        public int GetLevel(SchoolBuildingConfig building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            return _levels.TryGetValue(building.Id, out int level)
                ? level
                : 0;
        }

        public bool IsMaxLevel(SchoolBuildingConfig building)
        {
            return GetLevel(building) >= building.MaxLevel;
        }

        public bool CanUpgrade(SchoolBuildingConfig building)
        {
            if (!building.TryGetUpgradeCost(GetLevel(building), out int cost))
                return false;

            return Denarii >= cost;
        }

        public bool TryUpgrade(SchoolBuildingConfig building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            int level = GetLevel(building);

            if (!building.TryGetUpgradeCost(level, out int cost))
                return false;

            if (Denarii < cost)
                return false;

            Denarii -= cost;
            _levels[building.Id] = level + 1;

            Changed?.Invoke();
            return true;
        }

        public void AddDenarii(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (amount == 0)
                return;

            Denarii += amount;
            Changed?.Invoke();
        }

        /// <summary>Полный сброс прогресса школы.</summary>
        public void Reset()
        {
            _levels.Clear();
            Denarii = 0;

            Changed?.Invoke();
        }
    }
}
