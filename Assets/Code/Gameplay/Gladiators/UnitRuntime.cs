using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitRuntime
    {
        /// <summary>
        /// Ниже этого остатка таймер считается истёкшим. Порог берёт
        /// на себя накопленную ошибку float при вычитании дельты.
        /// </summary>
        private const float TimerEpsilon = 0.0001f;

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

        // --- боевые состояния ---

        /// <summary>Сколько осталось до удара. Ноль — замаха нет.</summary>
        public float RemainingWindup { get; private set; }

        public bool IsWindingUp => RemainingWindup > 0f;

        public float RemainingStun { get; private set; }
        public bool IsStunned => RemainingStun > 0f;

        public float RemainingSlow { get; private set; }

        /// <summary>Множитель скорости от замедления: 1 — без эффекта.</summary>
        public float SlowFactor { get; private set; }

        public float RemainingHaste { get; private set; }

        /// <summary>Множитель скорости от ускорения: 1 — без эффекта.</summary>
        public float HasteFactor { get; private set; }

        /// <summary>
        /// Скорость с учётом эффектов. Замедление и ускорение — разные
        /// множители, потому что складывать их в один нельзя: тогда
        /// «замедлен и ускорен» дало бы неопределённый результат.
        /// </summary>
        public float EffectiveMoveSpeed =>
            IsStunned
                ? 0f
                : Stats.MoveSpeed * SlowFactor * HasteFactor;

        public float RemainingAbilityCooldown { get; private set; }

        public bool CanUseAbility =>
            IsAlive &&
            !IsStunned &&
            RemainingAbilityCooldown <= 0f;

        public bool CanAttack =>
            IsAlive &&
            !IsStunned &&
            RemainingAttackCooldown <= 0f;

        public string ConfigId { get; }
        public UnitClassId ClassId { get; }

        /// <summary>Золото, которое получит убийца этого юнита.</summary>
        public int GoldReward { get; }

        /// <summary>Способность бойца. Может быть пустой.</summary>
        public AbilitySpec Ability { get; }

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
            GoldReward = definition.GoldReward;
            Ability = definition.Ability;

            Position = position;
            PreviousPosition = position;

            CurrentHealth = Stats.MaxHealth;
            RemainingAttackCooldown = 0f;
            RemainingTargetRefreshTime = 0f;
            RemainingWindup = 0f;
            RemainingStun = 0f;
            RemainingSlow = 0f;
            RemainingAbilityCooldown = 0f;
            RemainingHaste = 0f;
            SlowFactor = 1f;
            HasteFactor = 1f;

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

            RemainingAttackCooldown =
                Countdown(RemainingAttackCooldown, deltaTime);

            RemainingTargetRefreshTime =
                Countdown(RemainingTargetRefreshTime, deltaTime);

            RemainingAbilityCooldown =
                Countdown(RemainingAbilityCooldown, deltaTime);

            TickStatusEffects(deltaTime);
        }

        /// <summary>
        /// Уменьшает таймер, защёлкивая остаток на ноль.
        ///
        /// Простое вычитание сюда не годится: 0.3 минус три раза по 0.1
        /// во float даёт не ноль, а полторы стомиллионных, и таймер
        /// остаётся «работающим» лишний тик. На замахе это ловится
        /// сразу, на кулдаунах копится незаметно.
        /// </summary>
        private static float Countdown(float remaining, float deltaTime)
        {
            if (remaining <= 0f)
                return 0f;

            remaining -= deltaTime;

            return remaining <= TimerEpsilon
                ? 0f
                : remaining;
        }

        private void TickStatusEffects(float deltaTime)
        {
            RemainingStun = Countdown(RemainingStun, deltaTime);

            if (RemainingSlow > 0f)
            {
                RemainingSlow = Countdown(RemainingSlow, deltaTime);

                // Замедление кончилось — скорость возвращается сама,
                // отдельной системы снятия эффектов не нужно.
                if (RemainingSlow <= 0f)
                    SlowFactor = 1f;
            }

            if (RemainingHaste > 0f)
            {
                RemainingHaste = Countdown(RemainingHaste, deltaTime);

                if (RemainingHaste <= 0f)
                    HasteFactor = 1f;
            }
        }

        // --- замах ---

        public void BeginWindup(float duration)
        {
            if (!IsAlive)
                return;

            // Нулевой замах означает мгновенный удар: систему это
            // избавляет от ветвления, у неё всегда один и тот же путь.
            RemainingWindup = Mathf.Max(0f, duration);
            SetState(UnitState.WindingUp);
        }

        /// <summary>
        /// Уменьшает оставшийся замах. Отдельно от BeginSimulationTick,
        /// потому что оглушённый боец замах не доводит.
        /// </summary>
        public void TickWindup(float deltaTime)
        {
            RemainingWindup = Countdown(RemainingWindup, deltaTime);
        }

        public void CancelWindup()
        {
            RemainingWindup = 0f;
        }

        // --- эффекты ---

        public void ApplyStun(float duration)
        {
            if (!IsAlive || duration <= 0f)
                return;

            // Эффекты не складываются, а продлеваются: иначе толпа
            // ретиариев держала бы цель в вечном оглушении.
            RemainingStun = Mathf.Max(RemainingStun, duration);

            CancelWindup();
            SetState(UnitState.Stunned);
        }

        public void ApplySlow(float duration, float factor)
        {
            if (!IsAlive || duration <= 0f)
                return;

            float clampedFactor = Mathf.Clamp(factor, 0.05f, 1f);

            RemainingSlow = Mathf.Max(RemainingSlow, duration);

            // Берём самое сильное замедление из действующих.
            SlowFactor = Mathf.Min(SlowFactor, clampedFactor);
        }

        public void ApplyHaste(float duration, float factor)
        {
            if (!IsAlive || duration <= 0f)
                return;

            float clampedFactor = Mathf.Clamp(factor, 1f, 5f);

            RemainingHaste = Mathf.Max(RemainingHaste, duration);
            HasteFactor = Mathf.Max(HasteFactor, clampedFactor);
        }

        public void StartAbilityCooldown(float duration)
        {
            if (duration <= 0f)
                return;

            RemainingAbilityCooldown = duration;
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
            RemainingWindup = 0f;
            State = UnitState.Dead;

            HoldsFormation = false;
        }
    }
}
