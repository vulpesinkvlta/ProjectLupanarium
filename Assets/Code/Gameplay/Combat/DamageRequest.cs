using System;
using UnityEngine;

namespace Code.Gameplay
{
    public readonly struct DamageRequest
    {
        public UnitRuntime Source { get; }
        public UnitRuntime Target { get; }
        public float Amount { get; }
        public bool IsCrit { get; }

        public DamageRequest(
            UnitRuntime source,
            UnitRuntime target,
            float amount,
            bool isCrit)
        {
            Source = source ??
                throw new ArgumentNullException(nameof(source));

            Target = target ??
                throw new ArgumentNullException(nameof(target));

            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Damage amount must be greater than zero.");
            }

            Amount = amount;
            IsCrit = isCrit;
        }
    }
}
