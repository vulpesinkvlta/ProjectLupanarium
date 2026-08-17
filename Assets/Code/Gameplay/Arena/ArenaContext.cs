using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class ArenaContext 
    {
        private const int InitialCapacity = 512;

        private readonly List<UnitRuntime> _allUnits =
            new(InitialCapacity);

        private readonly List<UnitRuntime> _playerUnits =
            new(InitialCapacity / 2);

        private readonly List<UnitRuntime> _enemyUnits =
            new(InitialCapacity / 2);

        private readonly Dictionary<int, UnitRuntime> _unitsById =
            new(InitialCapacity);

        public IReadOnlyList<UnitRuntime> AllUnits => _allUnits;
        public IReadOnlyList<UnitRuntime> PlayerUnits => _playerUnits;
        public IReadOnlyList<UnitRuntime> EnemyUnits => _enemyUnits;

        public int UnitCount => _allUnits.Count;

        public void AddUnit(UnitRuntime unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            if (!_unitsById.TryAdd(unit.Id, unit))
            {
                throw new InvalidOperationException(
                    $"A unit with ID {unit.Id} is already registered.");
            }

            _allUnits.Add(unit);

            switch (unit.Team)
            {
                case TeamId.Player:
                    _playerUnits.Add(unit);
                    break;

                case TeamId.Enemy:
                    _enemyUnits.Add(unit);
                    break;

                default:
                    _unitsById.Remove(unit.Id);
                    _allUnits.Remove(unit);

                    throw new ArgumentOutOfRangeException(
                        nameof(unit),
                        unit.Team,
                        "Unsupported team.");
            }
        }

        public bool RemoveUnit(UnitRuntime unit)
        {
            if (unit == null)
                return false;

            if (!_unitsById.Remove(unit.Id))
                return false;

            _allUnits.Remove(unit);

            switch (unit.Team)
            {
                case TeamId.Player:
                    _playerUnits.Remove(unit);
                    break;

                case TeamId.Enemy:
                    _enemyUnits.Remove(unit);
                    break;
            }

            return true;
        }

        public bool TryGetUnit(
            int unitId,
            out UnitRuntime unit)
        {
            return _unitsById.TryGetValue(unitId, out unit);
        }

        public void Clear()
        {
            _unitsById.Clear();
            _allUnits.Clear();
            _playerUnits.Clear();
            _enemyUnits.Clear();
        }
    }
}
