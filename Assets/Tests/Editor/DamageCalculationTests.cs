using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Code.Tests
{
    public sealed class DamageCalculationTests
    {
        [TestCase(0f, 8f)]
        [TestCase(15.5f, 1f)]
        [TestCase(25f, 1f)]
        public void WolfHitUsesFinalMurmilloArmorAndUpdatesPlayerHealth(float bonusArmor, float expected)
        {
            var builder = new UnitStatsBuilder();
            var modifiers = new ModifierSet();
            modifiers.Add(new StatModifier(StatId.Armor, ModType.Flat, bonusArmor));
            var defender = new UnitRuntime(0, TeamId.Player, Vector2.zero, new UnitDefinition("murmillo", UnitClassId.Murmillo,
                builder.Build(AssetDatabase.LoadAssetAtPath<UnitConfig>("Assets/Configs/Gladiators/Murmillo.asset"), modifiers), 0, default));
            var attacker = new UnitRuntime(1, TeamId.Enemy, Vector2.right, new UnitDefinition("wolf", UnitClassId.Wolf,
                builder.Build(AssetDatabase.LoadAssetAtPath<UnitConfig>("Assets/Configs/Gladiators/Wolf.asset"), new ModifierSet()), 0, default));
            var buffer = new DamageBuffer(); var feedback = new BattleFeedbackQueue();
            var dirty = new BattleHudDirtyTracker(); var stats = new BattleStatistics();
            float before = defender.CurrentHealth;
            buffer.Add(new DamageRequest(attacker, defender, attacker.Stats.AttackDamage, false));
            new DamageSystem(buffer, dirty, feedback, stats).Tick();
            Assert.That(before - defender.CurrentHealth, Is.EqualTo(expected));
            Assert.That(feedback.Events[0].Amount, Is.EqualTo(expected));
            Assert.That(stats.GetPlayerStats(UnitClassId.Murmillo).DamageTaken, Is.EqualTo(expected));
            Assert.That(dirty.HealthVersion, Is.GreaterThan(0));
        }
    }
}
