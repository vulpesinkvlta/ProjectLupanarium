using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(
        fileName = "UpgradeCatalog",
        menuName = "Gladiator Game/Upgrades/Upgrade Catalog")]
    public sealed class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private UpgradeConfig[] _upgrades;

        public IReadOnlyList<UpgradeConfig> Upgrades =>
            _upgrades ?? System.Array.Empty<UpgradeConfig>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_upgrades == null)
                return;

            for (var i = 0; i < _upgrades.Length; i++)
            {
                if (_upgrades[i] == null)
                {
                    Debug.LogWarning(
                        $"UpgradeCatalog '{name}': пустая ячейка {i}.",
                        this);
                }
            }
        }
#endif
    }
}
