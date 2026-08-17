using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitDeathBuffer
    {
        private const int InitialCapacity = 256;

        private readonly List<UnitRuntime> _deadUnits =
            new(InitialCapacity);

        public IReadOnlyList<UnitRuntime> DeadUnits =>
            _deadUnits;

        public int Count => _deadUnits.Count;

        public void Add(UnitRuntime unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            _deadUnits.Add(unit);
        }

        public void Clear()
        {
            _deadUnits.Clear();
        }
    }
}
