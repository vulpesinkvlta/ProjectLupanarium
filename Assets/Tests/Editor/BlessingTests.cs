using System;
using System.Collections.Generic;
using System.Reflection;
using Code.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Tests
{
    public sealed class BlessingTests
    {
        private readonly List<Object> _assets = new();
        private LupanariumState _school;
        private RunState _run;
        private ArenaContext _arena;
        private BlessingConfig _config;
        private BlessingBattleController _controller;
        private BattleHudDirtyTracker _dirty;
        private UnitRuntime _player, _enemy, _outside;
        private RunConfig _runConfig;

        private T Asset<T>() where T : ScriptableObject
        { var a = ScriptableObject.CreateInstance<T>(); _assets.Add(a); return a; }
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [SetUp]
        public void Setup()
        {
            _config = Asset<BlessingConfig>();
            Set(_config, "_id", "test.blessing"); Set(_config, "_price", 50);
            Set(_config, "_power", 30f); Set(_config, "_radius", 2f);
            Set(_config, "_duration", 5f); Set(_config, "_speedFactor", .5f);
            var catalog = Asset<BlessingCatalog>(); Set(catalog, "_blessings", new[] { _config });
            _school = new LupanariumState(); _school.AddDenarii(150);
            _school.TryBuyBlessing(_config);
            _runConfig = Asset<RunConfig>(); _run = new RunState(_runConfig);
            _run.SetProgress(BattleFlowState.Fighting, Array.Empty<UpgradeConfig>(), Array.Empty<ContractOffer>());
            _arena = new ArenaContext(); _dirty = new BattleHudDirtyTracker();
            var definition = new UnitDefinition("unit", UnitClassId.Murmillo,
                new UnitStats(100, 4, 10, 1, 1, .2f, 2, 5, 0, .1f, 2), 10, default);
            _player = new UnitRuntime(0, TeamId.Player, Vector2.zero, definition);
            _enemy = new UnitRuntime(1, TeamId.Enemy, Vector2.right, definition);
            _outside = new UnitRuntime(2, TeamId.Enemy, Vector2.right * 4, definition);
            _arena.AddUnit(_player); _arena.AddUnit(_enemy); _arena.AddUnit(_outside);
            _controller = new BlessingBattleController(_run, _school, catalog, _arena,
                new ArenaBounds(Vector2.zero, 8), _dirty, new BattleFeedbackQueue(), new BattleStatistics());
        }
        [TearDown] public void Cleanup() { foreach (var a in _assets) Object.DestroyImmediate(a); _assets.Clear(); }

        [Test] public void PurchaseIsAtomicAndCannotOverspend()
        {
            int events = 0; _school.Changed += () => events++;
            Assert.That(_school.TryBuyBlessing(_config), Is.True);
            Assert.That(_school.TryBuyBlessing(_config), Is.True);
            Assert.That(_school.TryBuyBlessing(_config), Is.False);
            Assert.That(_school.Denarii, Is.Zero);
            Assert.That(_school.GetBlessingCharges(_config.Id), Is.EqualTo(3));
            Assert.That(events, Is.EqualTo(2));
        }
        [Test] public void HealHitsBothTeamsInsideCircleAndSpendsExactlyOneCharge()
        {
            _player.ApplyDamage(20); _enemy.ApplyDamage(70); _outside.ApplyDamage(50);
            Assert.That(_controller.TryCast(_config, Vector2.zero), Is.True);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100));
            Assert.That(_enemy.CurrentHealth, Is.EqualTo(60));
            Assert.That(_outside.CurrentHealth, Is.EqualTo(50));
            Assert.That(_school.GetBlessingCharges(_config.Id), Is.Zero);
            Assert.That(_dirty.HealthVersion, Is.EqualTo(1));
            Assert.That(_controller.TryCast(_config, Vector2.zero), Is.False);
        }
        [Test] public void HealCannotResurrectZeroHealthUnit()
        {
            _outside.ApplyDamage(100); _outside.SetPosition(Vector2.zero);
            _controller.TryCast(_config, Vector2.zero);
            _outside.Heal(100);
            Assert.That(_outside.IsAlive, Is.False);
            Assert.That(_outside.CurrentHealth, Is.Zero);
        }
        [Test] public void TarDamagesBothSidesRespectsArmorAndSlowExpires()
        {
            Set(_config, "_effect", BlessingEffect.Tar);
            Assert.That(_controller.TryCast(_config, Vector2.zero), Is.True);
            Assert.That(_player.CurrentHealth, Is.EqualTo(75));
            Assert.That(_enemy.CurrentHealth, Is.EqualTo(75));
            Assert.That(_outside.CurrentHealth, Is.EqualTo(100));
            Assert.That(_player.EffectiveMoveSpeed, Is.EqualTo(2));
            _player.BeginSimulationTick(5);
            Assert.That(_player.EffectiveMoveSpeed, Is.EqualTo(4));
        }
        [Test] public void HasteAffectsBothSidesAndDoesNotPermanentlyChangeStats()
        {
            Set(_config, "_effect", BlessingEffect.Haste); Set(_config, "_speedFactor", 1.5f);
            _controller.TryCast(_config, Vector2.zero);
            Assert.That(_player.EffectiveMoveSpeed, Is.EqualTo(6));
            Assert.That(_enemy.EffectiveMoveSpeed, Is.EqualTo(6));
            _enemy.BeginSimulationTick(5);
            Assert.That(_enemy.EffectiveMoveSpeed, Is.EqualTo(4));
        }
        [TestCase(BattleFlowState.Preparation)]
        [TestCase(BattleFlowState.Reward)]
        [TestCase(BattleFlowState.Defeat)]
        public void CannotCastOutsideBattle(BattleFlowState phase)
        {
            _run.SetProgress(phase, Array.Empty<UpgradeConfig>(), Array.Empty<ContractOffer>());
            Assert.That(_controller.TryCast(_config, Vector2.zero), Is.False);
            Assert.That(_school.GetBlessingCharges(_config.Id), Is.EqualTo(1));
        }
        [Test] public void InvalidPointOrUnknownConfigDoesNotSpendCharge()
        {
            Assert.That(_controller.TryCast(_config, Vector2.one * 99), Is.False);
            Assert.That(_controller.TryCast(_config, new Vector2(float.NaN, 0)), Is.False);
            Assert.That(_controller.TryCast(Asset<BlessingConfig>(), Vector2.zero), Is.False);
            Assert.That(_school.GetBlessingCharges(_config.Id), Is.EqualTo(1));
        }
        [Test] public void LastKillPreventsFurtherSpendingAndGoesThroughDeathSystem()
        {
            Set(_config, "_effect", BlessingEffect.Tar); Set(_config, "_power", 500f);
            _outside.MarkDead();
            _controller.TryCast(_config, Vector2.zero);
            var deaths = new UnitDeathBuffer();
            new DeathSystem(_arena, deaths, _dirty).Tick();
            Assert.That(_player.State, Is.EqualTo(UnitState.Dead));
            Assert.That(_enemy.State, Is.EqualTo(UnitState.Dead));
            _school.TryBuyBlessing(_config);
            Assert.That(_controller.TryCast(_config, Vector2.zero), Is.False);
            Assert.That(_school.GetBlessingCharges(_config.Id), Is.EqualTo(1));
        }
        [Test] public void ChargesSurviveDefeatAndConsumptionAutosavesWithoutRefundOnReload()
        {
            var codec = new RunSaveCodec(_runConfig, Asset<RosterCatalog>(), Asset<UpgradeCatalog>(),
                Asset<FormationCatalog>(), Asset<ContractCatalog>());
            var storage = new MemoryStorage(); var save = new SaveService(storage, _school, _run, codec);
            using (var runner = new SaveRunner(save, _school, _run))
            {
                runner.Start();
                _school.TryBuyBlessing(_config);
                _run.EndRun(); save.Save();
                Assert.That(save.TryLoad(), Is.True);
                Assert.That(_school.GetBlessingCharges(_config.Id), Is.EqualTo(2));
                _school.TryConsumeBlessing(_config.Id);
                var restored = new LupanariumState();
                Assert.That(new SaveService(storage, restored, new RunState(_runConfig), codec).TryLoad(), Is.True);
                Assert.That(restored.GetBlessingCharges(_config.Id), Is.EqualTo(1));
            }
        }

        [Test] public void ArenaViewAndCatalogHaveAllRequiredReferences()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/3.Arena.unity");
            try
            {
                BlessingBattleView view = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var candidate = root.GetComponentInChildren<BlessingBattleView>(true);
                    if (candidate != null) view = candidate;
                }
                Assert.That(view, Is.Not.Null);
                var serialized = new UnityEditor.SerializedObject(view);
                foreach (string field in new[] { "_root", "_cardsRoot", "_cardPrefab", "_hint", "_cancelButton" })
                    Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var panel = (GameObject)serialized.FindProperty("_root").objectReferenceValue;
                Assert.That(panel.activeSelf, Is.False);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ProjectLifetimeScope.prefab");
                var catalog = (BlessingCatalog)new UnityEditor.SerializedObject(prefab.GetComponent<ProjectLifetimeScope>())
                    .FindProperty("_blessingCatalog").objectReferenceValue;
                Assert.That(catalog, Is.Not.Null);
                Assert.That(catalog.Blessings.Count, Is.EqualTo(3));
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }
        private sealed class MemoryStorage : ISaveStorage
        {
            private string _payload;
            public bool TryRead(string key, out string payload) { payload = _payload; return payload != null; }
            public void Write(string key, string payload) => _payload = payload;
            public void Delete(string key) => _payload = null;
        }
    }
}
