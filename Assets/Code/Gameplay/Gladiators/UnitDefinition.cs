using System;
using UnityEngine;

namespace Code.Gameplay
{
    public readonly struct UnitDefinition
    {
        public string ConfigId { get; }
        public UnitClassId ClassId { get; }
        public UnitStats Stats { get; }
        public int GoldReward { get; }

        /// <summary>Способность класса. Может быть пустой.</summary>
        public AbilitySpec Ability { get; }

        public UnitDefinition(
            string configId,
            UnitClassId classId,
            UnitStats stats,
            int goldReward,
            AbilitySpec ability)
        {
            if (string.IsNullOrWhiteSpace(configId))
            {
                throw new ArgumentException(
                    "Unit config ID cannot be empty.",
                    nameof(configId));
            }

            if (classId == UnitClassId.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(classId),
                    "Unit class cannot be None.");
            }

            ConfigId = configId;
            ClassId = classId;
            Stats = stats;
            GoldReward = goldReward < 0 ? 0 : goldReward;
            Ability = ability;
        }
    }
}
