namespace Code.Gameplay
{
    public enum BattleFlowState
    {
        None = 0,

        /// <summary>Игрок собирает стартовый отряд из открытых бойцов.</summary>
        SquadSelection = 1,

        /// <summary>Игрок выбирает, с кем драться в этом раунде.</summary>
        ContractSelection = 2,

        /// <summary>
        /// Игрок выбирает строй на этот бой. Состояние пропускается,
        /// пока не открыт ни один строй: выбирать было бы не из чего.
        /// </summary>
        FormationSelection = 3,

        /// <summary>Контракт взят, отряд собран, ждём команды начать бой.</summary>
        Preparation = 4,

        /// <summary>Симуляция идёт.</summary>
        Fighting = 5,

        /// <summary>Контракт выполнен, показываем итоги боя.</summary>
        BattleSummary = 6,

        /// <summary>Игрок выбирает улучшение.</summary>
        Reward = 7,

        /// <summary>Забег окончен.</summary>
        Defeat = 8
    }
}
