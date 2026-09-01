using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(
        fileName = "ContractCatalog",
        menuName = "Gladiator Game/Contracts/Contract Catalog")]
    public sealed class ContractCatalog : ScriptableObject
    {
        [SerializeField] private ContractConfig[] _contracts;

        [Header("Endless scaling")]
        [Tooltip("Прирост количества врагов за каждый раунд сверх первого.")]
        [SerializeField, Min(0f)] private float _enemyGrowthPerRound = 0.15f;

        [Tooltip("Прирост награды за каждый раунд сверх первого.")]
        [SerializeField, Min(0f)] private float _goldGrowthPerRound = 0.12f;

        public IReadOnlyList<ContractConfig> Contracts =>
            _contracts ?? System.Array.Empty<ContractConfig>();

        public float EnemyGrowthPerRound => _enemyGrowthPerRound;
        public float GoldGrowthPerRound => _goldGrowthPerRound;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_contracts == null || _contracts.Length == 0)
            {
                Debug.LogWarning(
                    $"ContractCatalog '{name}': не задано ни одного " +
                    $"контракта, игроку нечего будет выбрать.",
                    this);

                return;
            }

            var usedIds = new HashSet<string>();

            for (var i = 0; i < _contracts.Length; i++)
            {
                ContractConfig contract = _contracts[i];

                if (contract == null)
                {
                    Debug.LogWarning(
                        $"ContractCatalog '{name}': пустая ячейка {i}.",
                        this);

                    continue;
                }

                if (!usedIds.Add(contract.Id))
                {
                    Debug.LogWarning(
                        $"ContractCatalog '{name}': дублирующийся id " +
                        $"'{contract.Id}'.",
                        this);
                }
            }
        }
#endif
    }
}
