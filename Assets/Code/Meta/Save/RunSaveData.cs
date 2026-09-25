using System;

namespace Code.Gameplay
{
    [Serializable]
    public sealed class RunSaveData
    {
        public BattleFlowState Phase;
        public int WaveIndex;
        public int Gold;
        public string FormationId;
        public SavedSquadEntry[] Squad = Array.Empty<SavedSquadEntry>();
        public string[] UpgradeIds = Array.Empty<string>();
        public string[] RewardChoiceIds = Array.Empty<string>();
        public SavedContractOffer ActiveContract;
        public SavedContractOffer[] ContractOffers = Array.Empty<SavedContractOffer>();
    }

    [Serializable]
    public sealed class SavedSquadEntry
    {
        public string UnitId;
        public int Count;
    }

    [Serializable]
    public sealed class SavedContractOffer
    {
        public string ContractId;
        public int GoldReward;
        public SavedSquadEntry[] Enemies = Array.Empty<SavedSquadEntry>();
    }
}
