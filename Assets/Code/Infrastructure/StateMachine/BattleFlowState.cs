namespace Code.Gameplay
{
    public enum BattleFlowState
    {
        None = 0,

        /// <summary>Игрок собирает стартовый отряд из открытых бойцов.</summary>
        SquadSelection = 1,

        /// <summary>Игрок выбирает, с кем драться в этом раунде.</summary>
        ContractSelection = 2,

        /// <summary>Контракт взят, отряд собран, ждём команды начать бой.</summary>
        Preparation = 3,

        /// <summary>Симуляция идёт.</summary>
        Fighting = 4,

        /// <summary>Контракт выполнен, показываем итоги боя.</summary>
        BattleSummary = 5,

        /// <summary>Игрок выбирает улучшение.</summary>
        Reward = 6,

        /// <summary>Забег окончен.</summary>
        Defeat = 7
    }
}
