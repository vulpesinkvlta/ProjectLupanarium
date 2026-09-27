using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Code.Tests
{
    // Uses the actual fixed-tick combat systems; no scene, views or player saves.
    public static class BalanceCampaignProbe
    {
        [Serializable] public sealed class Result
        {
            public int seed, defeatRound, gold, squad;
            public string starter, policy;
            public float battleSeconds, estimatedMinutes, longestBattle;
            public bool timedOut;
        }
        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Configs/" + path + ".asset");

        public static Result Run(int seed, string starter = "Murmillo", string policy = "balanced", int metaLevel = 0)
        {
            var run = new RunState(Load<RunConfig>("Run/RunConfig"));
            run.SetStartingSquad(Load<UnitConfig>("Gladiators/" + starter), 3);
            var contractCatalog = Load<ContractCatalog>("Wave/ContractCatalog");
            var contracts = new ContractDrafter(contractCatalog);
            var upgrades = new UpgradeDrafter(Load<UpgradeCatalog>("Upgrades/UpgradeCatalog"));
            typeof(ContractDrafter).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(contracts, new System.Random(seed));
            typeof(UpgradeDrafter).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(upgrades, new System.Random(seed + 10000));
            var offers = new List<ContractOffer>(); var rewards = new List<UpgradeConfig>();
            var result = new Result { seed = seed, starter = starter, policy = policy };
            for (int round = 1; round <= 60; round++)
            {
                contracts.Draw(round, 2, offers);
                var offer = offers.Where(o => o.Source != null).OrderBy(o => o.Enemies.Sum(e => e.Count * Power(new UnitStatsBuilder().Build(e.Config, new ModifierSet())))).First();
                var world = new World(seed + round * 137);
                int id = 0;
                void Spawn(IReadOnlyList<SquadEntry> squad, TeamId team, float sign)
                {
                    world.Get<FormationRegistry>().Setup(team, null, new Vector2(-1.5f * sign, 0), sign);
                    int count = squad.Sum(e => e.Count);
                    int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
                    int rows = Mathf.CeilToInt(count / (float)columns);
                    int slot = 0;
                    foreach (var entry in squad)
                    {
                        var mods = new ModifierSet();
                        if (team == TeamId.Player)
                        {
                            new RunModifierSource(run).Collect(entry.Config, team, mods);
                            foreach (var b in Load<SchoolCatalog>("Buildings/SchoolCatalog").Buildings)
                                if (b.AppliesTo(entry.Config.ClassId)) b.CollectModifiers(metaLevel, mods);
                        }
                        var stats = new UnitStatsBuilder().Build(entry.Config, mods);
                        for (int n = 0; n < entry.Count; n++)
                        {
                            var pos = new Vector2(-1.5f * sign - sign * (slot % columns) * .8f, (slot / columns) * .8f - (rows - 1) * .4f);
                            slot++;
                            world.Get<ArenaContext>().AddUnit(new UnitRuntime(id++, team, world.Get<ArenaBounds>().Clamp(pos, stats.Radius),
                                new UnitDefinition(entry.Config.Id, entry.Config.ClassId, stats, entry.Config.GoldReward, entry.Config.Ability)));
                        }
                    }
                }
                Spawn(run.Squad, TeamId.Player, 1); Spawn(offer.Enemies, TeamId.Enemy, -1);
                float seconds = world.Run(240);
                result.battleSeconds += seconds;
                result.longestBattle = Mathf.Max(result.longestBattle, seconds);
                var outcome = world.Get<VictorySystem>().Result;
                result.gold += offer.Enemies.Sum(e => e.Count * e.Config.GoldReward) - world.Get<ArenaContext>().EnemyUnits.Where(u => u.IsAlive).Sum(u => u.GoldReward);
                result.defeatRound = round;
                result.squad = run.TotalUnitCount;
                if (outcome != BattleResult.PlayerVictory)
                {
                    result.timedOut = outcome == BattleResult.None;
                    break;
                }
                result.gold += offer.GoldReward;
                upgrades.Draw(2, rewards);
                UpgradeConfig choice;
                if (policy == "recruit") choice = rewards.FirstOrDefault(u => u.AddsUnits) ?? rewards[0];
                else if (policy == "damage") choice = rewards.FirstOrDefault(u => u.Modifiers.Any(m => m.StatId == StatId.AttackDamage)) ?? rewards[0];
                else choice = run.TotalUnitCount < 3 + round * .4f ? rewards.FirstOrDefault(u => u.AddsUnits) : null;
                choice ??= rewards.FirstOrDefault(u => u.Modifiers.Any(m => m.StatId == (round % 2 == 0 ? StatId.MaxHealth : StatId.AttackDamage))) ?? rewards[0];
                run.AddUpgrade(choice);
            }
            result.estimatedMinutes = (result.battleSeconds + result.defeatRound * 20 + 60) / 60;
            return result;
        }

        public static float Power(UnitStats s)
        {
            float damage = (1 - s.CritChance) * Mathf.Max(1, s.AttackDamage - 2) + s.CritChance * Mathf.Max(1, s.AttackDamage * s.CritMultiplier - 2);
            float dps = damage / (s.AttackCooldown + s.AttackWindup + 1f / 30);
            return Mathf.Sqrt(s.MaxHealth * 20 / Mathf.Max(1, 20 - s.Armor) * dps);
        }

        private sealed class World
        {
            private readonly Dictionary<Type, object> _objects = new();
            public World(int seed)
            {
                _objects[typeof(ArenaBounds)] = new ArenaBounds(Vector2.zero, 8);
                Get<BattleRandom>().Reseed(seed);
            }
            public T Get<T>() => (T)Resolve(typeof(T));
            private object Resolve(Type type)
            {
                if (_objects.TryGetValue(type, out var found)) return found;
                var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).First();
                var value = ctor.Invoke(ctor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray());
                _objects[type] = value; return value;
            }
            public float Run(float limit)
            {
                var sim = Get<ArenaSimulation>(); sim.Start(); int ticks = 0;
                for (; ticks < limit * 30 && sim.IsRunning; ticks++)
                {
                    sim.Tick(1f / 30);
                    Get<BattleFeedbackQueue>().Clear(); Get<DeadViewQueue>().Clear();
                }
                return ticks / 30f;
            }
        }
    }
}
