using System.Collections.Generic;
using System.Linq;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace Code.Tests
{
    public sealed class BalanceRegressionTests
    {
        private static ContractCatalog Catalog => AssetDatabase.LoadAssetAtPath<ContractCatalog>("Assets/Configs/Wave/ContractCatalog.asset");

        [Test]
        public void EnemyGrowthIsMonotonicAndBounded()
        {
            int previous = 0;
            for (int round = 1; round <= 1000; round++)
            {
                int count = Catalog.ScaleCount(4, round);
                Assert.That(count, Is.InRange(previous, 256));
                previous = count;
            }
            Assert.That(Catalog.ScaleCount(4, int.MaxValue), Is.EqualTo(256));
        }

        [Test]
        public void TrainingContractExpiresAndCatalogIdsAreUnique()
        {
            var catalog = Catalog;
            Assert.That(catalog.Contracts.All(c => c != null && !string.IsNullOrWhiteSpace(c.Id)), Is.True);
            Assert.That(catalog.Contracts.Select(c => c.Id).Distinct().Count(), Is.EqualTo(catalog.Contracts.Count));
            var draft = new ContractDrafter(catalog);
            var offers = new List<ContractOffer>();
            draft.Draw(1, 100, offers);
            Assert.That(offers.Any(o => o.Source != null && o.Source.MaximumRound == 3), Is.True);
            draft.Draw(4, 100, offers);
            Assert.That(offers.Where(o => o.Source != null).All(o => o.Source.MaximumRound == 0 || o.Source.MaximumRound >= 4), Is.True);
        }

        [TestCase(1)]
        [TestCase(8)]
        public void FreshMixedRewardCampaignFitsTarget(int seed)
        {
            var result = BalanceCampaignProbe.Run(seed);
            Assert.That(result.timedOut, Is.False);
            Assert.That(result.estimatedMinutes, Is.InRange(20f, 30f));
            Assert.That(result.defeatRound, Is.InRange(18, 26));
        }
    }
}
