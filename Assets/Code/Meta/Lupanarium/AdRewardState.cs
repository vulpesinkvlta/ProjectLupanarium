using System;

namespace Code.Gameplay
{
    public enum AdRewardKind { Money, Supplies, Heal, BattleBonus }

    public sealed partial class LupanariumState
    {
        private long[] _adReadyAt = new long[3];
        public int PendingBattleBonus { get; private set; }
        public string BattleBonusId { get; private set; }
        public long RewardReadyAt(AdRewardKind kind) => (int)kind < 3 ? _adReadyAt[(int)kind] : 0;
        public void SetRewardCooldown(AdRewardKind kind, long readyAt)
        { if ((int)kind < 3) _adReadyAt[(int)kind] = readyAt; }
        public void SetBattleBonus(int amount)
        {
            PendingBattleBonus = Math.Max(0, amount);
            BattleBonusId = Guid.NewGuid().ToString("N");
            // The flow's next state transition saves this with the battle payout.
        }
        public bool TryClaimBattleBonus(string id)
        {
            if (id != BattleBonusId || PendingBattleBonus <= 0) return false;
            int amount = PendingBattleBonus;
            PendingBattleBonus = 0;
            AddDenarii(amount);
            return true;
        }
        public void AddBlessingCharge(BlessingConfig blessing)
        {
            if (blessing == null) return;
            _blessingCharges[blessing.Id] = Math.Min(int.MaxValue - 1, GetBlessingCharges(blessing.Id)) + 1;
            Changed?.Invoke();
        }
        private void ResetAdRewards() { _adReadyAt = new long[3]; PendingBattleBonus = 0; BattleBonusId = null; }
        private void CaptureAdRewards(GameSaveData data)
        { data.AdReadyAt = (long[])_adReadyAt.Clone(); data.PendingBattleBonus = PendingBattleBonus; data.BattleBonusId = BattleBonusId; }
        private void RestoreAdRewards(GameSaveData data)
        {
            _adReadyAt = new long[3];
            if (data.AdReadyAt != null) Array.Copy(data.AdReadyAt, _adReadyAt, Math.Min(3, data.AdReadyAt.Length));
            PendingBattleBonus = Math.Max(0, data.PendingBattleBonus);
            BattleBonusId = data.BattleBonusId;
        }
    }
}
