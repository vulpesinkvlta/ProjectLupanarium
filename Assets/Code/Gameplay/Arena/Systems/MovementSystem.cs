using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class MovementSystem
    {
        private const int MinimumBufferCapacity = 16;

        /// <summary>Ближе этого расстояния слот считается занятым.</summary>
        private const float SlotArrivalTolerance = 0.05f;

        private readonly ArenaContext _context;
        private readonly FormationRegistry _formationRegistry;

        private Vector2[] _nextPositions =
            Array.Empty<Vector2>();

        private UnitState[] _nextStates =
            Array.Empty<UnitState>();

        public int MovingUnitsLastTick { get; private set; }
        public int UnitsInAttackRangeLastTick { get; private set; }
        public int UnitsMarchingInFormationLastTick { get; private set; }

        private static readonly ProfilerMarker TickMarker =
            new("Arena.Movement");

        public MovementSystem(
            ArenaContext context,
            FormationRegistry formationRegistry)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _formationRegistry = formationRegistry ??
                throw new ArgumentNullException(
                    nameof(formationRegistry));
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime));
            }

            using (TickMarker.Auto())
            {
                var units = _context.AllUnits;

                EnsureBufferCapacity(units.Count);

                MovingUnitsLastTick = 0;
                UnitsInAttackRangeLastTick = 0;
                UnitsMarchingInFormationLastTick = 0;

                CalculateNextPositions(
                    units,
                    deltaTime);

                ApplyNextPositions(units);
            }
        }

        private void CalculateNextPositions(
            IReadOnlyList<UnitRuntime> units,
            float deltaTime)
        {
            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                _nextPositions[i] = unit.Position;
                _nextStates[i] = unit.State;

                if (!unit.IsAlive)
                    continue;

                // Оглушённый стоит на месте: позиция уже записана выше,
                // остаётся только показать состояние.
                if (unit.IsStunned)
                {
                    _nextStates[i] = UnitState.Stunned;
                    continue;
                }

                UnitRuntime target = unit.Target;

                bool hasValidTarget = IsTargetValid(unit, target);

                if (!hasValidTarget)
                    unit.ClearTarget();

                if (unit.HoldsFormation &&
                    TryMarchInFormation(
                        unit,
                        target,
                        hasValidTarget,
                        i,
                        deltaTime))
                {
                    continue;
                }

                if (!hasValidTarget)
                {
                    _nextStates[i] = UnitState.Idle;
                    continue;
                }

                ChaseTarget(unit, target, i, deltaTime);
            }
        }

        /// <summary>
        /// Ведёт бойца к его слоту в наступающем строю.
        /// Возвращает false, если строй распался или боец вступил в бой —
        /// тогда работает обычное преследование.
        /// </summary>
        private bool TryMarchInFormation(
            UnitRuntime unit,
            UnitRuntime target,
            bool hasValidTarget,
            int index,
            float deltaTime)
        {
            TeamFormationState formation =
                _formationRegistry.Get(unit.Team);

            if (!formation.IsActive)
            {
                unit.BreakFormation();
                return false;
            }

            if (hasValidTarget &&
                ShouldBreakFormation(unit, target, formation))
            {
                unit.BreakFormation();
                return false;
            }

            Vector2 slotPosition =
                formation.SlotToWorld(unit.SlotOffset);

            Vector2 offset = slotPosition - unit.Position;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance <=
                SlotArrivalTolerance * SlotArrivalTolerance)
            {
                _nextStates[index] = UnitState.Idle;
                UnitsMarchingInFormationLastTick++;

                return true;
            }

            float distance = Mathf.Sqrt(sqrDistance);

            float movementDistance = Mathf.Min(
                unit.EffectiveMoveSpeed * deltaTime,
                distance);

            _nextPositions[index] =
                unit.Position +
                offset / distance * movementDistance;

            _nextStates[index] = UnitState.Moving;

            MovingUnitsLastTick++;
            UnitsMarchingInFormationLastTick++;

            return true;
        }

        private static bool ShouldBreakFormation(
            UnitRuntime unit,
            UnitRuntime target,
            TeamFormationState formation)
        {
            float sqrDistance =
                (target.Position - unit.Position).sqrMagnitude;

            float breakRange = formation.BreakRange;

            if (sqrDistance <= breakRange * breakRange)
                return true;

            // Страховка на случай, если дизайнер выставит BreakRange
            // меньше дальности атаки: боец, способный ударить, обязан
            // выйти из строя, иначе он будет маршировать мимо врага.
            float attackRange = unit.Stats.AttackRange;

            return sqrDistance <= attackRange * attackRange;
        }

        private void ChaseTarget(
            UnitRuntime unit,
            UnitRuntime target,
            int index,
            float deltaTime)
        {
            Vector2 offset =
                target.Position - unit.Position;

            float sqrDistance =
                offset.sqrMagnitude;

            float attackRange =
                unit.Stats.AttackRange;

            float sqrAttackRange =
                attackRange * attackRange;

            if (sqrDistance <= sqrAttackRange)
            {
                _nextStates[index] =
                    UnitState.Attacking;

                UnitsInAttackRangeLastTick++;
                return;
            }

            float distance =
                Mathf.Sqrt(sqrDistance);

            if (distance <= Mathf.Epsilon)
            {
                _nextStates[index] =
                    UnitState.Attacking;

                UnitsInAttackRangeLastTick++;
                return;
            }

            float distanceUntilAttackRange =
                distance - attackRange;

            float maximumMovement =
                unit.EffectiveMoveSpeed * deltaTime;

            float movementDistance =
                Mathf.Min(
                    maximumMovement,
                    distanceUntilAttackRange);

            if (movementDistance <= 0f)
            {
                _nextStates[index] =
                    UnitState.Attacking;

                UnitsInAttackRangeLastTick++;
                return;
            }

            Vector2 direction =
                offset / distance;

            _nextPositions[index] =
                unit.Position +
                direction * movementDistance;

            _nextStates[index] =
                UnitState.Moving;

            MovingUnitsLastTick++;
        }

        private void ApplyNextPositions(
            IReadOnlyList<UnitRuntime> units)
        {
            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                if (!unit.IsAlive)
                    continue;

                unit.SetPosition(
                    _nextPositions[i]);

                unit.SetState(
                    _nextStates[i]);
            }
        }

        private void EnsureBufferCapacity(
            int requiredCapacity)
        {
            if (_nextPositions.Length >= requiredCapacity)
                return;

            int capacity = Mathf.NextPowerOfTwo(
                Mathf.Max(
                    requiredCapacity,
                    MinimumBufferCapacity));

            Array.Resize(
                ref _nextPositions,
                capacity);

            Array.Resize(
                ref _nextStates,
                capacity);
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
