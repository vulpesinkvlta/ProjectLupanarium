namespace Code.Gameplay
{
    /// <summary>
    /// Всё, что нужно знать о волне помимо состава врагов:
    /// награда за победу и строй, которым враги выходят на арену.
    /// </summary>
    public readonly struct WaveSetup
    {
        public int GoldReward { get; }
        public FormationConfig Formation { get; }

        public WaveSetup(int goldReward, FormationConfig formation)
        {
            GoldReward = goldReward;
            Formation = formation;
        }
    }
}
