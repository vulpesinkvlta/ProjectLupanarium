using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Применяет способности бойцов по готовности кулдауна.
    ///
    /// Никакого выбора «когда лучше» — автобаттлер: способность
    /// срабатывает при первой возможности. Вся тактика игрока задаётся
    /// составом отряда и строем, а не микроконтролем в бою.
    /// </summary>
    public sealed class AbilitySystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Abilities");

        private readonly ArenaContext _context;
        private readonly BattleFeedbackQueue _feedback;

        public int AbilitiesUsedLastTick { get; private set; }

        public AbilitySystem(
            ArenaContext context,
            BattleFeedbackQueue feedback)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _feedback = feedback ??
                throw new ArgumentNullException(nameof(feedback));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                AbilitiesUsedLastTick = 0;

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    TryUseAbility(unit);
                }
            }
        }

        private void TryUseAbility(UnitRuntime unit)
        {
            AbilitySpec spec = unit.Ability;

            if (!spec.IsValid)
                return;

            if (!unit.CanUseAbility)
                return;

            if (spec.RequiresTarget)
            {
                UnitRuntime target = unit.Target;

                if (!IsTargetValid(unit, target))
                    return;

                if (!IsInRange(unit, target, spec.Range))
                    return;

                ApplyToTarget(spec, target);
            }
            else
            {
                ApplyToSelf(unit, spec);
            }

            unit.StartAbilityCooldown(spec.Cooldown);
            AbilitiesUsedLastTick++;

            _feedback.Push(
                BattleFeedbackKind.Ability,
                unit.Id,
                unit.Position,
                (float)spec.Effect);
        }

        private static void ApplyToTarget(
            AbilitySpec spec,
            UnitRuntime target)
        {
            switch (spec.Effect)
            {
                case AbilityEffect.SlowTarget:
                    target.ApplySlow(spec.Duration, spec.Magnitude);
                    break;

                case AbilityEffect.StunTarget:
                    target.ApplyStun(spec.Duration);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(spec),
                        spec.Effect,
                        "Эффект не применяется к цели.");
            }
        }

        private static void ApplyToSelf(
            UnitRuntime unit,
            AbilitySpec spec)
        {
            switch (spec.Effect)
            {
                case AbilityEffect.HasteSelf:
                    unit.ApplyHaste(spec.Duration, spec.Magnitude);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(spec),
                        spec.Effect,
                        "Эффект не применяется к себе.");
            }
        }

        private static bool IsInRange(
            UnitRuntime unit,
            UnitRuntime target,
            float range)
        {
            Vector2 offset = target.Position - unit.Position;

            return offset.sqrMagnitude <= range * range;
        }

        private static bool IsTargetValid(
            UnitRuntime owner,
            UnitRuntime target)
        {
            return target != null &&
                   target.IsAlive &&
                   target != owner &&
                   target.Team != owner.Team;
        }
    }
}
