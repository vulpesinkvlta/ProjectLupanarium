using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class DamageSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Damage");

        private readonly DamageBuffer _damageBuffer;

        public int RequestsProcessedLastTick { get; private set; }
        public float DamageAppliedLastTick { get; private set; }

        public DamageSystem(DamageBuffer damageBuffer)
        {
            _damageBuffer = damageBuffer ??
                throw new ArgumentNullException(nameof(damageBuffer));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                RequestsProcessedLastTick = 0;
                DamageAppliedLastTick = 0f;

                var requests =
                    _damageBuffer.Requests;

                for (var i = 0; i < requests.Count; i++)
                {
                    DamageRequest request =
                        requests[i];

                    if (request.Target.State ==
                        UnitState.Dead)
                    {
                        continue;
                    }

                    float healthBefore =
                        request.Target.CurrentHealth;

                    request.Target.ApplyDamage(
                        request.Amount);

                    float appliedDamage =
                        healthBefore -
                        request.Target.CurrentHealth;

                    DamageAppliedLastTick +=
                        appliedDamage;

                    RequestsProcessedLastTick++;
                }

                _damageBuffer.Clear();
            }
        }
    }
}
