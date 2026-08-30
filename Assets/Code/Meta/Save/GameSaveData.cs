using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Плоский снимок постоянного прогресса.
    ///
    /// Только массивы и примитивы: JsonUtility не умеет ни словари,
    /// ни интерфейсы, ни ссылки на ScriptableObject. Поэтому пары
    /// «ключ-значение» разложены в параллельные массивы, а постройки
    /// и предметы хранятся строковыми id.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        /// <summary>
        /// Версия схемы. Меняется при несовместимом изменении полей,
        /// чтобы старый сейв можно было опознать и мигрировать,
        /// а не читать как мусор.
        /// </summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        public int Denarii;

        public string[] BuildingIds = Array.Empty<string>();
        public int[] BuildingLevels = Array.Empty<int>();

        public string[] OwnedItemIds = Array.Empty<string>();

        public string[] EquippedSlotKeys = Array.Empty<string>();
        public string[] EquippedItemIds = Array.Empty<string>();

        public void WriteBuildings(Dictionary<string, int> levels)
        {
            int count = levels.Count;

            BuildingIds = new string[count];
            BuildingLevels = new int[count];

            var i = 0;

            foreach (KeyValuePair<string, int> pair in levels)
            {
                BuildingIds[i] = pair.Key;
                BuildingLevels[i] = pair.Value;
                i++;
            }
        }

        public void ReadBuildings(Dictionary<string, int> destination)
        {
            destination.Clear();

            if (BuildingIds == null || BuildingLevels == null)
                return;

            // Массивы пишутся парой, но файл мог быть отредактирован
            // руками — читаем по минимальной длине, а не по одной из них.
            int count = Math.Min(
                BuildingIds.Length,
                BuildingLevels.Length);

            for (var i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(BuildingIds[i]))
                    continue;

                destination[BuildingIds[i]] = BuildingLevels[i];
            }
        }

        public void WriteEquipment(Dictionary<string, string> equipped)
        {
            int count = equipped.Count;

            EquippedSlotKeys = new string[count];
            EquippedItemIds = new string[count];

            var i = 0;

            foreach (KeyValuePair<string, string> pair in equipped)
            {
                EquippedSlotKeys[i] = pair.Key;
                EquippedItemIds[i] = pair.Value;
                i++;
            }
        }

        public void ReadEquipment(Dictionary<string, string> destination)
        {
            destination.Clear();

            if (EquippedSlotKeys == null || EquippedItemIds == null)
                return;

            int count = Math.Min(
                EquippedSlotKeys.Length,
                EquippedItemIds.Length);

            for (var i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(EquippedSlotKeys[i]))
                    continue;

                destination[EquippedSlotKeys[i]] = EquippedItemIds[i];
            }
        }
    }
}
