using System;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Code.Tests
{
    public sealed class HealthAndPlatformTests
    {
        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Configs/"+path+".asset");
        private static UnitRuntime Unit(int id, string name = "Murmillo")
        {
            var config = Load<UnitConfig>("Gladiators/"+name);
            return new UnitRuntime(id, TeamId.Player, Vector2.zero, new UnitDefinition(config.Id, config.ClassId,
                new UnitStatsBuilder().Build(config, new ModifierSet()), 0, default));
        }
        private static RunState Run()
        {
            var run = new RunState(Load<RunConfig>("Run/RunConfig"));
            run.SetStartingSquad(Load<UnitConfig>("Gladiators/Murmillo"), 2);
            run.SetProgress(BattleFlowState.ContractSelection, Array.Empty<UpgradeConfig>(), Array.Empty<ContractOffer>());
            return run;
        }
        private static RunSaveCodec Codec() => new RunSaveCodec(Load<RunConfig>("Run/RunConfig"), Load<RosterCatalog>("Run/RosterCatalog"),
            Load<UpgradeCatalog>("Upgrades/UpgradeCatalog"), Load<FormationCatalog>("Formations/FormationCatalog"), Load<ContractCatalog>("Wave/ContractCatalog"));

        [Test] public void IndividualHealthAndDownedUnitsSurviveJsonReload()
        {
            var run=Run(); var a=Unit(0); var b=Unit(1);
            a.ApplyDamage(a.Stats.MaxHealth / 2); b.MarkDead();
            run.CaptureHealth(new[]{a,b}); run.BeginRest(1000);
            var snapshot=JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(Codec().Capture(run)));
            var resumed=Run(); Codec().Restore(resumed,snapshot);
            var restored=new[]{Unit(0),Unit(1)}; resumed.ApplyHealth(restored);
            Assert.That(restored[0].CurrentHealth,Is.EqualTo(a.CurrentHealth));
            Assert.That(restored[1].IsAlive,Is.False);
            Assert.That(resumed.RestReadyAt,Is.EqualTo(1120));
            Assert.That(resumed.TryFinishRest(1119),Is.False);
            Assert.That(resumed.TryFinishRest(1120),Is.True);
            Assert.That(resumed.HasWounded,Is.False);
        }
        [Test] public void ReinforcementsDoNotShiftOtherClassInjuries()
        {
            var run=Run(); var a=Unit(0); var b=Unit(1,"Secutor"); b.ApplyDamage(100);
            run.CaptureHealth(new[]{a,b});
            var next=new[]{Unit(0),Unit(1),Unit(2,"Secutor")};run.ApplyHealth(next);
            Assert.That(next[1].HealthNormalized,Is.EqualTo(1));
            Assert.That(next[2].CurrentHealth,Is.EqualTo(b.CurrentHealth).Within(.001));
        }
        [Test] public void InProgressBattleCannotOverwritePreBattleHealthSnapshot()
        {
            var run=Run();var original=Unit(0);original.ApplyDamage(100);run.CaptureHealth(new[]{original});
            run.BeginRest(1000);
            run.SetProgress(BattleFlowState.Fighting,Array.Empty<UpgradeConfig>(),Array.Empty<ContractOffer>());
            Assert.That(run.TryFinishRest(1120),Is.False);
            var live=Unit(0);run.ApplyHealth(new[]{live});live.ApplyDamage(200);
            run.HealSquad();
            var saved=Codec().Capture(run);
            Assert.That(saved.Health[0].Fraction,Is.EqualTo(original.HealthNormalized));
            Assert.That(saved.Phase,Is.EqualTo(BattleFlowState.Preparation));
        }
        [Test] public void OldSaveWithoutHealthStartsHealthyAndPermanentProgressMigrates()
        {
            var data=new GameSaveData {Version=5,Denarii=123};
            Assert.That((bool)typeof(SaveService).GetMethod("TryMigrate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[]{data}),Is.True);
            Assert.That(data.Denarii,Is.EqualTo(123));
            var run=Run();run.RestoreHealth(null,0);Assert.That(run.GetHealthFraction("any",0),Is.EqualTo(1));
        }
        [Test] public void RewardCooldownsAndBonusClaimAreSavedAndCannotBeRepeated()
        {
            var school=new LupanariumState();school.SetRewardCooldown(AdRewardKind.Money,1120);school.SetBattleBonus(42);
            string id=school.BattleBonusId;var data=new GameSaveData();school.CaptureTo(data);
            var loaded=new LupanariumState();loaded.RestoreFrom(JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data)));
            Assert.That(loaded.RewardReadyAt(AdRewardKind.Money),Is.EqualTo(1120));
            Assert.That(loaded.TryClaimBattleBonus(id),Is.True);Assert.That(loaded.TryClaimBattleBonus(id),Is.False);
            Assert.That(loaded.Denarii,Is.EqualTo(42));
            loaded.SetBattleBonus(50);Assert.That(loaded.TryClaimBattleBonus(id),Is.False);
            loaded.Reset();Assert.That(loaded.PendingBattleBonus,Is.Zero);Assert.That(loaded.RewardReadyAt(AdRewardKind.Money),Is.Zero);
        }
        [Test] public void SdkPausePreservesPreviousTimeScaleAndAudioState()
        {
            float before=Time.timeScale;bool audio=AudioListener.pause;
            var go=new GameObject("TestBridge");var bridge=go.AddComponent<YandexBridge>();
            try {
                Time.timeScale=.5f;AudioListener.pause=true;
                bridge.OnPlatformPause("1");bridge.OnPageFocus("0");
                Assert.That(Time.timeScale,Is.Zero);
                bridge.OnPlatformPause("0");Assert.That(Time.timeScale,Is.Zero);
                bridge.OnAdVisibility("1");
                bridge.OnPageFocus("1");Assert.That(Time.timeScale,Is.Zero);
                bridge.OnAdVisibility("0");
                bridge.OnPageFocus("1");Assert.That(Time.timeScale,Is.EqualTo(.5f));Assert.That(AudioListener.pause,Is.True);
            } finally {UnityEngine.Object.DestroyImmediate(go);Time.timeScale=before;AudioListener.pause=audio;}
        }
    }
}
