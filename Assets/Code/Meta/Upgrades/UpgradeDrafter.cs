using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Взвешенная выборка карточек улучшений без повторов внутри одной выдачи.
    /// </summary>
    public sealed class UpgradeDrafter
    {
        private const int InitialCapacity = 32;

        private readonly UpgradeCatalog _catalog;
        private readonly List<UpgradeConfig> _candidates = new(InitialCapacity);
        private readonly Random _random;

        public UpgradeDrafter(UpgradeCatalog catalog)
        {
            _catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));

            _random = new Random();
        }

        /// <summary>
        /// Кладёт в destination до count различных улучшений.
        /// Если подходящих вариантов в каталоге меньше, вернёт сколько есть.
        /// </summary>
        public void Draw(int count, List<UpgradeConfig> destination)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            BuildCandidates();

            int drawCount = Math.Min(count, _candidates.Count);

            for (var i = 0; i < drawCount; i++)
            {
                int index = PickWeightedIndex();

                destination.Add(_candidates[index]);

                // Выбранный кандидат выбывает из пула, иначе одна и та же
                // карточка могла бы попасть в выдачу дважды.
                _candidates.RemoveAt(index);
            }
        }

        private void BuildCandidates()
        {
            _candidates.Clear();

            IReadOnlyList<UpgradeConfig> upgrades = _catalog.Upgrades;

            for (var i = 0; i < upgrades.Count; i++)
            {
                UpgradeConfig upgrade = upgrades[i];

                if (upgrade == null || upgrade.Weight <= 0)
                    continue;

                _candidates.Add(upgrade);
            }
        }

        private int PickWeightedIndex()
        {
            var totalWeight = 0;

            for (var i = 0; i < _candidates.Count; i++)
                totalWeight += _candidates[i].Weight;

            int roll = _random.Next(totalWeight);
            var accumulated = 0;

            for (var i = 0; i < _candidates.Count; i++)
            {
                accumulated += _candidates[i].Weight;

                if (roll < accumulated)
                    return i;
            }

            return _candidates.Count - 1;
        }
    }
}
