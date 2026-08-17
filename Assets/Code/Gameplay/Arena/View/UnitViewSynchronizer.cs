using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitViewSynchronizer
    {
        private static readonly ProfilerMarker UpdateMarker =
            new("Arena.ViewSynchronization");

        private readonly ArenaContext _context;
        private readonly UnitViewRegistry _viewRegistry;
        private readonly UnitViewPool _viewPool;
        private readonly DeadViewQueue _deadViewQueue;

        public UnitViewSynchronizer(
            ArenaContext context,
            UnitViewRegistry viewRegistry,
            UnitViewPool viewPool,
            DeadViewQueue deadViewQueue)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _deadViewQueue = deadViewQueue ??
                throw new ArgumentNullException(
                    nameof(deadViewQueue));
        }

        public void UpdateViews(float interpolationAlpha)
        {
            using (UpdateMarker.Auto())
            {
                ReleaseDeadViews();

                float alpha =
                    Mathf.Clamp01(interpolationAlpha);

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    var unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    if (!_viewRegistry.TryGet(
                            unit.Id,
                            out UnitView view))
                    {
                        continue;
                    }

                    Vector2 visualPosition =
                        Vector2.Lerp(
                            unit.PreviousPosition,
                            unit.Position,
                            alpha);

                    view.SetVisualPosition(
                        visualPosition);
                }
            }
        }

        private void ReleaseDeadViews()
        {
            IReadOnlyList<int> deadUnitIds =
                _deadViewQueue.UnitIds;

            for (var i = 0; i < deadUnitIds.Count; i++)
            {
                if (!_viewRegistry.Remove(
                        deadUnitIds[i],
                        out UnitView view))
                {
                    continue;
                }

                _viewPool.Release(view);
            }

            _deadViewQueue.Clear();
        }
    }
}
