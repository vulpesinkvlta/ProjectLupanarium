using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Держит бойцов внутри круга арены.
    ///
    /// Работает последней из систем, двигающих юнитов — после движения
    /// и расталкивания. Иначе расталкивание вытолкнуло бы крайних за
    /// границу уже после того, как её проверили: в плотной свалке толчок
    /// наружу ничем не ограничен.
    /// </summary>
    public sealed class ArenaBoundsSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Bounds");

        private readonly ArenaContext _context;
        private readonly ArenaBounds _bounds;

        public int ClampedUnitsLastTick { get; private set; }

        public ArenaBoundsSystem(
            ArenaContext context,
            ArenaBounds bounds)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _bounds = bounds ??
                throw new ArgumentNullException(nameof(bounds));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                ClampedUnitsLastTick = 0;

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    Vector2 clamped = _bounds.Clamp(
                        unit.Position,
                        unit.Stats.Radius);

                    if (clamped == unit.Position)
                        continue;

                    unit.SetPosition(clamped);
                    ClampedUnitsLastTick++;
                }
            }
        }
    }
}
