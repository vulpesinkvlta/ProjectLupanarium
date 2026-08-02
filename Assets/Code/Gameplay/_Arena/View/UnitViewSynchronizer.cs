using System;
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
        private readonly UnitDeathBuffer _deathBuffer;

        public UnitViewSynchronizer(
            ArenaContext context,
            UnitViewRegistry viewRegistry,
            UnitViewPool viewPool,
            UnitDeathBuffer deathBuffer)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _deathBuffer = deathBuffer ??
                throw new ArgumentNullException(nameof(deathBuffer));
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
            var deadUnits =
                _deathBuffer.DeadUnits;

            for (var i = 0; i < deadUnits.Count; i++)
            {
                var deadUnit = deadUnits[i];

                if (!_viewRegistry.Remove(
                        deadUnit.Id,
                        out UnitView view))
                {
                    continue;
                }

                _viewPool.Release(view);
            }

            _deathBuffer.Clear();
        }
    }
}
