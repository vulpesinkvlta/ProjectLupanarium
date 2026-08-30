using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Замах и удар.
    ///
    /// Удар больше не мгновенный: боец сначала заносит оружие
    /// (UnitState.WindingUp) и бьёт только когда замах доигран. Это даёт
    /// анимации и звуку время сработать до того, как цель получит урон,
    /// и делает бой читаемым — видно, кто сейчас ударит.
    ///
    /// Нулевой замах в конфиге возвращает прежнее мгновенное поведение,
    /// поэтому старые ассеты продолжают работать.
    /// </summary>
    public sealed class AttackSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Attack");

        private readonly ArenaContext _context;
        private readonly DamageBuffer _damageBuffer;
        private readonly BattleRandom _random;

        public int AttackAttemptsLastTick { get; private set; }
        public int AttacksPerformedLastTick { get; private set; }
        public int WindupsStartedLastTick { get; private set; }
        public int CritsLastTick { get; private set; }

        public AttackSystem(
            ArenaContext context,
            DamageBuffer damageBuffer,
            BattleRandom random)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _damageBuffer = damageBuffer ??
                throw new ArgumentNullException(nameof(damageBuffer));

            _random = random ??
                throw new ArgumentNullException(nameof(random));
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            using (TickMarker.Auto())
            {
                AttackAttemptsLastTick = 0;
                AttacksPerformedLastTick = 0;
                WindupsStartedLastTick = 0;
                CritsLastTick = 0;

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime attacker = units[i];

                    if (!attacker.IsAlive)
                        continue;

                    ProcessUnit(attacker, deltaTime);
                }
            }
        }

        private void ProcessUnit(UnitRuntime attacker, float deltaTime)
        {
            // Оглушённый роняет замах и в этом тике ничего не делает.
            if (attacker.IsStunned)
            {
                attacker.CancelWindup();
                return;
            }

            UnitRuntime target = attacker.Target;

            if (attacker.IsWindingUp)
            {
                ContinueWindup(attacker, target, deltaTime);
                return;
            }

            if (!IsTargetValid(attacker, target))
                return;

            AttackAttemptsLastTick++;

            if (!attacker.CanAttack)
                return;

            if (!IsTargetInAttackRange(attacker, target))
                return;

            BeginAttack(attacker);
        }

        private void BeginAttack(UnitRuntime attacker)
        {
            float windup = attacker.Stats.AttackWindup;

            attacker.BeginWindup(windup);
            WindupsStartedLastTick++;

            // Замаха нет — бьём тем же тиком, чтобы у мгновенных ударов
            // не появилось искусственной задержки в один тик.
            if (windup <= 0f)
                Strike(attacker);
        }

        private void ContinueWindup(
            UnitRuntime attacker,
            UnitRuntime target,
            float deltaTime)
        {
            // Цель умерла или сменилась, пока шёл замах — удар в пустоту.
            if (!IsTargetValid(attacker, target))
            {
                attacker.CancelWindup();
                attacker.SetState(UnitState.Idle);

                return;
            }

            attacker.TickWindup(deltaTime);

            if (attacker.IsWindingUp)
                return;

            // Пока заносили оружие, цель могла отойти.
            if (!IsTargetInAttackRange(attacker, target))
            {
                attacker.SetState(UnitState.Idle);
                return;
            }

            Strike(attacker);
        }

        private void Strike(UnitRuntime attacker)
        {
            UnitRuntime target = attacker.Target;

            if (!IsTargetValid(attacker, target))
            {
                attacker.CancelWindup();
                return;
            }

            bool isCrit = _random.Roll(attacker.Stats.CritChance);

            float amount = isCrit
                ? attacker.Stats.AttackDamage * attacker.Stats.CritMultiplier
                : attacker.Stats.AttackDamage;

            if (amount > 0f)
            {
                _damageBuffer.Add(
                    new DamageRequest(
                        source: attacker,
                        target: target,
                        amount: amount,
                        isCrit: isCrit));
            }

            attacker.StartAttackCooldown();
            attacker.SetState(UnitState.Attacking);

            AttacksPerformedLastTick++;

            if (isCrit)
                CritsLastTick++;
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
