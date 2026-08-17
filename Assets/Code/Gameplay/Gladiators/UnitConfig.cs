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

        [Header("Body")]
        [SerializeField, Min(0.05f)]
        private float _radius = 0.35f;

        [SerializeField, Min(1)]
        private int _engagementCapacity = 4;

        public string Id => _id;
        public UnitClassId ClassId => _classId;

        public UnitDefinition CreateDefinition()
        {
            Validate();

            var stats = new UnitStats(
                maxHealth: _maxHealth,
                moveSpeed: _moveSpeed,
                attackDamage: _attackDamage,
                attackRange: _attackRange,
                attackCooldown: _attackCooldown,
                radius: _radius,
                engagementCapacity: _engagementCapacity);

            return new UnitDefinition(
                configId: _id,
                classId: _classId,
                stats: stats);
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
