using UnityEngine;

namespace Code.Gameplay
{
    public sealed class BattleHudDirtyTracker
    {
        public int RosterVersion { get; private set; }
        public int HealthVersion { get; private set; }
        public void MarkRosterChanged()
        {
            unchecked
            {
                RosterVersion++;
                HealthVersion++;
            }
        }

        public void MarkHealthChanged()
        {
            unchecked
            {
                HealthVersion++;
            }
        }
    }
}
