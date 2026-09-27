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

        [Tooltip("Составной рост числа врагов. Выключен для совместимости старых каталогов.")]
        [SerializeField] private bool _compoundEnemyGrowth;
        [SerializeField, Min(1)] private int _maximumEnemiesPerSquad = 256;
        public int ScaleCount(int count, int round)
        {
            float steps = Mathf.Max(0, round - 1);
            float multiplier = _compoundEnemyGrowth
                ? Mathf.Pow(1 + _enemyGrowthPerRound, Mathf.Min(steps, 1000))
                : 1 + _enemyGrowthPerRound * steps;
            return Mathf.RoundToInt(Mathf.Clamp(count * multiplier, 1, _maximumEnemiesPerSquad));
        }

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
