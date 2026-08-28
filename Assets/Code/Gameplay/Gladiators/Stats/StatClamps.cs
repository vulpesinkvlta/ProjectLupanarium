using System;

namespace Code.Gameplay
{
    public static class StatClamps
    {
        public static float Apply(StatId id, float value)
        {
            switch (id)
            {
                case StatId.MaxHealth:
                    return Math.Clamp(value, 1f, float.MaxValue);
                case StatId.MoveSpeed:
                    return Math.Clamp(value, 0f, float.MaxValue);
                case StatId.AttackDamage:
                    return Math.Clamp(value, 0f, float.MaxValue);
                case StatId.AttackRange:
                    return Math.Clamp(value, 0f, float.MaxValue);
                case StatId.AttackCooldown:
                    return Math.Clamp(value, 0.05f, float.MaxValue);
                case StatId.Radius:
                    return Math.Clamp(value, 0.05f, float.MaxValue);
                case StatId.EngagementCapacity:
                    return Math.Clamp(value, 1f, float.MaxValue);
                case StatId.Armor:
                    return Math.Clamp(value, 0f, float.MaxValue);
                case StatId.CritChance:
                    return Math.Clamp(value, 0f, 1f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }
    }
}
