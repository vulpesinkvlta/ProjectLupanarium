using System;
using UnityEngine;

namespace Code.Gameplay
{
    public readonly struct UnitStats
    {
        public float MaxHealth { get; }
        public float MoveSpeed { get; }

        public float AttackDamage { get; }
        public float AttackRange { get; }
        public float AttackCooldown { get; }

        public float Radius { get; }
        public int EngagementCapacity { get; }

        public UnitStats(
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float radius,
            int engagementCapacity)
        {
            if (maxHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth));
            }

            if (moveSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(moveSpeed));
            }

            if (attackDamage < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackDamage));
            }

            if (attackRange < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackRange));
            }

            if (attackCooldown <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackCooldown));
            }

            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius));
            }

            if (engagementCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(engagementCapacity));
            }

            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;

            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;

            Radius = radius;
            EngagementCapacity = engagementCapacity;
        }
    }
}
