using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(
           fileName = "UnitClassHudCatalog",
           menuName = "Gladiator Game/UI/Unit Class HUD Catalog")]
    public sealed class UnitClassHudCatalog : ScriptableObject
    {
        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGet(
            UnitClassId classId,
            out Entry result)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];

                if (entry != null &&
                    entry.ClassId == classId)
                {
                    result = entry;
                    return true;
                }
            }

            result = null;
            return false;
        }

        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private UnitClassId _classId;

            [SerializeField]
            private string _displayName;

            [SerializeField]
            private Sprite _icon;

            [SerializeField]
            private Color _accentColor = Color.white;

            public UnitClassId ClassId => _classId;

            public string DisplayName =>
                string.IsNullOrWhiteSpace(_displayName)
                    ? _classId.ToString()
                    : _displayName;

            public Sprite Icon => _icon;
            public Color AccentColor => _accentColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var usedClasses = new HashSet<UnitClassId>();

            for (var i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];

                if (entry == null)
                {
                    Debug.LogWarning(
                        $"HUD catalog contains an empty entry at index {i}.",
                        this);

                    continue;
                }

                if (entry.ClassId == UnitClassId.None)
                {
                    Debug.LogWarning(
                        $"HUD catalog entry {i} has class None.",
                        this);
                }

                if (!usedClasses.Add(entry.ClassId))
                {
                    Debug.LogWarning(
                        $"HUD catalog contains duplicate class " +
                        $"{entry.ClassId}.",
                        this);
                }
            }
        }
#endif
    }
}
