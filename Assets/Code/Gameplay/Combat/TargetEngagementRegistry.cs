using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Gameplay
{
    public sealed class TargetEngagementRegistry
    {
        private const int MinimumCapacity = 16;

        private int[] _claimCounts =
            Array.Empty<int>();

        private int _usedLength;

        public int TotalClaims { get; private set; }

        public void Rebuild(
            IReadOnlyList<UnitRuntime> units)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));

            int highestUnitId = FindHighestUnitId(units);
            int requiredLength = highestUnitId + 1;

            EnsureCapacity(requiredLength);

            if (_usedLength > 0)
            {
                Array.Clear(
                    _claimCounts,
                    0,
                    _usedLength);
            }

            _usedLength = requiredLength;
            TotalClaims = 0;

            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime attacker = units[i];

                if (!attacker.IsAlive)
                    continue;

                UnitRuntime target = attacker.Target;

                if (!IsValidTarget(attacker, target))
                    continue;

                AddClaim(target);
            }
        }

        public int GetClaimCount(
            UnitRuntime target)
        {
            if (target == null)
                return 0;

            int targetId = target.Id;

            if (targetId < 0 ||
                targetId >= _claimCounts.Length)
            {
                return 0;
            }

            return _claimCounts[targetId];
        }

        public bool HasFreeSlot(
            UnitRuntime target)
        {
            if (target == null || !target.IsAlive)
                return false;

            return GetClaimCount(target) <
                   target.Stats.EngagementCapacity;
        }

        public void AddClaim(
            UnitRuntime target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            EnsureCapacity(target.Id + 1);

            _claimCounts[target.Id]++;
            TotalClaims++;
        }

        public void ReleaseClaim(
            UnitRuntime target)
        {
            if (target == null)
                return;

            int targetId = target.Id;

            if (targetId < 0 ||
                targetId >= _claimCounts.Length)
            {
                return;
            }

            if (_claimCounts[targetId] <= 0)
                return;

            _claimCounts[targetId]--;
            TotalClaims--;
        }

        public void Clear()
        {
            if (_usedLength > 0)
            {
                Array.Clear(
                    _claimCounts,
                    0,
                    _usedLength);
            }

            _usedLength = 0;
            TotalClaims = 0;
        }

        private void EnsureCapacity(
            int requiredCapacity)
        {
            if (_claimCounts.Length >= requiredCapacity)
                return;

            int capacity = Math.Max(
                requiredCapacity,
                MinimumCapacity);

            capacity = GetNextPowerOfTwo(capacity);

            Array.Resize(
                ref _claimCounts,
                capacity);
        }

        private static int FindHighestUnitId(
            IReadOnlyList<UnitRuntime> units)
        {
            int highestId = -1;

            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].Id > highestId)
                    highestId = units[i].Id;
            }

            return highestId;
        }

        private static bool IsValidTarget(
            UnitRuntime attacker,
            UnitRuntime target)
        {
            return target != null &&
                   target.IsAlive &&
                   target != attacker &&
                   target.Team != attacker.Team;
        }

        private static int GetNextPowerOfTwo(
            int value)
        {
            int result = 1;

            while (result < value)
                result <<= 1;

            return result;
        }
    }
}
