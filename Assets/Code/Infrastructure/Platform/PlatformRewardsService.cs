using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class PlatformRewardsService : IStartable, IDisposable
    {
        private readonly RunState _run;
        private readonly LupanariumState _school;
        private readonly BlessingCatalog _blessings;
        private readonly SaveService _save;
        public YandexBridge Bridge { get; private set; }
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public bool AdsVisible => YandexBridge.Enabled;
        public bool IsBase => SceneManager.GetActiveScene().name == "2.Base";
        private bool _banner;
        private float _nextInterstitial;
        public PlatformRewardsService(RunState run, LupanariumState school, BlessingCatalog blessings, SaveService save)
        { _run = run; _school = school; _blessings = blessings; _save = save; }
        public void Start()
        {
            var go = new GameObject("YandexSdkBridge"); UnityEngine.Object.DontDestroyOnLoad(go);
            Bridge = go.AddComponent<YandexBridge>(); Bridge.Tick = Update;
            _nextInterstitial = Time.realtimeSinceStartup + 180;
        }
        private void Update()
        {
            if (IsBase && _run.IsActive && _run.HasWounded) _run.BeginRest(Now);
            bool fighting = !IsBase && _run.Phase == BattleFlowState.Fighting;
            Bridge.Gameplay(fighting);
            bool banner = Bridge.Ready && !Bridge.Paused && (IsBase || SceneManager.GetActiveScene().name == "1.MainMenu");
            if (banner != _banner) { _banner = banner; Bridge.Banner(banner); }
        }
        public BlessingConfig Supply => _blessings.Blessings.Count > 0 ? _blessings.Blessings[0] : null;
        public long Remaining(AdRewardKind kind) => Math.Max(0, _school.RewardReadyAt(kind) - Now);
        public bool CanReward(AdRewardKind kind)
        {
            if (!AdsVisible || Bridge == null || !Bridge.Ready || Bridge.Paused || Remaining(kind) > 0) return false;
            if (kind == AdRewardKind.BattleBonus)
                return !IsBase && _school.PendingBattleBonus > 0 &&
                    (_run.Phase == BattleFlowState.BattleSummary || _run.Phase == BattleFlowState.Defeat || _run.Phase == BattleFlowState.Reward);
            if (!IsBase) return false;
            if (kind == AdRewardKind.Heal) return _run.IsActive && _run.HasWounded;
            return kind != AdRewardKind.Supplies || Supply != null;
        }
        public void Request(AdRewardKind kind)
        {
            if (!CanReward(kind)) return;
            string battleId = _school.BattleBonusId;
            BlessingConfig supply = Supply;
            _save.Save();
            Bridge.Show(true, () =>
            {
                _nextInterstitial = Time.realtimeSinceStartup + 180;
                if (kind == AdRewardKind.BattleBonus) _school.TryClaimBattleBonus(battleId);
                else
                {
                    _school.SetRewardCooldown(kind, Now + (kind == AdRewardKind.Money ? 120 : kind == AdRewardKind.Supplies ? 180 : 300));
                    switch (kind)
                    {
                        case AdRewardKind.Money: _school.AddDenarii(50); break;
                        case AdRewardKind.Supplies: _school.AddBlessingCharge(supply); break;
                        case AdRewardKind.Heal: _run.HealSquad(); break;
                    }
                }
                _save.Save();
            });
        }
        // Called by the user's Continue action at a battle summary, never by a repeating timer.
        public void AtBreak(Action continueAction)
        {
            if (Bridge != null && Bridge.Paused) return;
            if (Bridge != null && AdsVisible && Time.realtimeSinceStartup >= _nextInterstitial)
            {
                _save.Save(); _nextInterstitial = Time.realtimeSinceStartup + 180;
                if (Bridge.Show(false, null, continueAction)) return;
            }
            continueAction();
        }
        public void Dispose() { if (Bridge != null) UnityEngine.Object.Destroy(Bridge.gameObject); }
    }
}
