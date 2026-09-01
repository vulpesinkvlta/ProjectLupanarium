using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class TargetingSystem
    {
        private const float TargetRefreshInterval = 0.25f;
        private const float TargetRefreshJitter = 0.1f;

        private const float EngagementTolerance = 0.15f;
        private const float ReciprocalTargetPriorityRange = 3f;

        private const float TargetSwitchDistanceFactor = 0.75f;

        private const float ClaimDistancePenalty = 0.2f;

        /// <summary>
        /// В каком радиусе боец ищет цель через грид. Заведомо больше
        /// ReciprocalTargetPriorityRange, иначе приоритет «бьёт меня в ответ»
        /// перестал бы работать. Если в радиусе никого — система честно
        /// откатывается к полному перебору.
        /// </summary>
        private const float TargetSearchRadius = 6f;

        private const int InitialCandidateCapacity = 64;

        private static readonly ProfilerMarker TickMarker =
            new("Arena.Targeting");

        private readonly ArenaContext _context;
        private readonly TargetEngagementRegistry
            _engagementRegistry;

        private readonly SpatialGrid _spatialGrid;

        private readonly List<UnitRuntime> _candidateBuffer =
            new(InitialCandidateCapacity);

        public int DistanceChecksLastTick { get; private set; }
        public int AssignedTargetsLastTick { get; private set; }
        public int RetargetedUnitsLastTick { get; private set; }
        public int FullTargetSkipsLastTick { get; private set; }

        /// <summary>
        /// Сколько раз за тик пришлось перебирать всю армию, потому что
        /// в радиусе поиска не нашлось ни одного врага.
        /// </summary>
        public int FullScanFallbacksLastTick { get; private set; }

        public TargetingSystem(
            ArenaContext context,
            TargetEngagementRegistry engagementRegistry,
            SpatialGrid spatialGrid)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _engagementRegistry = engagementRegistry ??
                throw new ArgumentNullException(
                    nameof(engagementRegistry));

            _spatialGrid = spatialGrid ??
                throw new ArgumentNullException(nameof(spatialGrid));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                DistanceChecksLastTick = 0;
                AssignedTargetsLastTick = 0;
                RetargetedUnitsLastTick = 0;
                FullTargetSkipsLastTick = 0;
                FullScanFallbacksLastTick = 0;

                var units = _context.AllUnits;

                _engagementRegistry.Rebuild(units);

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    ProcessUnit(unit);
                }
            }
        }

        public void Reset()
        {
            _engagementRegistry.Clear();

            DistanceChecksLastTick = 0;
            AssignedTargetsLastTick = 0;
            RetargetedUnitsLastTick = 0;
            FullTargetSkipsLastTick = 0;
        }

        private void ProcessUnit(
            UnitRuntime unit)
        {
            UnitRuntime currentTarget =
                unit.Target;

            bool currentTargetIsValid =
                IsTargetValid(
                    unit,
                    currentTarget);

            if (currentTargetIsValid &&
                !unit.IsTargetRefreshReady)
            {
                return;
            }
            if (currentTargetIsValid)
            {
                _engagementRegistry.ReleaseClaim(
                    currentTarget);
            }

            UnitRuntime bestTarget =
                FindBestTarget(
                    unit,
                    out float bestTargetSqrDistance);

            if (bestTarget == null)
            {
                unit.ClearTarget();
                unit.SetState(UnitState.Idle);

                RestartRefreshTimer(unit);
                return;
            }

            bool shouldSwitch =
                !currentTargetIsValid ||
                ShouldSwitchTarget(
                    unit,
                    currentTarget,
                    bestTarget,
                    bestTargetSqrDistance);

            UnitRuntime selectedTarget;

            if (shouldSwitch)
            {
                selectedTarget = bestTarget;

                if (currentTargetIsValid &&
                    currentTarget != bestTarget)
                {
                    RetargetedUnitsLastTick++;
                }

                if (currentTarget != bestTarget)
                {
                    unit.SetTarget(bestTarget);
                    AssignedTargetsLastTick++;
                }
            }
            else
            {
                selectedTarget = currentTarget;
            }

            if (selectedTarget != null &&
                selectedTarget.IsAlive)
            {
                _engagementRegistry.AddClaim(
                    selectedTarget);
            }

            RestartRefreshTimer(unit);
        }

        /// <summary>
        /// Кандидаты в цели: сначала ближние через грид, и только если
        /// рядом никого — вся вражеская армия.
        ///
        /// Полный перебор оставлен намеренно как запасной путь: армии
        /// в начале волны могут стоять дальше радиуса поиска, и без
        /// отката бойцы просто не нашли бы друг друга.
        /// </summary>
        private IReadOnlyList<UnitRuntime> SelectCandidates(
            UnitRuntime searchingUnit)
        {
            TeamId enemyTeam =
                searchingUnit.Team == TeamId.Player
                    ? TeamId.Enemy
                    : TeamId.Player;

            _spatialGrid.QueryTeam(
                searchingUnit.Position,
                TargetSearchRadius,
                enemyTeam,
                _candidateBuffer);

            if (_candidateBuffer.Count > 0)
                return _candidateBuffer;

            FullScanFallbacksLastTick++;

            return enemyTeam == TeamId.Enemy
                ? _context.EnemyUnits
                : _context.PlayerUnits;
        }

        private UnitRuntime FindBestTarget(
            UnitRuntime searchingUnit,
            out float selectedSqrDistance)
        {
            IReadOnlyList<UnitRuntime> possibleTargets =
                SelectCandidates(searchingUnit);

            UnitRuntime nearestFreeEngaged = null;
            float nearestFreeEngagedDistance =
                float.MaxValue;

            UnitRuntime nearestFreeReciprocal = null;
            float nearestFreeReciprocalDistance =
                float.MaxValue;

            UnitRuntime bestFreeTarget = null;
            float bestFreeTargetDistance =
                float.MaxValue;

            float bestFreeTargetScore =
                float.MaxValue;

            UnitRuntime nearestFallbackTarget = null;
            float nearestFallbackDistance =
                float.MaxValue;

            float engagementRange =
                searchingUnit.Stats.AttackRange +
                EngagementTolerance;

            float engagementSqrRange =
                engagementRange * engagementRange;

            float reciprocalSqrRange =
                ReciprocalTargetPriorityRange *
                ReciprocalTargetPriorityRange;

            Vector2 searchingPosition =
                searchingUnit.Position;

            for (var i = 0; i < possibleTargets.Count; i++)
            {
                UnitRuntime candidate =
                    possibleTargets[i];

                if (!candidate.IsAlive)
                    continue;

                DistanceChecksLastTick++;

                Vector2 offset =
                    candidate.Position -
                    searchingPosition;

                float sqrDistance =
                    offset.sqrMagnitude;

                if (sqrDistance <
                    nearestFallbackDistance)
                {
                    nearestFallbackDistance =
                        sqrDistance;

                    nearestFallbackTarget =
                        candidate;
                }

                bool hasFreeSlot =
                    _engagementRegistry.HasFreeSlot(
                        candidate);

                if (!hasFreeSlot)
                {
                    FullTargetSkipsLastTick++;
                    continue;
                }

                int claimCount =
                    _engagementRegistry.GetClaimCount(
                        candidate);

                float score =
                    sqrDistance *
                    (1f +
                     claimCount *
                     ClaimDistancePenalty);

                if (score < bestFreeTargetScore)
                {
                    bestFreeTargetScore = score;
                    bestFreeTargetDistance = sqrDistance;
                    bestFreeTarget = candidate;
                }

                if (sqrDistance <= engagementSqrRange &&
                    sqrDistance <
                    nearestFreeEngagedDistance)
                {
                    nearestFreeEngagedDistance =
                        sqrDistance;

                    nearestFreeEngaged =
                        candidate;
                }

                if (candidate.Target == searchingUnit &&
                    sqrDistance <= reciprocalSqrRange &&
                    sqrDistance <
                    nearestFreeReciprocalDistance)
                {
                    nearestFreeReciprocalDistance =
                        sqrDistance;

                    nearestFreeReciprocal =
                        candidate;
                }
            }

            if (nearestFreeEngaged != null)
            {
                selectedSqrDistance =
                    nearestFreeEngagedDistance;

                return nearestFreeEngaged;
            }

            if (nearestFreeReciprocal != null)
            {
                selectedSqrDistance =
                    nearestFreeReciprocalDistance;

                return nearestFreeReciprocal;
            }

            if (bestFreeTarget != null)
            {
                selectedSqrDistance =
                    bestFreeTargetDistance;

                return bestFreeTarget;
            }

            selectedSqrDistance =
                nearestFallbackDistance;

            return nearestFallbackTarget;
        }

        private bool ShouldSwitchTarget(
            UnitRuntime owner,
            UnitRuntime currentTarget,
            UnitRuntime candidate,
            float candidateSqrDistance)
        {
            if (candidate == currentTarget)
                return false;

            if (!IsTargetValid(owner, currentTarget))
                return true;

            bool currentHasFreeSlot =
                _engagementRegistry.HasFreeSlot(
                    currentTarget);

            bool candidateHasFreeSlot =
                _engagementRegistry.HasFreeSlot(
                    candidate);

            if (!currentHasFreeSlot &&
                candidateHasFreeSlot)
            {
                return true;
            }

            Vector2 currentOffset =
                currentTarget.Position -
                owner.Position;

            float currentSqrDistance =
                currentOffset.sqrMagnitude;

            float engagementRange =
                owner.Stats.AttackRange +
                EngagementTolerance;

            float engagementSqrRange =
                engagementRange * engagementRange;

            if (currentSqrDistance <=
                engagementSqrRange)
            {
                return false;
            }

            if (candidateSqrDistance <=
                engagementSqrRange)
            {
                return true;
            }

            if (candidate.Target == owner &&
                candidateSqrDistance <=
                currentSqrDistance * 2.25f)
            {
                return true;
            }

            int currentClaims =
                _engagementRegistry.GetClaimCount(
                    currentTarget);

            int candidateClaims =
                _engagementRegistry.GetClaimCount(
                    candidate);

            if (candidateHasFreeSlot &&
                candidateClaims < currentClaims &&
                candidateSqrDistance <=
                currentSqrDistance * 1.44f)
            {
                return true;
            }

            float switchFactorSqr =
                TargetSwitchDistanceFactor *
                TargetSwitchDistanceFactor;

            return candidateSqrDistance <
                   currentSqrDistance *
                   switchFactorSqr;
        }

        private static void RestartRefreshTimer(
            UnitRuntime unit)
        {
            float normalizedJitter =
                unit.Id % 8 / 7f;

            float refreshDuration =
                TargetRefreshInterval +
                TargetRefreshJitter *
                normalizedJitter;

            unit.StartTargetRefreshCooldown(
                refreshDuration);
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
