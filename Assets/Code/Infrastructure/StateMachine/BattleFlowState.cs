namespace Code.Gameplay
{
    public enum BattleFlowState
    {
        None = 0,

        /// <summary>Отряд собран, ждём команды игрока начать бой.</summary>
        Preparation = 1,

        /// <summary>Симуляция идёт.</summary>
        Fighting = 2,

        /// <summary>Волна взята, игрок выбирает улучшение.</summary>
        Reward = 3,

        /// <summary>Забег окончен.</summary>
        Defeat = 4
    }
}
