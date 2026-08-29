using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Двигает якоря строёв навстречу врагу.
    ///
    /// Без наступающего якоря бой встал бы намертво: обе армии держали бы
    /// свои слоты и никогда не сошлись. Якорь едет со скоростью самого
    /// медленного бойца в строю, иначе строй уехал бы вперёд без отставших.
    /// </summary>
    public sealed class FormationSystem
    {
        private static readonly ProfilerMarker TickMarker =
            new("Arena.Formation");

        private readonly ArenaContext _context;
        private readonly FormationRegistry _registry;

        public int UnitsHoldingFormation { get; private set; }

        public FormationSystem(
            ArenaContext context,
            FormationRegistry registry)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _registry = registry ??
                throw new ArgumentNullException(nameof(registry));
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            using (TickMarker.Auto())
            {
                UnitsHoldingFormation = 0;

                AdvanceTeam(TeamId.Player, deltaTime);
                AdvanceTeam(TeamId.Enemy, deltaTime);
            }
        }

        private void AdvanceTeam(TeamId team, float deltaTime)
        {
            TeamFormationState state = _registry.Get(team);

            if (!state.IsActive)
                return;

            float slowestSpeed = float.MaxValue;
            var holders = 0;

            IReadOnlyList<UnitRuntime> units =
                team == TeamId.Player
                    ? _context.PlayerUnits
                    : _context.EnemyUnits;

            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                if (!unit.IsAlive || !unit.HoldsFormation)
                    continue;

                holders++;

                if (unit.Stats.MoveSpeed < slowestSpeed)
                    slowestSpeed = unit.Stats.MoveSpeed;
            }

            UnitsHoldingFormation += holders;

            // Строй распался — якорю больше некого вести.
            if (holders == 0)
            {
                _registry.Deactivate(team);
                return;
            }

            float marchSpeed = Mathf.Min(
                state.MarchSpeed,
                slowestSpeed);

            if (marchSpeed <= 0f)
                return;

            state.Anchor = new Vector2(
                state.Anchor.x + state.FacingSign * marchSpeed * deltaTime,
                state.Anchor.y);
        }
    }
}
