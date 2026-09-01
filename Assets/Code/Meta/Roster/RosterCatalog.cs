using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Один тип гладиатора в ростере школы: чем он открывается
    /// и с каким отрядом выходит на первый бой.
    /// </summary>
    [Serializable]
    public sealed class RosterEntry
    {
        [SerializeField] private UnitConfig _unit;

        [Tooltip("Доступен с самого начала, без покупки.")]
        [SerializeField] private bool _unlockedFromStart;

        [SerializeField, Min(0)] private int _price = 500;

        [Tooltip("Открыть можно, только если игрок хотя бы раз дошёл " +
                 "до этого раунда. Ноль — без требования.")]
        [SerializeField, Min(0)] private int _requiredBestRound;

        [Tooltip("Сколько таких бойцов даётся при выборе стартового отряда.")]
        [SerializeField, Min(1)] private int _startingCount = 5;

        public UnitConfig Unit => _unit;
        public bool UnlockedFromStart => _unlockedFromStart;
        public int Price => _price;
        public int RequiredBestRound => _requiredBestRound;
        public int StartingCount => _startingCount;

        public string Id => _unit != null ? _unit.Id : string.Empty;
    }

    /// <summary>
    /// Все типы гладиаторов, которые игрок может открыть.
    ///
    /// Открытие стоит денариев и требует дойти до определённого раунда.
    /// Одна цена без порога превратила бы прогресс в чистый гринд денег:
    /// можно было бы купить всё, ни разу не пройдя дальше первой волны.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RosterCatalog",
        menuName = "Gladiator Game/Roster/Roster Catalog")]
    public sealed class RosterCatalog : ScriptableObject
    {
        [SerializeField] private RosterEntry[] _entries;

        public IReadOnlyList<RosterEntry> Entries =>
            _entries ?? Array.Empty<RosterEntry>();

        public RosterEntry FindByUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return null;

            IReadOnlyList<RosterEntry> entries = Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Id == unitId)
                    return entries[i];
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_entries == null || _entries.Length == 0)
            {
                Debug.LogWarning(
                    $"RosterCatalog '{name}': ростер пуст, игроку " +
                    $"не с кем будет начать забег.",
                    this);

                return;
            }

            var hasStarter = false;

            for (var i = 0; i < _entries.Length; i++)
            {
                RosterEntry entry = _entries[i];

                if (entry == null || entry.Unit == null)
                {
                    Debug.LogWarning(
                        $"RosterCatalog '{name}': пустая запись {i}.",
                        this);

                    continue;
                }

                if (entry.UnlockedFromStart)
                    hasStarter = true;
            }

            if (!hasStarter)
            {
                Debug.LogWarning(
                    $"RosterCatalog '{name}': ни один боец не открыт " +
                    $"с самого начала — первый забег будет не с кем начать.",
                    this);
            }
        }
#endif
    }
}
