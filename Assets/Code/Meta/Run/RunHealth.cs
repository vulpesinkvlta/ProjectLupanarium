using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [Serializable]
    public sealed class SavedUnitHealth
    {
        public string UnitId;
        public int Ordinal;
        public float Fraction = 1;
    }

    public sealed partial class RunState
    {
        private readonly List<SavedUnitHealth> _health = new();
        public long RestReadyAt { get; private set; }
        public bool HasWounded => _health.Exists(h => h.Fraction < .9999f);
        public int DownedCount => _health.FindAll(h => h.Fraction <= 0).Count;

        // Ordinal is per config, not per flattened squad: reinforcements cannot shift other classes.
        public float GetHealthFraction(string unitId, int ordinal)
        {
            var entry = _health.Find(h => h.UnitId == unitId && h.Ordinal == ordinal);
            return entry?.Fraction ?? 1f;
        }

        public void CaptureHealth(IReadOnlyList<UnitRuntime> deployed)
        {
            _health.Clear();
            var ordinals = new Dictionary<string, int>();
            foreach (var unit in deployed)
            {
                ordinals.TryGetValue(unit.ConfigId, out int ordinal);
                ordinals[unit.ConfigId] = ordinal + 1;
                _health.Add(new SavedUnitHealth { UnitId = unit.ConfigId, Ordinal = ordinal,
                    Fraction = Mathf.Clamp01(unit.HealthNormalized) });
            }
            RestReadyAt = 0;
            // Flow saves atomically with the result and gold, not in the middle of that transition.
        }

        public void ApplyHealth(IReadOnlyList<UnitRuntime> deployed)
        {
            var ordinals = new Dictionary<string, int>();
            foreach (var unit in deployed)
            {
                ordinals.TryGetValue(unit.ConfigId, out int ordinal);
                ordinals[unit.ConfigId] = ordinal + 1;
                unit.RestoreHealthFraction(GetHealthFraction(unit.ConfigId, ordinal));
            }
        }

        public SavedUnitHealth[] ExportHealth() => _health.ConvertAll(h => new SavedUnitHealth
            { UnitId = h.UnitId, Ordinal = h.Ordinal, Fraction = h.Fraction }).ToArray();

        public void RestoreHealth(SavedUnitHealth[] health, long restReadyAt)
        {
            _health.Clear();
            if (health != null)
                foreach (var h in health)
                    if (h != null && !string.IsNullOrEmpty(h.UnitId) && h.Ordinal >= 0 &&
                        !float.IsNaN(h.Fraction) && !float.IsInfinity(h.Fraction))
                        _health.Add(new SavedUnitHealth { UnitId = h.UnitId, Ordinal = h.Ordinal,
                            Fraction = Mathf.Clamp01(h.Fraction) });
            RestReadyAt = Math.Max(0, restReadyAt);
        }

        public void BeginRest(long now)
        {
            if (!IsActive || !HasWounded || RestReadyAt > 0) return;
            RestReadyAt = now + 120;
            ProgressChanged?.Invoke();
        }

        public bool TryFinishRest(long now)
        {
            if (!IsActive || Phase == BattleFlowState.Fighting || !HasWounded || RestReadyAt <= 0 || now < RestReadyAt) return false;
            HealSquad();
            return true;
        }

        public void HealSquad()
        {
            if (!IsActive || Phase == BattleFlowState.Fighting) return;
            ResetHealth();
            ProgressChanged?.Invoke();
        }

        private void ResetHealth() { _health.Clear(); RestReadyAt = 0; }
    }
}
