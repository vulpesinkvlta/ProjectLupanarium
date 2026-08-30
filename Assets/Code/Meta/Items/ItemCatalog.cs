using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(
        fileName = "ItemCatalog",
        menuName = "Gladiator Game/Items/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemConfig[] _items;

        public IReadOnlyList<ItemConfig> Items =>
            _items ?? System.Array.Empty<ItemConfig>();

        public ItemConfig FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            IReadOnlyList<ItemConfig> items = Items;

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Id == id)
                    return items[i];
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_items == null)
                return;

            var usedIds = new HashSet<string>();

            for (var i = 0; i < _items.Length; i++)
            {
                ItemConfig item = _items[i];

                if (item == null)
                {
                    Debug.LogWarning(
                        $"ItemCatalog '{name}': пустая ячейка {i}.",
                        this);

                    continue;
                }

                if (!usedIds.Add(item.Id))
                {
                    Debug.LogWarning(
                        $"ItemCatalog '{name}': дублирующийся id " +
                        $"'{item.Id}'.",
                        this);
                }
            }
        }
#endif
    }
}
