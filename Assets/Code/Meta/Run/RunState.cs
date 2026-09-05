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

        /// <summary>
        /// Строй, которым игрок выйдет в следующий бой.
        /// null означает бой без строя — это законное состояние,
        /// а не «не выбрано»: в начале игры не открыт ни один строй.
        /// </summary>
        public FormationConfig SelectedFormation { get; private set; }

        /// <summary>
        /// Контракт, взятый на текущий раунд. Хранится копией предложения,
        /// а не ссылкой на выданное: список предложений переиспользуется
        /// драфтером и к началу боя будет уже перезаписан.
        /// </summary>
        public ContractOffer ActiveContract { get; } = new();

        public bool HasActiveContract => ActiveContract.Source != null;

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

            // Ни отряда, ни строя: и то, и другое игрок выбирает сам
            // в начале забега. Состав из RunConfig и строй из него же
            // подставляются только как запасной вариант, если ростер
            // или каталог строёв ещё не настроены.
            SelectedFormation = null;

            _acquiredUpgrades.Clear();
            _squad.Clear();

            ActiveContract.Clear();
        }

        public void SelectFormation(FormationConfig formation)
        {
            SelectedFormation = formation;
        }

        public void AcceptContract(ContractOffer offer)
        {
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));

            ActiveContract.CopyFrom(offer);
        }

        public void ClearContract()
        {
            ActiveContract.Clear();
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

        /// <summary>
        /// Забирает всё золото забега, обнуляя счёт.
        ///
        /// Именно забирает, а не читает: золото переезжает в денарии
        /// школы, и после переезда его на счету забега быть не должно.
        /// Иначе повторный вызов начислил бы те же деньги второй раз.
        /// </summary>
        public int TakeAllGold()
        {
            int taken = Gold;
            Gold = 0;

            return taken;
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

        /// <summary>Ставит стартовый отряд из одного типа бойцов.</summary>
        public void SetStartingSquad(UnitConfig config, int count)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _squad.Clear();
            AddUnits(config, count);
        }

        /// <summary>Запасной путь: состав целиком из RunConfig.</summary>
        public void ApplyFallbackSquad()
        {
            _squad.Clear();

            IReadOnlyList<SquadEntry> startingSquad = _config.StartingSquad;

            for (var i = 0; i < startingSquad.Count; i++)
            {
                SquadEntry entry = startingSquad[i];

                if (entry == null || entry.Config == null)
                    continue;

                // Копия, а не ссылка: рост отряда по ходу забега иначе
                // писался бы прямо в ассет и пережил бы Play Mode.
                _squad.Add(entry.Clone());
            }
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
