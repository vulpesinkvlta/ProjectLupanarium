using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Составляет набор контрактов на выбор для текущего раунда.
    ///
    /// Берёт только доступные по раунду, тянет их взвешенно без повторов
    /// и масштабирует под номер раунда: к десятому бою «10 мирмиллонов»
    /// должны быть заметно опаснее и дороже, чем к первому.
    /// </summary>
    public sealed class ContractDrafter
    {
        private const int InitialCapacity = 32;

        private readonly ContractCatalog _catalog;
        private readonly System.Random _random = new();

        private readonly List<ContractConfig> _candidates =
            new(InitialCapacity);

        private readonly List<SquadEntry> _scaledEnemies = new(8);

        public ContractDrafter(ContractCatalog catalog)
        {
            _catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Заполняет destination предложениями на раунд.
        /// Если подходящих контрактов меньше, чем просят, вернёт сколько есть.
        /// </summary>
        public void Draw(
            int roundNumber,
            int count,
            List<ContractOffer> destination)
        {
            if (roundNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(roundNumber));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            BuildCandidates(roundNumber);

            // Переиспользуем уже созданные предложения: экран контрактов
            // открывается каждый раунд, и плодить мусор незачем.
            EnsureCapacity(destination, Mathf.Min(count, _candidates.Count));

            int drawCount = Mathf.Min(count, _candidates.Count);

            for (var i = 0; i < drawCount; i++)
            {
                int index = PickWeightedIndex();

                ContractConfig picked = _candidates[index];
                _candidates.RemoveAt(index);

                Scale(picked, roundNumber, destination[i]);
            }

            // Лишние предложения гасим, чтобы UI не показал прошлый раунд.
            for (int i = drawCount; i < destination.Count; i++)
                destination[i].Clear();
        }

        private void BuildCandidates(int roundNumber)
        {
            _candidates.Clear();

            IReadOnlyList<ContractConfig> contracts = _catalog.Contracts;

            for (var i = 0; i < contracts.Count; i++)
            {
                ContractConfig contract = contracts[i];

                if (contract == null || contract.Weight <= 0)
                    continue;

                if (contract.MinimumRound > roundNumber)
                    continue;

                _candidates.Add(contract);
            }
        }

        private void Scale(
            ContractConfig source,
            int roundNumber,
            ContractOffer destination)
        {
            int extraRounds = Mathf.Max(0, roundNumber - 1);

            float enemyMultiplier =
                1f + _catalog.EnemyGrowthPerRound * extraRounds;

            float goldMultiplier =
                1f + _catalog.GoldGrowthPerRound * extraRounds;

            _scaledEnemies.Clear();

            IReadOnlyList<SquadEntry> enemies = source.Enemies;

            for (var i = 0; i < enemies.Count; i++)
            {
                SquadEntry entry = enemies[i];

                if (entry == null || entry.Config == null)
                    continue;

                int scaledCount = Mathf.Max(
                    1,
                    Mathf.RoundToInt(entry.Count * enemyMultiplier));

                _scaledEnemies.Add(
                    new SquadEntry(entry.Config, scaledCount));
            }

            destination.Set(
                source,
                Mathf.RoundToInt(source.GoldReward * goldMultiplier),
                _scaledEnemies);
        }

        private static void EnsureCapacity(
            List<ContractOffer> destination,
            int required)
        {
            while (destination.Count < required)
                destination.Add(new ContractOffer());
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
