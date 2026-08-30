using UnityEngine;

namespace Code.Gameplay
{
    public enum UnitState : byte
    {
        Idle = 0,
        Moving = 1,

        /// <summary>Замах занесён, удар ещё не нанесён.</summary>
        WindingUp = 2,

        /// <summary>Удар нанесён в этом тике.</summary>
        Attacking = 3,

        Dead = 4,

        /// <summary>Оглушён: не ходит и не бьёт.</summary>
        Stunned = 5
    }
}
