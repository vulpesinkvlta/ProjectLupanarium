using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class SeparationSystem
    {
        private const int MinimumBufferCapacity = 16;

        private const float SeparationResponsiveness = 12f;
        private const float MaximumSeparationSpeed = 5f;

        private static readonly ProfilerMarker TickMarker =
            new("Arena.Separation");

        private readonly ArenaContext _context;
        private readonly SpatialGrid _spatialGrid;

        private Vector2[] _corrections =
            Array.Empty<Vector2>();

        public int PairChecksLastTick
        {
            get;
            private set;
        }

        public int OverlappingPairsLastTick
        {
            get;
            private set;
        }

        public int CorrectedUnitsLastTick
        {
            get;
            private set;
        }

        public SeparationSystem(
            ArenaContext context,
            SpatialGrid spatialGrid)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _spatialGrid = spatialGrid ??
                throw new ArgumentNullException(
                    nameof(spatialGrid));
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
                IReadOnlyList<UnitRuntime> units =
                    _context.AllUnits;

                EnsureBufferCapacity(units.Count);

                Array.Clear(
                    _corrections,
                    0,
                    units.Count);

                PairChecksLastTick = 0;
                OverlappingPairsLastTick = 0;
                CorrectedUnitsLastTick = 0;

                CalculateCorrections(
                    units,
                    deltaTime);

                ApplyCorrections(
                    units,
                    deltaTime);
            }
        }

        private void CalculateCorrections(
            IReadOnlyList<UnitRuntime> units,
            float deltaTime)
        {
            float responseFactor =
                Mathf.Clamp01(
                    SeparationResponsiveness *
                    deltaTime);

            for (var firstIndex = 0;
                 firstIndex < units.Count;
                 firstIndex++)
            {
                UnitRuntime firstUnit =
                    units[firstIndex];

                if (!firstUnit.IsAlive)
                    continue;

                SpatialCell centerCell =
                    _spatialGrid.GetCell(
                        firstUnit.Position);

                int neighbourRange =
                    _spatialGrid.GetNeighbourRange(
                        firstUnit.Stats.Radius);

                CheckNeighbourCells(
                    firstUnit,
                    firstIndex,
                    centerCell,
                    neighbourRange,
                    responseFactor);
            }
        }

        private void CheckNeighbourCells(
            UnitRuntime firstUnit,
            int firstIndex,
            SpatialCell centerCell,
            int neighbourRange,
            float responseFactor)
        {
            for (var offsetX = -neighbourRange;
                 offsetX <= neighbourRange;
                 offsetX++)
            {
                for (var offsetY = -neighbourRange;
                     offsetY <= neighbourRange;
                     offsetY++)
                {
                    int cellX =
                        centerCell.X + offsetX;

                    int cellY =
                        centerCell.Y + offsetY;

                    if (!_spatialGrid.TryGetEntries(
                            cellX,
                            cellY,
                            out IReadOnlyList<SpatialGridEntry>
                                entries))
                    {
                        continue;
                    }

                    CheckEntries(
                        firstUnit,
                        firstIndex,
                        entries,
                        responseFactor);
                }
            }
        }

        private void CheckEntries(
            UnitRuntime firstUnit,
            int firstIndex,
            IReadOnlyList<SpatialGridEntry> entries,
            float responseFactor)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                SpatialGridEntry entry =
                    entries[i];

                int secondIndex =
                    entry.ContextIndex;

                // Каждую пару обрабатываем только один раз.
                //
                // Если пара 10 и 25 уже проверяется
                // при первом индексе 10, при индексе 25
                // она будет пропущена.
                if (secondIndex <= firstIndex)
                    continue;

                UnitRuntime secondUnit =
                    entry.Unit;

                if (!secondUnit.IsAlive)
                    continue;

                PairChecksLastTick++;

                CalculatePairCorrection(
                    firstUnit,
                    secondUnit,
                    firstIndex,
                    secondIndex,
                    responseFactor);
            }
        }

        private void CalculatePairCorrection(
            UnitRuntime firstUnit,
            UnitRuntime secondUnit,
            int firstIndex,
            int secondIndex,
            float responseFactor)
        {
            Vector2 offset =
                secondUnit.Position -
                firstUnit.Position;

            float minimumDistance =
                firstUnit.Stats.Radius +
                secondUnit.Stats.Radius;

            float minimumSqrDistance =
                minimumDistance *
                minimumDistance;

            float sqrDistance =
                offset.sqrMagnitude;

            if (sqrDistance >= minimumSqrDistance)
                return;

            OverlappingPairsLastTick++;

            Vector2 direction;
            float distance;

            if (sqrDistance > Mathf.Epsilon)
            {
                distance = Mathf.Sqrt(
                    sqrDistance);

                direction =
                    offset / distance;
            }
            else
            {
                distance = 0f;

                direction =
                    GetFallbackDirection(
                        firstUnit.Id,
                        secondUnit.Id);
            }

            float overlap =
                minimumDistance -
                distance;

            float correctionMagnitude =
                overlap *
                0.5f *
                responseFactor;

            Vector2 correction =
                direction *
                correctionMagnitude;

            _corrections[firstIndex] -=
                correction;

            _corrections[secondIndex] +=
                correction;
        }

        private void ApplyCorrections(
            IReadOnlyList<UnitRuntime> units,
            float deltaTime)
        {
            float maximumCorrection =
                MaximumSeparationSpeed *
                deltaTime;

            float maximumSqrCorrection =
                maximumCorrection *
                maximumCorrection;

            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit =
                    units[i];

                if (!unit.IsAlive)
                    continue;

                Vector2 correction =
                    _corrections[i];

                float sqrMagnitude =
                    correction.sqrMagnitude;

                if (sqrMagnitude <= Mathf.Epsilon)
                    continue;

                if (sqrMagnitude >
                    maximumSqrCorrection)
                {
                    correction =
                        correction.normalized *
                        maximumCorrection;
                }

                unit.SetPosition(
                    unit.Position +
                    correction);

                CorrectedUnitsLastTick++;
            }
        }

        private void EnsureBufferCapacity(
            int requiredCapacity)
        {
            if (_corrections.Length >=
                requiredCapacity)
            {
                return;
            }

            int capacity =
                Mathf.NextPowerOfTwo(
                    Mathf.Max(
                        requiredCapacity,
                        MinimumBufferCapacity));

            Array.Resize(
                ref _corrections,
                capacity);
        }

        private static Vector2 GetFallbackDirection(
            int firstUnitId,
            int secondUnitId)
        {
            int hash = unchecked(
                firstUnitId * 73856093 ^
                secondUnitId * 19349663);

            return (hash & 3) switch
            {
                0 => Vector2.right,
                1 => Vector2.left,
                2 => Vector2.up,
                _ => Vector2.down
            };
        }
    }
}
