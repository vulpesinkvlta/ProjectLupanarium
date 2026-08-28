using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitStatsBuilder
    {
        private const int StatCount = (int)StatId.Count;

        private readonly float[] _base = new float[StatCount];
        private readonly float[] _flat = new float[StatCount];
        private readonly float[] _percentAdd = new float[StatCount];
        private readonly float[] _percentMul = new float[StatCount];
        private readonly float[] _values = new float[StatCount];
        public UnitStats Build(UnitConfig config, ModifierSet modifiers)
        {
            if(config == null) 
                throw new ArgumentNullException(nameof(config));

            if(modifiers == null) 
                throw new ArgumentNullException(nameof(modifiers));
            Array.Clear(_flat, 0 , StatCount);
            Array.Clear(_percentAdd, 0 , StatCount);
            Array.Fill(_percentMul, 1f);
            Array.Clear(_base, 0 , StatCount);
            config.WriteBaseStats(_base);

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                int statIndex = (int)modifier.StatId;
                switch (modifier.ModType)
                {
                    case ModType.Flat:
                        _flat[statIndex] += modifier.Value;
                        break;
                    case ModType.PercentAdd:
                        _percentAdd[statIndex] += modifier.Value;
                        break;
                    case ModType.PercentMul:
                        _percentMul[statIndex] *= 1f + modifier.Value;
                        break;
                }
            }

            for (int i = 1; i < StatCount; i++)
            {
                float value = (_base[i] + _flat[i]) * (1f + _percentAdd[i]) * _percentMul[i];
                _values[i] = StatClamps.Apply((StatId)i, value);
            }

            return new UnitStats(maxHealth: _values[(int)StatId.MaxHealth],
                    moveSpeed: _values[(int)StatId.MoveSpeed],
                    attackDamage: _values[(int)StatId.AttackDamage],
                    attackRange: _values[(int)StatId.AttackRange],
                    attackCooldown: _values[(int)StatId.AttackCooldown],
                    radius: _values[(int)StatId.Radius],
                    engagementCapacity: Mathf.RoundToInt(_values[(int)StatId.EngagementCapacity]),
                    armor: _values[(int)StatId.Armor],
                    critChance: _values[(int)StatId.CritChance]);
        }
    }
}
