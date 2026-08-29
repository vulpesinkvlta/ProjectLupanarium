using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Все постройки лупанария в порядке показа игроку.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SchoolCatalog",
        menuName = "Gladiator Game/Lupanarium/School Catalog")]
    public sealed class SchoolCatalog : ScriptableObject
    {
        [SerializeField] private SchoolBuildingConfig[] _buildings;

        public IReadOnlyList<SchoolBuildingConfig> Buildings =>
            _buildings ?? System.Array.Empty<SchoolBuildingConfig>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_buildings == null)
                return;

            var usedIds = new HashSet<string>();

            for (var i = 0; i < _buildings.Length; i++)
            {
                SchoolBuildingConfig building = _buildings[i];

                if (building == null)
                {
                    Debug.LogWarning(
                        $"SchoolCatalog '{name}': пустая ячейка {i}.",
                        this);

                    continue;
                }

                // Уровни построек хранятся по строковому id — дубликат
                // означал бы, что две постройки делят один прогресс.
                if (!usedIds.Add(building.Id))
                {
                    Debug.LogWarning(
                        $"SchoolCatalog '{name}': дублирующийся id " +
                        $"'{building.Id}'.",
                        this);
                }
            }
        }
#endif
    }
}
