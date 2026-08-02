using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class AttackSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Attack");

        private readonly ArenaContext _context;
        private readonly DamageBuffer _damageBuffer;

        public int AttackAttemptsLastTick { get; private set; }
        public int AttacksPerformedLastTick { get; private set; }

        public AttackSystem(
            ArenaContext context,
            DamageBuffer damageBuffer)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _damageBuffer = damageBuffer ??
                throw new ArgumentNullException(nameof(damageBuffer));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                AttackAttemptsLastTick = 0;
                AttacksPerformedLastTick = 0;

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime attacker = units[i];

                    if (!attacker.IsAlive)
                        continue;

                    UnitRuntime target = attacker.Target;

                    if (!IsTargetValid(attacker, target))
                        continue;

                    AttackAttemptsLastTick++;

                    if (!attacker.CanAttack)
                        continue;

                    if (!IsTargetInAttackRange(
                            attacker,
                            target))
                    {
                        continue;
                    }

                    _damageBuffer.Add(
                        new DamageRequest(
                            source: attacker,
                            target: target,
                            amount: attacker.Stats.AttackDamage));

                    attacker.StartAttackCooldown();
                    attacker.SetState(UnitState.Attacking);

                    AttacksPerformedLastTick++;
                }
            }
        }

        private static bool IsTargetInAttackRange(
            UnitRuntime attacker,
            UnitRuntime target)
        {
            Vector2 offset =
                target.Position -
                attacker.Position;

            float attackRange =
                attacker.Stats.AttackRange;

            return offset.sqrMagnitude <=
                   attackRange * attackRange;
        }

        private static bool IsTargetValid(
            UnitRuntime attacker,
            UnitRuntime target)
        {
            return target != null &&
                   target.IsAlive &&
                   target != attacker &&
                   target.Team != attacker.Team;
        }
    }
}
