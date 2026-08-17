using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public class UnitViewRegistry 
    {
        private const int InitialCapacity = 512;

        private readonly Dictionary<int, UnitView> _viewsByUnitId =
            new(InitialCapacity);

        public int Count => _viewsByUnitId.Count;

        public void Add(int unitId, UnitView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (!_viewsByUnitId.TryAdd(unitId, view))
            {
                throw new InvalidOperationException(
                    $"A view for unit {unitId} is already registered.");
            }
        }

        public bool TryGet(
            int unitId,
            out UnitView view)
        {
            return _viewsByUnitId.TryGetValue(
                unitId,
                out view);
        }

        public bool Remove(
            int unitId,
            out UnitView view)
        {
            if (!_viewsByUnitId.TryGetValue(unitId, out view))
                return false;

            _viewsByUnitId.Remove(unitId);
            return true;
        }

        /// <summary>
        /// Перекладывает все зарегистрированные вью в переданный список
        /// и очищает реестр. Нужен для полной зачистки арены: обходить
        /// ArenaContext для этого нельзя, погибших юнитов там уже нет.
        /// </summary>
        public void DrainInto(List<UnitView> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            foreach (KeyValuePair<int, UnitView> pair in _viewsByUnitId)
                destination.Add(pair.Value);

            _viewsByUnitId.Clear();
        }

        public void Clear()
        {
            _viewsByUnitId.Clear();
        }
    }
}
