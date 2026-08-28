using System;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Сколько юнитов одного типа входит в отряд.
    /// Используется и в ассетах (состав волны, стартовый отряд),
    /// и в рантайме внутри RunState.
    /// </summary>
    [Serializable]
    public sealed class SquadEntry
    {
        [SerializeField] private UnitConfig _config;
        [SerializeField, Min(1)] private int _count = 1;

        public UnitConfig Config => _config;
        public int Count => _count;

        public SquadEntry(UnitConfig config, int count)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Squad entry count must be greater than zero.");
            }

            _config = config;
            _count = count;
        }

        public void AddCount(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _count += amount;
        }

        public void SetCount(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _count = count;
        }

        /// <summary>
        /// Копия для рантайма. Состав из ассета копировать обязательно:
        /// RunState меняет количество юнитов по ходу забега, а правка
        /// массива внутри ScriptableObject пережила бы выход из Play Mode
        /// и испортила исходные данные.
        /// </summary>
        public SquadEntry Clone()
        {
            return new SquadEntry(_config, _count);
        }
    }
}
