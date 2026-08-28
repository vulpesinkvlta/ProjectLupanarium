using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Состояние текущего забега: номер волны, золото, состав отряда
    /// и набранные улучшения.
    ///
    /// Обычный C#-класс, а не ScriptableObject: SO сохранял бы изменения
    /// между запусками Play Mode, и забег продолжался бы после выхода.
    /// В Фазе 5 этот же класс уйдёт в JSON-сохранение.
    /// </summary>
    public sealed class RunState
    {
        private const int InitialSquadCapacity = 8;
        private const int InitialUpgradeCapacity = 32;

        private readonly RunConfig _config;

        private readonly List<SquadEntry> _squad = new(InitialSquadCapacity);

        private readonly List<UpgradeConfig> _acquiredUpgrades =
            new(InitialUpgradeCapacity);

        public int WaveIndex { get; private set; }
        public int Gold { get; private set; }

        public IReadOnlyList<SquadEntry> Squad => _squad;
        public IReadOnlyList<UpgradeConfig> AcquiredUpgrades => _acquiredUpgrades;

        /// <summary>Номер волны для показа игроку, считается с единицы.</summary>
        public int WaveNumber => WaveIndex + 1;

        public RunState(RunConfig config)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            Reset();
        }

        public int TotalUnitCount
        {
            get
            {
                var total = 0;

                for (var i = 0; i < _squad.Count; i++)
                    total += _squad[i].Count;

                return total;
            }
        }

        public void Reset()
        {
            WaveIndex = 0;
            Gold = _config.StartingGold;

            _acquiredUpgrades.Clear();
            _squad.Clear();

            IReadOnlyList<SquadEntry> startingSquad = _config.StartingSquad;

            for (var i = 0; i < startingSquad.Count; i++)
            {
                SquadEntry entry = startingSquad[i];

                if (entry == null || entry.Config == null)
                    continue;

                // Копия, а не ссылка: рост отряда по ходу забега иначе
                // писался бы прямо в ассет RunConfig и пережил бы Play Mode.
                _squad.Add(entry.Clone());
            }
        }

        public void AdvanceWave()
        {
            WaveIndex++;
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Gold += amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (Gold < amount)
                return false;

            Gold -= amount;
            return true;
        }

        public void AddUpgrade(UpgradeConfig upgrade)
        {
            if (upgrade == null)
                throw new ArgumentNullException(nameof(upgrade));

            _acquiredUpgrades.Add(upgrade);

            if (upgrade.AddsUnits)
                AddUnits(upgrade.UnitToAdd, upgrade.UnitAddCount);
        }

        public void AddUnits(UnitConfig config, int count)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (var i = 0; i < _squad.Count; i++)
            {
                if (_squad[i].Config != config)
                    continue;

                _squad[i].AddCount(count);
                return;
            }

            _squad.Add(new SquadEntry(config, count));
        }
    }
}
