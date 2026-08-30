namespace Code.Gameplay
{
    public enum AbilityEffect : byte
    {
        None = 0,

        /// <summary>Замедляет цель.</summary>
        SlowTarget = 1,

        /// <summary>Оглушает цель.</summary>
        StunTarget = 2,

        /// <summary>Разгоняет самого бойца — рывок.</summary>
        HasteSelf = 3
    }

    /// <summary>
    /// Способность в виде, не зависящем от ассета.
    ///
    /// Тот же приём, что с FormationShape: AbilityConfig остаётся
    /// ScriptableObject для дизайнера, а в симуляцию едет обычная
    /// структура. Благодаря этому AbilitySystem тестируется без Unity.
    /// </summary>
    public readonly struct AbilitySpec
    {
        public AbilityEffect Effect { get; }
        public float Cooldown { get; }
        public float Range { get; }
        public float Duration { get; }
        public float Magnitude { get; }

        public bool IsValid => Effect != AbilityEffect.None;

        /// <summary>Нужна ли способности вражеская цель.</summary>
        public bool RequiresTarget =>
            Effect == AbilityEffect.SlowTarget ||
            Effect == AbilityEffect.StunTarget;

        public AbilitySpec(
            AbilityEffect effect,
            float cooldown,
            float range,
            float duration,
            float magnitude)
        {
            Effect = effect;
            Cooldown = cooldown;
            Range = range;
            Duration = duration;
            Magnitude = magnitude;
        }

        public static AbilitySpec None =>
            new(AbilityEffect.None, 0f, 0f, 0f, 0f);
    }
}
