using System;
using UnityEngine;

namespace Code.Gameplay
{
    public readonly struct SpatialGridEntry
    {
        public UnitRuntime Unit { get; }
        public int ContextIndex { get; }

        public SpatialGridEntry(
            UnitRuntime unit,
            int contextIndex)
        {
            Unit = unit ??
                throw new ArgumentNullException(nameof(unit));

            if (contextIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contextIndex));
            }

            ContextIndex = contextIndex;
        }
    }
}
