namespace Code.Gameplay
{
    public enum StatId
    {
        Unknown = 0,
        MaxHealth = 1,
        MoveSpeed = 2,
        AttackDamage = 3,
        AttackRange = 4,
        AttackCooldown = 5,
        Radius = 6,
        EngagementCapacity = 7,  
        Armor = 8,
        CritChance = 9,

        /// <summary>Замах: сколько секунд боец заносит оружие до удара.</summary>
        AttackWindup = 10,

        /// <summary>Во сколько раз критический удар сильнее обычного.</summary>
        CritMultiplier = 11,

        Count = 12
    }
}
