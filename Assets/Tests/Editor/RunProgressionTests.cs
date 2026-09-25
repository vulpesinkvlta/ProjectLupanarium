using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Tests
{
    public sealed class RunProgressionTests
    {
        private readonly List<Object> _objects = new();
        private readonly List<BattleFlowController> _flows = new();
        private readonly List<UnitViewPool> _pools = new();
        private RunConfig _config;
        private UnitConfig _unit;
        private UpgradeConfig _reinforcement;
        private FormationConfig _formation;
        private RosterCatalog _roster;
        private UpgradeCatalog _upgrades;
        private FormationCatalog _formations;
        private ContractCatalog _contracts;
        private ContractOffer _offer;
        private RunState _run;
        private LupanariumState _school;
        private RunSaveCodec _codec;
        private MemoryStorage _storage;
        private FakeSceneLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _config = Asset<RunConfig>();
            Set(_config, "_startingGold", 25);
            _unit = Asset<UnitConfig>();
            Set(_unit, "_id", "starter");
            Set(_config, "_startingSquad", new[] { new SquadEntry(_unit, 5) });
            _reinforcement = Asset<UpgradeConfig>();
            Set(_reinforcement, "_id", "reinforcement");
            Set(_reinforcement, "_unitToAdd", _unit);
            Set(_reinforcement, "_unitAddCount", 3);
            _formation = Asset<FormationConfig>();
            Set(_formation, "_id", "line");
            Set(_config, "_defaultFormation", _formation);
            var entry = new RosterEntry();
            Set(entry, "_unit", _unit);
            Set(entry, "_unlockedFromStart", true);
            _roster = Asset<RosterCatalog>();
            Set(_roster, "_entries", new[] { entry });
            _upgrades = Asset<UpgradeCatalog>();
            Set(_upgrades, "_upgrades", new[] { _reinforcement });
            _formations = Asset<FormationCatalog>();
            var contract = Asset<ContractConfig>();
            Set(contract, "_id", "contract");
            Set(contract, "_enemies", new[] { new SquadEntry(_unit, 2) });
            _contracts = Asset<ContractCatalog>();
            Set(_contracts, "_contracts", new[] { contract });
            _offer = new ContractOffer();
            _offer.Set(contract, 90, new[] { new SquadEntry(_unit, 17) });
            _run = new RunState(_config);
            _school = new LupanariumState();
            _codec = new RunSaveCodec(_config, _roster, _upgrades, _formations, _contracts);
            _storage = new MemoryStorage();
            _loader = new FakeSceneLoader();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (BattleFlowController flow in _flows)
                flow.Dispose();
            foreach (UnitViewPool pool in _pools)
                pool.Dispose();
            foreach (Object item in _objects)
                Object.DestroyImmediate(item);
            _flows.Clear();
            _pools.Clear();
            _objects.Clear();
        }

        [TestCase(BattleFlowState.ContractSelection)]
        [TestCase(BattleFlowState.FormationSelection)]
        [TestCase(BattleFlowState.Preparation)]
        [TestCase(BattleFlowState.BattleSummary)]
        [TestCase(BattleFlowState.Reward)]
        public void BaseVisitAndArenaRecreationPreserveRoundTen(BattleFlowState phase)
        {
            PrepareRoundTen(phase);
            BattleFlowController flow = Flow();
            flow.Start();
            flow.ReturnToLupanarium();
            int banked = _school.Denarii;
            Assert.That(banked, Is.EqualTo(225));
            Assert.That(_loader.LastScene, Is.EqualTo(GameScene.Base));

            new LupanariumController(_school, _run, _loader).StartRun();
            Assert.That(_loader.LastScene, Is.EqualTo(GameScene.Arena));
            flow.Dispose();
            BattleFlowController resumed = Flow();
            resumed.Start();

            Assert.That(_run.WaveNumber, Is.EqualTo(10));
            Assert.That(_run.TotalUnitCount, Is.EqualTo(8));
            Assert.That(_run.AcquiredUpgrades.Count, Is.EqualTo(1));
            Assert.That(_run.Gold, Is.Zero);
            Assert.That(resumed.State, Is.EqualTo(phase == BattleFlowState.BattleSummary
                ? BattleFlowState.Reward : phase));
            Assert.That(resumed.ContractOffers[0].TotalEnemyCount, Is.EqualTo(17));
            resumed.ReturnToLupanarium();
            Assert.That(_school.Denarii, Is.EqualTo(banked), "Повторный визит не дублирует деньги.");
        }

        [Test]
        public void JsonReloadPreservesRosterUpgradesAndExactOffers()
        {
            PrepareRoundTen(BattleFlowState.Reward);
            Service().Save();
            _run = new RunState(_config);
            _school = new LupanariumState();
            Assert.That(Service().TryLoad(), Is.True);
            Assert.That(_run.WaveNumber, Is.EqualTo(10));
            Assert.That(_run.Gold, Is.EqualTo(225));
            Assert.That(_run.TotalUnitCount, Is.EqualTo(8), "Пополнение не применяется повторно.");
            Assert.That(_run.AcquiredUpgrades.Single(), Is.SameAs(_reinforcement));
            Assert.That(_run.RewardChoices.Single(), Is.SameAs(_reinforcement));
            Assert.That(_run.SelectedFormation, Is.SameAs(_formation));
            Assert.That(_run.ActiveContract.TotalEnemyCount, Is.EqualTo(17));
            Assert.That(_run.ActiveContract.GoldReward, Is.EqualTo(90));
            Assert.That(_run.ContractOffers.Single().TotalEnemyCount, Is.EqualTo(17));
        }

        [Test]
        public void PendingRewardAfterReloadAdvancesExactlyOnce()
        {
            PrepareRoundTen(BattleFlowState.BattleSummary);
            Service().Save();
            _run = new RunState(_config);
            Service().TryLoad();
            BattleFlowController flow = Flow();
            flow.Start();
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Reward));
            Assert.That(_run.Gold, Is.EqualTo(225), "Победа не оплачивается заново.");
            flow.SelectUpgrade(0);
            flow.SelectUpgrade(0);
            Assert.That(_run.WaveNumber, Is.EqualTo(11));
            Assert.That(_run.TotalUnitCount, Is.EqualTo(11));
            Assert.That(_run.AcquiredUpgrades.Count, Is.EqualTo(2));
        }

        [TestCase(BattleResult.EnemyVictory)]
        [TestCase(BattleResult.Draw)]
        public void DefeatEndsOnlyRunAndIncludesLastTickGold(BattleResult result)
        {
            PrepareRoundTen(BattleFlowState.Preparation);
            _school.AddDenarii(500);
            BattleFlowController flow = Flow();
            flow.Start();
            Invoke(flow, "SetState", BattleFlowState.Fighting);
            Invoke(flow, "OnBattleCompleted", result);
            _run.AddGold(7); // UnitCleanupSystem завершает последний тик.
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Fighting));
            flow.LateTick();
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.Defeat));
            Assert.That(_run.IsActive, Is.False);
            Assert.That(_school.Denarii, Is.EqualTo(732));
            Assert.That(_school.BestRoundReached, Is.EqualTo(10));
            Service().Save();
            Assert.That(JsonUtility.FromJson<GameSaveData>(_storage.Payload).HasActiveRun, Is.False);
            _run = new RunState(_config);
            Service().TryLoad();
            Assert.That(_run.WaveNumber, Is.EqualTo(1));
            Assert.That(_school.Denarii, Is.EqualTo(732));
        }

        [Test]
        public void VictoryIncludesLastTickGoldAndCannotPayTwice()
        {
            PrepareRoundTen(BattleFlowState.Preparation);
            BattleFlowController flow = Flow();
            flow.Start();
            Invoke(flow, "SetState", BattleFlowState.Fighting);
            Invoke(flow, "OnBattleCompleted", BattleResult.PlayerVictory);
            _run.AddGold(7);
            flow.LateTick();
            flow.LateTick();
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.BattleSummary));
            Assert.That(_run.Gold, Is.EqualTo(322));
            Assert.That(flow.CurrentChoices.Single(), Is.SameAs(_reinforcement));
        }

        [Test]
        public void InterruptedBattleReloadsPreparationWithoutPartialKillGold()
        {
            PrepareRoundTen(BattleFlowState.Preparation);
            _run.SetProgress(BattleFlowState.Fighting, Array.Empty<UpgradeConfig>(), new[] { _offer });
            _run.AddGold(17);
            Service().Save();
            _run = new RunState(_config);
            Service().TryLoad();
            Assert.That(_run.Phase, Is.EqualTo(BattleFlowState.Preparation));
            Assert.That(_run.Gold, Is.EqualTo(225));
            Assert.That(_run.WaveNumber, Is.EqualTo(10));
        }

        [Test]
        public void FailedBaseLoadAndRestartCannotEraseActiveRun()
        {
            PrepareRoundTen(BattleFlowState.ContractSelection);
            _loader.Succeeds = false;
            BattleFlowController flow = Flow();
            flow.Start();
            flow.ReturnToLupanarium();
            flow.StartRun();
            Assert.That(_run.WaveNumber, Is.EqualTo(10));
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.ContractSelection));
            Assert.That(_run.TotalUnitCount, Is.EqualTo(8));
        }

        [Test]
        public void BattleCannotBeLeftForBase()
        {
            PrepareRoundTen(BattleFlowState.Preparation);
            BattleFlowController flow = Flow();
            flow.Start();
            Invoke(flow, "SetState", BattleFlowState.Fighting);
            flow.ReturnToLupanarium();
            Assert.That(_loader.LoadCount, Is.Zero);
            Assert.That(_run.Gold, Is.EqualTo(225));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void OldSavesPreservePermanentProgress(int version)
        {
            _storage.Payload = "{\"Version\":" + version + ",\"Denarii\":1234," +
                "\"BuildingIds\":[\"school\"],\"BuildingLevels\":[3]," +
                "\"OwnedItemIds\":[\"sword\"],\"UnlockedUnitIds\":[\"archer\"]," +
                "\"UnlockedFormationIds\":[\"line\"],\"BestRoundReached\":10}";
            Assert.That(Service().TryLoad(), Is.True);
            Service().Save();
            GameSaveData saved = JsonUtility.FromJson<GameSaveData>(_storage.Payload);
            Assert.That(saved.Version, Is.EqualTo(4));
            Assert.That(saved.Denarii, Is.EqualTo(1234));
            Assert.That(saved.BuildingLevels, Is.EqualTo(new[] { 3 }));
            Assert.That(saved.OwnedItemIds, Is.EqualTo(new[] { "sword" }));
            Assert.That(saved.UnlockedUnitIds, Is.EqualTo(new[] { "archer" }));
            Assert.That(saved.UnlockedFormationIds, Is.EqualTo(new[] { "line" }));
            Assert.That(saved.HasActiveRun, Is.False);
        }

        [Test]
        public void AutosaveKeepsBankTransferAndRunTogether()
        {
            using var runner = new SaveRunner(Service(), _school, _run);
            runner.Start();
            PrepareRoundTen(BattleFlowState.ContractSelection);
            BattleFlowController flow = Flow();
            flow.Start();
            flow.ReturnToLupanarium();
            GameSaveData saved = JsonUtility.FromJson<GameSaveData>(_storage.Payload);
            Assert.That(saved.Denarii, Is.EqualTo(225));
            Assert.That(saved.ActiveRun.Gold, Is.Zero);
            Assert.That(saved.ActiveRun.WaveIndex, Is.EqualTo(9));
            Assert.That(saved.BestRoundReached, Is.EqualTo(10));
        }

        [Test]
        public void FirstRunAndPostDefeatRestartOfferStartingRoster()
        {
            BattleFlowController flow = Flow();
            flow.Start();
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.SquadSelection));
            flow.SelectStartingSquad(0);
            Assert.That(_run.TotalUnitCount, Is.EqualTo(5));
            _run.EndRun();
            Invoke(flow, "SetState", BattleFlowState.Defeat);
            flow.StartRun();
            Assert.That(flow.State, Is.EqualTo(BattleFlowState.SquadSelection));
            Assert.That(_run.WaveNumber, Is.EqualTo(1));
            Assert.That(_run.TotalUnitCount, Is.Zero);
            Assert.That(_run.Gold, Is.EqualTo(25));
        }

        private void PrepareRoundTen(BattleFlowState phase)
        {
            _run.SetStartingSquad(_unit, 5);
            _run.AddUpgrade(_reinforcement);
            _run.SelectFormation(_formation);
            _run.AcceptContract(_offer);
            _run.AddGold(200);
            for (var i = 0; i < 9; i++)
                _run.AdvanceWave();
            _run.SetProgress(phase, new[] { _reinforcement }, new[] { _offer });
        }

        private SaveService Service() => new(_storage, _school, _run, _codec);

        private BattleFlowController Flow()
        {
            // Настоящие системы и ClearArena, отдельные от текущей сцены.
            var go = new GameObject("RunProgressionTests");
            _objects.Add(go);
            var cache = new Dictionary<Type, object>
            {
                [typeof(RunState)] = _run,
                [typeof(ArenaBounds)] = new ArenaBounds(Vector2.zero, 8),
                [typeof(ArenaSceneReference)] = go.AddComponent<ArenaSceneReference>(),
                [typeof(IUnitModifierSource)] = new EmptyUnitModifierSource()
            };
            object Resolve(Type type)
            {
                if (cache.TryGetValue(type, out object found))
                    return found;
                ConstructorInfo ctor = type.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length).First();
                object instance = ctor.Invoke(ctor.GetParameters()
                    .Select(p => Resolve(p.ParameterType)).ToArray());
                cache[type] = instance;
                return instance;
            }
            var battle = (BattleController)Resolve(typeof(BattleController));
            _pools.Add((UnitViewPool)cache[typeof(UnitViewPool)]);
            var flow = new BattleFlowController(
                (VictorySystem)Resolve(typeof(VictorySystem)), battle, _run,
                new UpgradeDrafter(_upgrades), new ContractDrafter(_contracts),
                _config, _formations, _school, _loader,
                (BattleStatistics)Resolve(typeof(BattleStatistics)), _roster);
            _flows.Add(flow);
            return flow;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, args);

        private sealed class MemoryStorage : ISaveStorage
        {
            public string Payload;
            public bool TryRead(string key, out string payload)
            {
                payload = Payload;
                return payload != null;
            }
            public void Write(string key, string payload) => Payload = payload;
            public void Delete(string key) => Payload = null;
        }

        private sealed class FakeSceneLoader : IGameSceneLoader
        {
            public bool Succeeds = true;
            public GameScene LastScene;
            public int LoadCount;
            public bool TryLoad(GameScene scene)
            {
                LastScene = scene;
                LoadCount++;
                return Succeeds;
            }
        }
    }
}
