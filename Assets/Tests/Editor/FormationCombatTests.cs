using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Code.Tests
{
    public sealed class FormationCombatTests
    {
        private static readonly string[] FormationNames = { "None", "Column", "Circle", "Testudo", "Swine", "Falang", "Manipula" };
        public static IEnumerable Cases()
        {
            foreach (string formation in FormationNames)
                foreach (int count in new[] { 1, 14, 40 })
                    foreach (bool edge in new[] { false, true })
                        yield return new TestCaseData(formation, count, edge);
        }

        public static IEnumerable Matchups()
        {
            foreach (string player in FormationNames)
                foreach (string enemy in FormationNames)
                    yield return new TestCaseData(player, enemy);
        }

        [TestCaseSource(nameof(Matchups))]
        public void EveryFormationPairFinishesBattle(string player, string enemy)
        {
            var w = new World();
            var unitConfig = AssetDatabase.LoadAssetAtPath<UnitConfig>("Assets/Configs/Gladiators/Murmillo.asset");
            int id = 0;
            void Spawn(string name, TeamId team, float sign)
            {
                var config = name == "None" ? null : AssetDatabase.LoadAssetAtPath<FormationConfig>($"Assets/Configs/Formations/{name}.asset");
                var registry = w.Get<FormationRegistry>();
                registry.Setup(team, config, new Vector2(-2 * sign, 0), sign);
                var formation = registry.Get(team);
                var slots = new List<Vector2>();
                new FormationSolver().BuildSlots(formation.Shape, 14, slots);
                var mods = new ModifierSet();
                mods.AddRange(formation.Modifiers);
                var stats = new UnitStatsBuilder().Build(unitConfig, mods);
                foreach (var slot in slots)
                {
                    var u = new UnitRuntime(id++, team, w.Get<ArenaBounds>().Clamp(formation.SlotToWorld(slot), stats.Radius),
                        new UnitDefinition("test", UnitClassId.Murmillo, stats, 12, default));
                    if (config != null) u.AssignFormationSlot(slot);
                    w.Get<ArenaContext>().AddUnit(u);
                }
            }
            Spawn(player, TeamId.Player, 1);
            Spawn(enemy, TeamId.Enemy, -1);
            w.Run(120);
            Assert.That(w.Get<VictorySystem>().Result, Is.Not.EqualTo(BattleResult.None),
                string.Join("; ", w.Get<ArenaContext>().AllUnits.Select(u => $"{u.Id}/{u.Team}: hp={u.CurrentHealth},target={u.Target?.Id},distance={(u.Target == null ? -1 : Vector2.Distance(u.Position, u.Target.Position))},range={u.Stats.AttackRange},windup={u.RemainingWindup},formation={u.HoldsFormation}")));
            foreach (var unit in w.Get<ArenaContext>().AllUnits)
            {
                Assert.That(float.IsNaN(unit.Position.x) || float.IsNaN(unit.Position.y), Is.False);
                Assert.That(unit.Position.magnitude, Is.LessThanOrEqualTo(8.001f));
            }
        }

        [TestCase(.8500001f, true)]
        [TestCase(.86f, false)]
        public void AttackAllowsRoundingErrorButNotAnOutOfRangeTarget(float distance, bool expected)
        {
            var w = new World();
            var attacker = Unit(0, TeamId.Player, Vector2.zero);
            var target = Unit(1, TeamId.Enemy, new Vector2(distance, 0));
            attacker.SetTarget(target);
            w.Get<ArenaContext>().AddUnit(attacker); w.Get<ArenaContext>().AddUnit(target);
            for (int i = 0; i < 20; i++) w.Get<AttackSystem>().Tick(1f / 30);
            Assert.That(w.Get<DamageBuffer>().Requests.Count > 0, Is.EqualTo(expected));
        }

        [TestCase(TeamId.Player, 1f)]
        [TestCase(TeamId.Enemy, -1f)]
        public void FlankingEnemyReleasesFormationForBothTeams(TeamId team, float sign)
        {
            var w = new World();
            var config = AssetDatabase.LoadAssetAtPath<FormationConfig>("Assets/Configs/Formations/Column.asset");
            w.Get<FormationRegistry>().Setup(team, config, new Vector2(sign, 0), sign);
            var unit = Unit(0, team, Vector2.zero);
            var target = Unit(1, team == TeamId.Player ? TeamId.Enemy : TeamId.Player, new Vector2(0, 7));
            unit.AssignFormationSlot(Vector2.zero); unit.SetTarget(target);
            w.Get<ArenaContext>().AddUnit(unit); w.Get<ArenaContext>().AddUnit(target);
            w.Get<MovementSystem>().Tick(1f / 30);
            Assert.That(unit.HoldsFormation, Is.False);
            Assert.That(unit.Position.y, Is.GreaterThan(0));
        }

        private sealed class World
        {
            private readonly Dictionary<Type, object> _objects = new();
            public World() { _objects[typeof(ArenaBounds)] = new ArenaBounds(Vector2.zero, 8); }
            public T Get<T>() => (T)Resolve(typeof(T));
            private object Resolve(Type type)
            {
                if (_objects.TryGetValue(type, out var found)) return found;
                var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
                var value = ctor.Invoke(ctor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray());
                _objects[type] = value;
                return value;
            }
            public void Run(float seconds)
            {
                var sim = Get<ArenaSimulation>(); sim.Start();
                for (int i = 0; i < seconds * 30 && sim.IsRunning; i++)
                {
                    sim.Tick(1f / 30);
                    Get<BattleFeedbackQueue>().Clear(); Get<DeadViewQueue>().Clear();
                }
            }
        }

        private static UnitRuntime Unit(int id, TeamId team, Vector2 position, float health = 100)
        {
            return new UnitRuntime(id, team, position, new UnitDefinition("test", UnitClassId.Murmillo,
                new UnitStats(health, 2.4f, 26, .85f, .95f, .4f, 5, 2, 0, .5f, 2), 12, default));
        }

        [TestCaseSource(nameof(Cases))]
        public void FormationCanFinishLastEnemy(string name, int count, bool edge)
        {
            var w = new World();
            var context = w.Get<ArenaContext>();
            var bounds = w.Get<ArenaBounds>();
            var config = name == "None" ? null : AssetDatabase.LoadAssetAtPath<FormationConfig>($"Assets/Configs/Formations/{name}.asset");
            if (name != "None") Assert.That(config, Is.Not.Null);
            var formation = w.Get<FormationRegistry>();
            formation.Setup(TeamId.Player, config, new Vector2(-2, 0), 1);
            formation.Setup(TeamId.Enemy, null, new Vector2(2, 0), -1);
            var slots = new List<Vector2>();
            new FormationSolver().BuildSlots(config != null ? config.Shape : new FormationShape(FormationLayout.Box, 1, 1, .85f, .85f), count, slots);
            for (int i = 0; i < count; i++)
            {
                var u = Unit(i, TeamId.Player, bounds.Clamp(new Vector2(-2, 0) + slots[i], .4f), 1000);
                if (config != null) u.AssignFormationSlot(slots[i]);
                context.AddUnit(u);
            }
            var enemy = Unit(count, TeamId.Enemy, edge ? new Vector2(0, 7.6f) : new Vector2(2, 0));
            context.AddUnit(enemy);
            w.Run(90);
            Assert.That(w.Get<VictorySystem>().Result, Is.EqualTo(BattleResult.PlayerVictory),
                string.Join("; ", context.AllUnits.Select(u => $"{u.Id}: HP={u.CurrentHealth}, formation={u.HoldsFormation}, distance={(u.Target == null ? -1 : Vector2.Distance(u.Position, u.Target.Position))}")));
        }

        [Test]
        public void ContactAtDiagonalDoesNotLoseEveryWindup()
        {
            var w = new World();
            w.Get<ArenaContext>().AddUnit(Unit(0, TeamId.Player, new Vector2(-1.53f, -1.22f), 1000));
            w.Get<ArenaContext>().AddUnit(Unit(1, TeamId.Enemy, new Vector2(-.93f, -.62f)));
            w.Run(20);
            Assert.That(w.Get<VictorySystem>().Result, Is.EqualTo(BattleResult.PlayerVictory));
        }
    }
}
