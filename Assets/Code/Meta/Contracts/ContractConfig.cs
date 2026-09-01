using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Контракт: с кем предстоит драться и сколько за это платят.
    ///
    /// Заменил линейный WaveConfig: раунд теперь не «следующая волна
    /// из списка», а выбор игрока из нескольких предложений. Богатый
    /// контракт против дешёвого — это решение, а не расписание.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContractConfig",
        menuName = "Gladiator Game/Contracts/Contract")]
    public sealed class ContractConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Enemies")]
        [SerializeField] private SquadEntry[] _enemies;
        [SerializeField] private FormationConfig _enemyFormation;

        [Header("Reward")]
        [SerializeField, Min(0)] private int _goldReward = 100;

        [Header("Availability")]
        [Tooltip("С какого раунда контракт может выпасть. " +
                 "Считается с единицы.")]
        [SerializeField, Min(1)] private int _minimumRound = 1;

        [Tooltip("Вес при жеребьёвке среди доступных контрактов.")]
        [SerializeField, Min(1)] private int _weight = 1;

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Description => _description;
        public FormationConfig EnemyFormation => _enemyFormation;
        public int GoldReward => _goldReward;
        public int MinimumRound => _minimumRound;
        public int Weight => _weight;

        public IReadOnlyList<SquadEntry> Enemies =>
            _enemies ?? System.Array.Empty<SquadEntry>();

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

        /// <summary>Сколько всего врагов в контракте.</summary>
        public int TotalEnemyCount
        {
            get
            {
                var total = 0;

                IReadOnlyList<SquadEntry> enemies = Enemies;

                for (var i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] != null)
                        total += enemies[i].Count;
                }

                return total;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"ContractConfig '{name}' has an empty ID.",
                    this);
            }

            if (_enemies == null || _enemies.Length == 0)
            {
                Debug.LogWarning(
                    $"ContractConfig '{name}': враги не заданы, " +
                    $"бой засчитается победой мгновенно.",
                    this);
            }
        }
#endif
    }
}
