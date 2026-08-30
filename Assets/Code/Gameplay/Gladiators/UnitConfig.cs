using System;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(
          fileName = "UnitConfig",
          menuName = "Gladiator Game/Units/Unit Config")]
    public sealed class UnitConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private UnitClassId _classId;

        [Header("Health and Movement")]
        [SerializeField, Min(1f)]
        private float _maxHealth = 100f;

        [SerializeField, Min(0f)]
        private float _moveSpeed = 3f;

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float _attackDamage = 10f;

        [SerializeField, Min(0f)]
        private float _attackRange = 0.8f;

        [SerializeField, Min(0.01f)]
        private float _attackCooldown = 1f;

        [Tooltip("Секунды замаха до удара. Ноль — бьёт мгновенно.")]
        [SerializeField, Min(0f)]
        private float _attackWindup = 0.25f;

        [SerializeField, Min(1f)]
        private float _critMultiplier = 2f;

        [Header("Body")]
        [SerializeField, Min(0.05f)]
        private float _radius = 0.35f;

        [SerializeField, Min(1)]
        private int _engagementCapacity = 4;

        [Header("Defence")]
        [SerializeField, Min(0f)] private float _armor = 0f;
        [SerializeField, Range(0f, 1f)] private float _critChance = 0f;
        [Header("Ability")]
        [Tooltip("Необязательно: способность класса.")]
        [SerializeField] private AbilityConfig _ability;

        [Header("Reward")]
        [Tooltip("Сколько золота даёт убийство этого юнита.")]
        [SerializeField, Min(0)] private int _goldReward = 1;

        public string Id => _id;
        public int GoldReward => _goldReward;

        /// <summary>
        /// Способность в виде, не зависящем от ассета. Пустая, если
        /// способность не назначена — симуляция обрабатывает оба случая
        /// одинаково и не проверяет ссылки на null.
        /// </summary>
        public AbilitySpec Ability =>
            _ability != null
                ? _ability.Spec
                : AbilitySpec.None;
        public UnitClassId ClassId => _classId;

        public void WriteBaseStats(float[] destination)
        {
            Validate();
            destination[(int)StatId.MaxHealth] = _maxHealth;
            destination[(int)StatId.MoveSpeed] = _moveSpeed;
            destination[(int)StatId.AttackDamage] = _attackDamage;
            destination[(int)StatId.AttackRange] = _attackRange;
            destination[(int)StatId.AttackCooldown] = _attackCooldown;  
            destination[(int)StatId.Radius] = _radius;
            destination[(int)StatId.EngagementCapacity] = _engagementCapacity;
            destination[(int)StatId.Armor] = _armor;                
            destination[(int)StatId.CritChance] = _critChance;
            destination[(int)StatId.AttackWindup] = _attackWindup;
            destination[(int)StatId.CritMultiplier] = _critMultiplier;              
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                throw new InvalidOperationException(
                    $"UnitConfig '{name}' has an empty ID.");
            }

            if (_classId == UnitClassId.None)
            {
                throw new InvalidOperationException(
                    $"UnitConfig '{name}' has no class.");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"UnitConfig '{name}' has an empty ID.",
                    this);
            }

            if (_classId == UnitClassId.None)
            {
                Debug.LogWarning(
                    $"UnitConfig '{name}' has no class.",
                    this);
            }
        }
#endif
    }
}
