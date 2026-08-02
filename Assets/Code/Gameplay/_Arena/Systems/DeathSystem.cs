using System;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class DeathSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Death");

        private readonly ArenaContext _context;
        private readonly UnitDeathBuffer _deathBuffer;

        public int DeathsLastTick { get; private set; }

        public DeathSystem(
            ArenaContext context,
            UnitDeathBuffer deathBuffer)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _deathBuffer = deathBuffer ??
                throw new ArgumentNullException(nameof(deathBuffer));
        }

        public void Tick()
        {
            using (TickMarker.Auto())
            {
                DeathsLastTick = 0;

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    UnitRuntime unit = units[i];

                    if (unit.State == UnitState.Dead)
                        continue;

                    if (unit.CurrentHealth > 0f)
                        continue;

                    unit.MarkDead();
                    _deathBuffer.Add(unit);

                    DeathsLastTick++;
                }
            }
        }
    }
}
