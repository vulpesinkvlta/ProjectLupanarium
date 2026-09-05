using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Постоянное состояние школы: денарии, уровни построек и снаряжение.
    ///
    /// Переживает забеги — в отличие от RunState, который сбрасывается
    /// на каждый выход на арену. Всё хранится строковыми id, а не ссылками
    /// на ScriptableObject: именно в таком виде состояние уходит в сейв.
    /// </summary>
    public sealed class LupanariumState
    {
        private const int InitialBuildingCapacity = 16;
        private const int InitialItemCapacity = 32;

        private readonly Dictionary<string, int> _buildingLevels =
            new(InitialBuildingCapacity);

        private readonly HashSet<string> _ownedItems =
            new(InitialItemCapacity);

        // Ключ — «класс:слот», значение — id надетого предмета.
        private readonly Dictionary<string, string> _equippedBySlot =
            new(InitialItemCapacity);

        private readonly HashSet<string> _unlockedUnits =
            new(InitialItemCapacity);

        private readonly HashSet<string> _unlockedFormations =
            new(InitialItemCapacity);

        public int Denarii { get; private set; }

        /// <summary>
        /// Самый дальний раунд, до которого игрок доходил за все забеги.
        /// Порог для открытия новых бойцов.
        /// </summary>
        public int BestRoundReached { get; private set; }

        /// <summary>Состояние изменилось — UI перерисоваться, сейв записаться.</summary>
        public event Action Changed;

        // ---------- постройки ----------

        public int GetLevel(SchoolBuildingConfig building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            return _buildingLevels.TryGetValue(building.Id, out int level)
                ? level
                : 0;
        }

        public bool IsMaxLevel(SchoolBuildingConfig building)
        {
            return GetLevel(building) >= building.MaxLevel;
        }

        public bool CanUpgrade(SchoolBuildingConfig building)
        {
            if (!building.TryGetUpgradeCost(GetLevel(building), out int cost))
                return false;

            return Denarii >= cost;
        }

        public bool TryUpgrade(SchoolBuildingConfig building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            int level = GetLevel(building);

            if (!building.TryGetUpgradeCost(level, out int cost))
                return false;

            if (Denarii < cost)
                return false;

            Denarii -= cost;
            _buildingLevels[building.Id] = level + 1;

            Changed?.Invoke();
            return true;
        }

        // ---------- ростер ----------

        public bool IsUnitUnlocked(RosterEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            return entry.UnlockedFromStart ||
                   _unlockedUnits.Contains(entry.Id);
        }

        public bool MeetsRoundRequirement(RosterEntry entry)
        {
            return BestRoundReached >= entry.RequiredBestRound;
        }

        public bool CanUnlockUnit(RosterEntry entry)
        {
            return !IsUnitUnlocked(entry) &&
                   MeetsRoundRequirement(entry) &&
                   Denarii >= entry.Price;
        }

        public bool TryUnlockUnit(RosterEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!CanUnlockUnit(entry))
                return false;

            Denarii -= entry.Price;
            _unlockedUnits.Add(entry.Id);

            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Запоминает, как далеко зашёл забег. Только рекорд —
        /// неудачный забег не должен отбирать уже открытые пороги.
        /// </summary>
        public void RegisterRoundReached(int roundNumber)
        {
            if (roundNumber <= BestRoundReached)
                return;

            BestRoundReached = roundNumber;
            Changed?.Invoke();
        }

        // ---------- строи ----------

        public bool IsFormationUnlocked(FormationEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            return entry.UnlockedFromStart ||
                   _unlockedFormations.Contains(entry.Id);
        }

        public bool MeetsRoundRequirement(FormationEntry entry)
        {
            return BestRoundReached >= entry.RequiredBestRound;
        }

        public bool CanUnlockFormation(FormationEntry entry)
        {
            return !IsFormationUnlocked(entry) &&
                   MeetsRoundRequirement(entry) &&
                   Denarii >= entry.Price;
        }

        public bool TryUnlockFormation(FormationEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!CanUnlockFormation(entry))
                return false;

            Denarii -= entry.Price;
            _unlockedFormations.Add(entry.Id);

            Changed?.Invoke();
            return true;
        }

        // ---------- предметы ----------

        public bool IsOwned(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _ownedItems.Contains(item.Id);
        }

        public bool IsEquipped(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _equippedBySlot.TryGetValue(item.SlotKey, out string equipped) &&
                   equipped == item.Id;
        }

        public bool CanBuy(ItemConfig item)
        {
            return !IsOwned(item) && Denarii >= item.Price;
        }

        public bool TryBuy(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (IsOwned(item) || Denarii < item.Price)
                return false;

            Denarii -= item.Price;
            _ownedItems.Add(item.Id);

            // Купленный предмет сразу надевается, если ячейка свободна:
            // покупка, которая ничего не меняет, выглядит как баг.
            if (!_equippedBySlot.ContainsKey(item.SlotKey))
                _equippedBySlot[item.SlotKey] = item.Id;

            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!IsOwned(item) || IsEquipped(item))
                return false;

            _equippedBySlot[item.SlotKey] = item.Id;

            Changed?.Invoke();
            return true;
        }

        /// <summary>Id предмета, надетого в ячейку, или null.</summary>
        public string GetEquippedItemId(string slotKey)
        {
            return _equippedBySlot.TryGetValue(slotKey, out string id)
                ? id
                : null;
        }

        public IReadOnlyDictionary<string, string> EquippedBySlot =>
            _equippedBySlot;

        // ---------- экономика ----------

        public void AddDenarii(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (amount == 0)
                return;

            Denarii += amount;
            Changed?.Invoke();
        }

        public void Reset()
        {
            _buildingLevels.Clear();
            _ownedItems.Clear();
            _equippedBySlot.Clear();
            _unlockedUnits.Clear();
            _unlockedFormations.Clear();

            Denarii = 0;
            BestRoundReached = 0;

            Changed?.Invoke();
        }

        // ---------- сохранение ----------

        public void CaptureTo(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.Version = GameSaveData.CurrentVersion;
            data.Denarii = Denarii;
            data.BestRoundReached = BestRoundReached;

            var unlocked = new string[_unlockedUnits.Count];
            _unlockedUnits.CopyTo(unlocked);
            data.UnlockedUnitIds = unlocked;

            var formations = new string[_unlockedFormations.Count];
            _unlockedFormations.CopyTo(formations);
            data.UnlockedFormationIds = formations;

            data.WriteBuildings(_buildingLevels);
            data.WriteEquipment(_equippedBySlot);

            var owned = new string[_ownedItems.Count];
            _ownedItems.CopyTo(owned);
            data.OwnedItemIds = owned;
        }

        /// <summary>
        /// Восстанавливает состояние из сейва. Событие Changed поднимается
        /// один раз в конце, а не на каждое поле.
        /// </summary>
        public void RestoreFrom(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Denarii = Math.Max(0, data.Denarii);
            BestRoundReached = Math.Max(0, data.BestRoundReached);

            _unlockedUnits.Clear();

            if (data.UnlockedUnitIds != null)
            {
                for (var i = 0; i < data.UnlockedUnitIds.Length; i++)
                {
                    string id = data.UnlockedUnitIds[i];

                    if (!string.IsNullOrEmpty(id))
                        _unlockedUnits.Add(id);
                }
            }

            _unlockedFormations.Clear();

            if (data.UnlockedFormationIds != null)
            {
                for (var i = 0; i < data.UnlockedFormationIds.Length; i++)
                {
                    string id = data.UnlockedFormationIds[i];

                    if (!string.IsNullOrEmpty(id))
                        _unlockedFormations.Add(id);
                }
            }

            data.ReadBuildings(_buildingLevels);
            data.ReadEquipment(_equippedBySlot);

            _ownedItems.Clear();

            if (data.OwnedItemIds != null)
            {
                for (var i = 0; i < data.OwnedItemIds.Length; i++)
                {
                    string id = data.OwnedItemIds[i];

                    if (!string.IsNullOrEmpty(id))
                        _ownedItems.Add(id);
                }
            }

            Changed?.Invoke();
        }
    }
}
