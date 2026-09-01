using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Контракт, предложенный игроку в этом раунде.
    ///
    /// Отдельный тип, а не сам ассет: с ростом раунда количество врагов
    /// и награда масштабируются, и эти посчитанные числа надо где-то
    /// держать. Писать их обратно в ScriptableObject нельзя — правки
    /// пережили бы выход из Play Mode.
    /// </summary>
    public sealed class ContractOffer
    {
        private readonly List<SquadEntry> _enemies = new(8);

        public ContractConfig Source { get; private set; }
        public int GoldReward { get; private set; }

        public IReadOnlyList<SquadEntry> Enemies => _enemies;

        public FormationConfig EnemyFormation =>
            Source != null
                ? Source.EnemyFormation
                : null;

        public string DisplayName =>
            Source != null
                ? Source.DisplayName
                : string.Empty;

        public string Description =>
            Source != null
                ? Source.Description
                : string.Empty;

        public int TotalEnemyCount
        {
            get
            {
                var total = 0;

                for (var i = 0; i < _enemies.Count; i++)
                    total += _enemies[i].Count;

                return total;
            }
        }

        public void Set(
            ContractConfig source,
            int goldReward,
            IReadOnlyList<SquadEntry> enemies)
        {
            Source = source;
            GoldReward = goldReward;

            _enemies.Clear();

            for (var i = 0; i < enemies.Count; i++)
                _enemies.Add(enemies[i]);
        }

        public void CopyFrom(ContractOffer other)
        {
            Set(other.Source, other.GoldReward, other.Enemies);
        }

        public void Clear()
        {
            Source = null;
            GoldReward = 0;

            _enemies.Clear();
        }
    }
}
