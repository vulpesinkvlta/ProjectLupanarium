using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class SpatialGrid
    {
        // Для текущего радиуса 0.35:
        // диаметр юнита равен 0.7.
        //
        // Ячейка 1.0 позволяет искать обычных соседей
        // в пределах сетки 3x3.
        private const float DefaultCellSize = 1f;

        private const int InitialBucketCapacity = 256;
        private const int InitialActiveCellCapacity = 128;
        private const int InitialEntriesPerCell = 16;

        private static readonly ProfilerMarker RebuildMarker =
            new("Arena.SpatialGrid.Rebuild");

        private readonly Dictionary<SpatialCell, CellBucket> _buckets =
            new(InitialBucketCapacity);

        private readonly List<CellBucket> _activeBuckets =
            new(InitialActiveCellCapacity);

        private readonly float _cellSize;
        private readonly float _inverseCellSize;

        public float CellSize => _cellSize;

        public int ActiveCellCount =>
            _activeBuckets.Count;

        public int CreatedCellCount =>
            _buckets.Count;

        public int UnitCountLastBuild
        {
            get;
            private set;
        }

        public float MaximumUnitRadius
        {
            get;
            private set;
        }

        public SpatialGrid()
        {
            _cellSize = DefaultCellSize;
            _inverseCellSize = 1f / _cellSize;
        }

        public void Rebuild(
            IReadOnlyList<UnitRuntime> units)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));

            using (RebuildMarker.Auto())
            {
                ClearActiveBuckets();

                UnitCountLastBuild = 0;
                MaximumUnitRadius = 0f;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    SpatialCell cell =
                        GetCell(unit.Position);

                    CellBucket bucket =
                        GetOrCreateBucket(cell);

                    ActivateBucketIfNeeded(bucket);

                    bucket.Entries.Add(
                        new SpatialGridEntry(
                            unit,
                            i));

                    MaximumUnitRadius = Mathf.Max(
                        MaximumUnitRadius,
                        unit.Stats.Radius);

                    UnitCountLastBuild++;
                }
            }
        }

        public SpatialCell GetCell(
            Vector2 position)
        {
            int x = Mathf.FloorToInt(
                position.x * _inverseCellSize);

            int y = Mathf.FloorToInt(
                position.y * _inverseCellSize);

            return new SpatialCell(x, y);
        }

        public int GetNeighbourRange(
            float unitRadius)
        {
            if (unitRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitRadius));
            }

            float maximumInteractionDistance =
                unitRadius + MaximumUnitRadius;

            return Mathf.Max(
                1,
                Mathf.CeilToInt(
                    maximumInteractionDistance /
                    _cellSize));
        }

        public bool TryGetEntries(
            int cellX,
            int cellY,
            out IReadOnlyList<SpatialGridEntry> entries)
        {
            var cell = new SpatialCell(
                cellX,
                cellY);

            if (_buckets.TryGetValue(
                    cell,
                    out CellBucket bucket) &&
                bucket.IsActive &&
                bucket.Entries.Count > 0)
            {
                entries = bucket.Entries;
                return true;
            }

            entries = null;
            return false;
        }

        /// <summary>
        /// Собирает живых юнитов указанной команды в радиусе от точки.
        ///
        /// Нужен таргетингу: перебирать всех врагов на арене — это O(n·m),
        /// а бойца интересуют только те, до кого он реально может дойти.
        /// Пишет в переданный список, чтобы не мусорить каждый тик.
        /// </summary>
        public void QueryTeam(
            Vector2 center,
            float radius,
            TeamId team,
            List<UnitRuntime> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));

            destination.Clear();

            SpatialCell centerCell = GetCell(center);

            int cellRange = Mathf.Max(
                1,
                Mathf.CeilToInt(radius * _inverseCellSize));

            float sqrRadius = radius * radius;

            for (int offsetX = -cellRange; offsetX <= cellRange; offsetX++)
            {
                for (int offsetY = -cellRange; offsetY <= cellRange; offsetY++)
                {
                    if (!TryGetEntries(
                            centerCell.X + offsetX,
                            centerCell.Y + offsetY,
                            out IReadOnlyList<SpatialGridEntry> entries))
                    {
                        continue;
                    }

                    CollectFromCell(
                        entries,
                        center,
                        sqrRadius,
                        team,
                        destination);
                }
            }
        }

        private static void CollectFromCell(
            IReadOnlyList<SpatialGridEntry> entries,
            Vector2 center,
            float sqrRadius,
            TeamId team,
            List<UnitRuntime> destination)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                UnitRuntime unit = entries[i].Unit;

                if (unit.Team != team)
                    continue;

                if (!unit.IsAlive)
                    continue;

                // Ячейки квадратные, радиус круглый: углы отсекаем здесь.
                Vector2 offset = unit.Position - center;

                if (offset.sqrMagnitude > sqrRadius)
                    continue;

                destination.Add(unit);
            }
        }

        public void Clear()
        {
            ClearActiveBuckets();

            UnitCountLastBuild = 0;
            MaximumUnitRadius = 0f;
        }

        private CellBucket GetOrCreateBucket(
            SpatialCell cell)
        {
            if (_buckets.TryGetValue(
                    cell,
                    out CellBucket bucket))
            {
                return bucket;
            }

            bucket = new CellBucket(
                InitialEntriesPerCell);

            _buckets.Add(
                cell,
                bucket);

            return bucket;
        }

        private void ActivateBucketIfNeeded(
            CellBucket bucket)
        {
            if (bucket.IsActive)
                return;

            bucket.IsActive = true;
            _activeBuckets.Add(bucket);
        }

        private void ClearActiveBuckets()
        {
            for (var i = 0;
                 i < _activeBuckets.Count;
                 i++)
            {
                CellBucket bucket =
                    _activeBuckets[i];

                bucket.Entries.Clear();
                bucket.IsActive = false;
            }

            _activeBuckets.Clear();
        }

        private sealed class CellBucket
        {
            public readonly List<SpatialGridEntry> Entries;

            public bool IsActive;

            public CellBucket(int initialCapacity)
            {
                Entries = new List<SpatialGridEntry>(
                    initialCapacity);
            }
        }
    }
}
