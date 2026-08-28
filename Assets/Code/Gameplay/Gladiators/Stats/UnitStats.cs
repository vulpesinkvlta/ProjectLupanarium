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

        public UnitStats(
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float radius,
            int engagementCapacity,
            float armor,
            float critChance)
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

            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            Radius = radius;
            EngagementCapacity = engagementCapacity;
            Armor = armor;
            CritChance = critChance;
        }
    }
}
