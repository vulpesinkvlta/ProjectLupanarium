using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public class MovementSystem
    {
        private const int MinimumBufferCapacity = 16;

        private readonly ArenaContext _context;

        private Vector2[] _nextPositions =
            Array.Empty<Vector2>();

        private UnitState[] _nextStates =
            Array.Empty<UnitState>();

        public int MovingUnitsLastTick { get; private set; }
        public int UnitsInAttackRangeLastTick { get; private set; }

        private static readonly ProfilerMarker TickMarker =
            new("Arena.Movement");

        public MovementSystem(ArenaContext context)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));
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

                CalculateNextPositions(
                    units,
                    deltaTime);

                ApplyNextPositions(units);
            }
        }

        private void CalculateNextPositions(
            System.Collections.Generic.IReadOnlyList<UnitRuntime> units,
            float deltaTime)
        {
            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                _nextPositions[i] = unit.Position;
                _nextStates[i] = unit.State;

                if (!unit.IsAlive)
                    continue;

                UnitRuntime target = unit.Target;

                if (!IsTargetValid(unit, target))
                {
                    unit.ClearTarget();
                    _nextStates[i] = UnitState.Idle;
                    continue;
                }

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
                    _nextStates[i] =
                        UnitState.Attacking;

                    UnitsInAttackRangeLastTick++;
                    continue;
                }

                float distance =
                    Mathf.Sqrt(sqrDistance);

                if (distance <= Mathf.Epsilon)
                {
                    _nextStates[i] =
                        UnitState.Attacking;

                    UnitsInAttackRangeLastTick++;
                    continue;
                }

                float distanceUntilAttackRange =
                    distance - attackRange;

                float maximumMovement =
                    unit.Stats.MoveSpeed * deltaTime;

                float movementDistance =
                    Mathf.Min(
                        maximumMovement,
                        distanceUntilAttackRange);

                if (movementDistance <= 0f)
                {
                    _nextStates[i] =
                        UnitState.Attacking;

                    UnitsInAttackRangeLastTick++;
                    continue;
                }

                Vector2 direction =
                    offset / distance;

                _nextPositions[i] =
                    unit.Position +
                    direction * movementDistance;

                _nextStates[i] =
                    UnitState.Moving;

                MovingUnitsLastTick++;
            }
        }

        private void ApplyNextPositions(
            System.Collections.Generic.IReadOnlyList<UnitRuntime> units)
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
