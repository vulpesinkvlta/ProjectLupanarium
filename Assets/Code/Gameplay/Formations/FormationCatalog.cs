using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Один строй в каталоге школы: чем он открывается и сколько стоит.
    ///
    /// Условия открытия живут здесь, а не в FormationConfig, по той же
    /// причине, что и у RosterEntry: сам FormationConfig — это чистая
    /// геометрия с бонусами, и его же используют вражеские контракты.
    /// Цена у вражеского строя не значила бы ничего.
    /// </summary>
    [Serializable]
    public sealed class FormationEntry
    {
        [SerializeField] private FormationConfig _formation;

        [Tooltip("Доступен с самого начала, без покупки.")]
        [SerializeField] private bool _unlockedFromStart;

        [SerializeField, Min(0)] private int _price = 600;

        [Tooltip("Открыть можно, только если игрок хотя бы раз дошёл " +
                 "до этого раунда. Ноль — без требования.")]
        [SerializeField, Min(0)] private int _requiredBestRound = 3;

        public FormationConfig Formation => _formation;
        public bool UnlockedFromStart => _unlockedFromStart;
        public int Price => _price;
        public int RequiredBestRound => _requiredBestRound;

        public string Id => _formation != null ? _formation.Id : string.Empty;
    }

    /// <summary>
    /// Строи, которые игрок может открыть и взять с собой на арену.
    ///
    /// В начале игры не открыт ни один: первые забеги проходят толпой,
    /// а строй становится наградой за прогресс. Поэтому каталог хранит
    /// не сами ассеты, а записи с условиями открытия.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FormationCatalog",
        menuName = "Gladiator Game/Formations/Formation Catalog")]
    public sealed class FormationCatalog : ScriptableObject
    {
        [SerializeField] private FormationEntry[] _entries;

        public IReadOnlyList<FormationEntry> Entries =>
            _entries ?? Array.Empty<FormationEntry>();

        public int Count => Entries.Count;

        public FormationEntry FindByFormation(FormationConfig formation)
        {
            if (formation == null)
                return null;

            IReadOnlyList<FormationEntry> entries = Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Formation == formation)
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
                    $"FormationCatalog '{name}': не задано ни одного строя. " +
                    $"Забеги будут проходить только без строя.",
                    this);

                return;
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                FormationEntry entry = _entries[i];

                if (entry == null || entry.Formation == null)
                {
                    Debug.LogWarning(
                        $"FormationCatalog '{name}': пустая запись {i}.",
                        this);

                    continue;
                }

                // Пустой id — это сломанное сохранение: открытый строй
                // не запишется в сейв и после перезапуска снова закроется.
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    Debug.LogWarning(
                        $"FormationCatalog '{name}': у строя " +
                        $"'{entry.Formation.name}' пустой Id, открытие " +
                        $"не переживёт перезапуск.",
                        this);
                }
            }
        }
#endif
    }
}
