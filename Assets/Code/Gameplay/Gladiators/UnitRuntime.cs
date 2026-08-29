using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitRuntime
    {
        public int Id { get; }
        public TeamId Team { get; }
        public UnitStats Stats { get; }

        public Vector2 Position { get; private set; }
        public Vector2 PreviousPosition { get; private set; }

        public float CurrentHealth { get; private set; }
        public float RemainingAttackCooldown { get; private set; }

        public UnitRuntime Target { get; private set; }
        public UnitState State { get; private set; }

        public float RemainingTargetRefreshTime { get; private set; }

        public bool IsTargetRefreshReady =>
            RemainingTargetRefreshTime <= 0f;

        public bool IsAlive =>
            State != UnitState.Dead &&
            CurrentHealth > 0f;

        public bool CanAttack =>
            IsAlive &&
            RemainingAttackCooldown <= 0f;

        public string ConfigId { get; }
        public UnitClassId ClassId { get; }

        /// <summary>Место бойца в строю, в локальных координатах формации.</summary>
        public Vector2 SlotOffset { get; private set; }

        /// <summary>
        /// Боец ещё держит строй. Сбрасывается навсегда при первом контакте:
        /// возвращаться в строй после схватки юниты не должны, иначе они
        /// начнут ходить туда-сюда между слотом и врагом.
        /// </summary>
        public bool HoldsFormation { get; private set; }

        public float HealthNormalized =>
            Stats.MaxHealth <= 0f
                ? 0f
                : CurrentHealth / Stats.MaxHealth;

        public UnitRuntime(
      int id,
      TeamId team,
      Vector2 position,
      UnitDefinition definition)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Unit ID cannot be negative.");
            }

            Id = id;
            Team = team;

            ConfigId = definition.ConfigId;
            ClassId = definition.ClassId;
            Stats = definition.Stats;

            Position = position;
            PreviousPosition = position;

            CurrentHealth = Stats.MaxHealth;
            RemainingAttackCooldown = 0f;
            RemainingTargetRefreshTime = 0f;

            State = UnitState.Idle;

            SlotOffset = Vector2.zero;
            HoldsFormation = false;
        }

        public void AssignFormationSlot(Vector2 slotOffset)
        {
            SlotOffset = slotOffset;
            HoldsFormation = true;
        }

        public void BreakFormation()
        {
            HoldsFormation = false;
        }

        public void BeginSimulationTick(float deltaTime)
        {
            if (deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            PreviousPosition = Position;

            if (RemainingAttackCooldown > 0f)
            {
                RemainingAttackCooldown = Mathf.Max(
                    0f,
                    RemainingAttackCooldown - deltaTime);
            }

            if (RemainingTargetRefreshTime > 0f)
            {
                RemainingTargetRefreshTime = Mathf.Max(
                    0f,
                    RemainingTargetRefreshTime - deltaTime);
            }
        }

        public void SetPosition(Vector2 position)
        {
            if (!IsAlive)
                return;

            Position = position;
        }

        public void SetTarget(UnitRuntime target)
        {
            if (!IsAlive)
                return;

            if (target == this)
                throw new InvalidOperationException(
                    "A unit cannot target itself.");

            if (target != null && target.Team == Team)
                throw new InvalidOperationException(
                    "A unit cannot target an ally in the sandbox battle.");

            Target = target;
        }

        public void ClearTarget()
        {
            Target = null;
        }

        public void SetState(UnitState state)
        {
            if (State == UnitState.Dead)
                return;

            State = state;
        }

        public void StartAttackCooldown()
        {
            if (!IsAlive)
                return;

            RemainingAttackCooldown = Stats.AttackCooldown;
        }

        public void StartTargetRefreshCooldown(
            float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "Target refresh duration must be greater than zero.");
            }

            RemainingTargetRefreshTime = duration;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(
                0f,
                CurrentHealth - amount);
        }

        public void MarkDead()
        {
            if (State == UnitState.Dead)
                return;

            CurrentHealth = 0f;
            Target = null;
            State = UnitState.Dead;

            HoldsFormation = false;
        }
    }
}
