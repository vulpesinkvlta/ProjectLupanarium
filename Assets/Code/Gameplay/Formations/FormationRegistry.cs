using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Состояние строя одной команды на время боя.
    /// </summary>
    public sealed class TeamFormationState
    {
        public FormationConfig Config;

        /// <summary>Остриё строя. Едет на врага, пока кто-то держит строй.</summary>
        public Vector2 Anchor;

        /// <summary>+1 если команда смотрит вправо, -1 если влево.</summary>
        public float FacingSign;

        public bool IsActive;

        public float BreakRange =>
            Config != null ? Config.BreakRange : 0f;

        public float MarchSpeed =>
            Config != null ? Config.MarchSpeed : 0f;

        public FormationShape Shape =>
            Config != null
                ? Config.Shape
                : new FormationShape(FormationLayout.Box, 1, 1, 0.8f, 0.8f);

        /// <summary>Переводит слот из локального пространства строя в мир.</summary>
        public Vector2 SlotToWorld(Vector2 slotOffset)
        {
            return new Vector2(
                Anchor.x + slotOffset.x * FacingSign,
                Anchor.y + slotOffset.y);
        }
    }

    /// <summary>
    /// Строи обеих команд. Живёт один бой.
    /// </summary>
    public sealed class FormationRegistry
    {
        private readonly TeamFormationState _player = new();
        private readonly TeamFormationState _enemy = new();

        public TeamFormationState Get(TeamId team)
        {
            return team == TeamId.Player
                ? _player
                : _enemy;
        }

        public void Setup(
            TeamId team,
            FormationConfig config,
            Vector2 anchor,
            float facingSign)
        {
            TeamFormationState state = Get(team);

            state.Config = config;
            state.Anchor = anchor;
            state.FacingSign = facingSign;

            // Без конфига команда просто бежит на врага, как до Фазы 3.
            state.IsActive = config != null;
        }

        public void Deactivate(TeamId team)
        {
            Get(team).IsActive = false;
        }

        public void Clear()
        {
            ResetState(_player);
            ResetState(_enemy);
        }

        private static void ResetState(TeamFormationState state)
        {
            state.Config = null;
            state.Anchor = Vector2.zero;
            state.FacingSign = 1f;
            state.IsActive = false;
        }
    }
}
