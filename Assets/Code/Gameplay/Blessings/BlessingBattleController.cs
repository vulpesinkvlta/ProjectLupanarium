using UnityEngine;

namespace Code.Gameplay
{
    public sealed class BlessingBattleController
    {
        private readonly RunState _run;
        private readonly LupanariumState _school;
        private readonly BlessingCatalog _catalog;
        private readonly ArenaContext _arena;
        private readonly ArenaBounds _bounds;
        private readonly BattleHudDirtyTracker _dirty;
        private readonly BattleFeedbackQueue _feedback;
        private readonly BattleStatistics _statistics;

        public BlessingBattleController(RunState run, LupanariumState school,
            BlessingCatalog catalog, ArenaContext arena, ArenaBounds bounds,
            BattleHudDirtyTracker dirty, BattleFeedbackQueue feedback, BattleStatistics statistics)
        {
            _run = run; _school = school; _catalog = catalog;
            _arena = arena; _bounds = bounds; _dirty = dirty; _feedback = feedback;
            _statistics = statistics;
        }

        public bool CanCast(BlessingConfig blessing) =>
            _run.Phase == BattleFlowState.Fighting && _catalog.Contains(blessing) &&
            _school.GetBlessingCharges(blessing.Id) > 0;

        public bool IsValidPoint(Vector2 point) =>
            !float.IsNaN(point.x) && !float.IsNaN(point.y) &&
            !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && _bounds.Contains(point, 0);

        public bool TryCast(BlessingConfig blessing, Vector2 point)
        {
            if (!CanCast(blessing) || !IsValidPoint(point)) return false;
            // Do not spend charges between the final hit and the victory tick.
            bool playerAlive = false, enemyAlive = false;
            foreach (var unit in _arena.AllUnits)
                if (unit.IsAlive)
                {
                    if (unit.Team == TeamId.Player) playerAlive = true;
                    else enemyAlive = true;
                }
            if (!playerAlive || !enemyAlive || !_school.TryConsumeBlessing(blessing.Id)) return false;

            float radiusSquared = blessing.Radius * blessing.Radius;
            foreach (var unit in _arena.AllUnits)
            {
                if (!unit.IsAlive || (unit.Position - point).sqrMagnitude > radiusSquared) continue;
                switch (blessing.Effect)
                {
                    case BlessingEffect.Heal:
                        unit.Heal(blessing.Power);
                        break;
                    case BlessingEffect.Tar:
                        unit.ApplySlow(blessing.Duration, blessing.SpeedFactor);
                        float damage = Mathf.Max(1, blessing.Power - unit.Stats.Armor);
                        float applied = Mathf.Min(damage, unit.CurrentHealth);
                        unit.ApplyDamage(damage);
                        unit.RecordDamageSource(null);
                        _statistics.RegisterDamage(null, unit, applied, Mathf.Max(0, blessing.Power - damage));
                        _feedback.Push(BattleFeedbackKind.Hit, unit.Id, unit.Position, applied);
                        break;
                    case BlessingEffect.Haste:
                        unit.ApplyHaste(blessing.Duration, blessing.SpeedFactor);
                        break;
                }
            }
            // Existing Death/Victory/Cleanup systems handle deaths on the next tick.
            _dirty.MarkHealthChanged();
            return true;
        }
    }
}
