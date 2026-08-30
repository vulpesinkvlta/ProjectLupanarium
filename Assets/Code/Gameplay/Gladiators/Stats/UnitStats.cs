using System;
using System.Collections.Generic;
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
        public float EngagementCapacity { get; }
        public float Armor { get; } 

        public float CritChance { get; }

        /// <summary>Секунды замаха до удара. Ноль — удар мгновенный.</summary>
        public float AttackWindup { get; }

        /// <summary>Множитель урона при критическом ударе.</summary>
        public float CritMultiplier { get; }

        public UnitStats(
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float radius,
            int engagementCapacity,
            float armor,
            float critChance,
            float attackWindup,
            float critMultiplier)
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

            if (armor < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(armor));
            }
            
            if (critChance < 0f || critChance > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(critChance));
            }

            if (attackWindup < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackWindup));
            }

            if (critMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(critMultiplier));
            }

            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            Radius = radius;
            EngagementCapacity = engagementCapacity;
            Armor = armor;
            CritChance = critChance;
            AttackWindup = attackWindup;
            CritMultiplier = critMultiplier;
        }
    }
}
