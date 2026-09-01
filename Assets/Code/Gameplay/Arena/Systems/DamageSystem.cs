using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Применяет накопленный за тик урон с учётом брони и сообщает
    /// слою представления, куда и на сколько ударили.
    /// </summary>
    public sealed class DamageSystem
    {
        /// <summary>
        /// Броня не может свести урон в ноль — иначе два толстых отряда
        /// встали бы друг напротив друга навсегда, и бой не кончился бы.
        /// </summary>
        private const float MinimumDamage = 1f;

        private static readonly ProfilerMarker TickMarker =
            new("Arena.Damage");

        private readonly DamageBuffer _damageBuffer;
        private readonly BattleHudDirtyTracker _hudDirtyTracker;
        private readonly BattleFeedbackQueue _feedback;
        private readonly BattleStatistics _statistics;

        public int RequestsProcessedLastTick { get; private set; }
        public float DamageAppliedLastTick { get; private set; }
        public float DamageBlockedLastTick { get; private set; }

        public DamageSystem(
            DamageBuffer damageBuffer,
            BattleHudDirtyTracker hudDirtyTracker,
            BattleFeedbackQueue feedback,
            BattleStatistics statistics)
        {
            _damageBuffer = damageBuffer ??
                throw new ArgumentNullException(nameof(damageBuffer));

            _hudDirtyTracker = hudDirtyTracker ??
                throw new ArgumentNullException(nameof(hudDirtyTracker));

            _feedback = feedback ??
                throw new ArgumentNullException(nameof(feedback));

            _statistics = statistics ??
                throw new ArgumentNullException(nameof(statistics));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                RequestsProcessedLastTick = 0;
                DamageAppliedLastTick = 0f;
                DamageBlockedLastTick = 0f;

                var requests = _damageBuffer.Requests;

                for (var i = 0; i < requests.Count; i++)
                {
                    DamageRequest request = requests[i];

                    if (request.Target.State == UnitState.Dead)
                        continue;

                    ApplyRequest(request);
                }

                _damageBuffer.Clear();

                if (DamageAppliedLastTick > 0f)
                    _hudDirtyTracker.MarkHealthChanged();
            }
        }

        private void ApplyRequest(DamageRequest request)
        {
            UnitRuntime target = request.Target;

            float armor = target.Stats.Armor;

            float mitigated = Mathf.Max(
                MinimumDamage,
                request.Amount - armor);

            DamageBlockedLastTick += request.Amount - mitigated;

            float healthBefore = target.CurrentHealth;

            target.ApplyDamage(mitigated);

            float appliedDamage =
                healthBefore - target.CurrentHealth;

            DamageAppliedLastTick += appliedDamage;
            RequestsProcessedLastTick++;

            target.RecordDamageSource(request.Source);

            _statistics.RegisterDamage(
                request.Source,
                target,
                appliedDamage,
                request.Amount - mitigated);

            if (appliedDamage <= 0f)
                return;

            _feedback.Push(
                request.IsCrit
                    ? BattleFeedbackKind.Crit
                    : BattleFeedbackKind.Hit,
                target.Id,
                target.Position,
                appliedDamage);
        }
    }
}
