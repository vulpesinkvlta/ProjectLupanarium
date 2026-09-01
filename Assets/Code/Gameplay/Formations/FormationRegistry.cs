using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Строй одной команды в бою.
    ///
    /// Ссылку на ассет не держит: параметры копируются при Setup. Так
    /// состояние остаётся обычными данными, его можно собрать в тесте
    /// без Unity — тот же приём, что с FormationShape.
    /// </summary>
    public sealed class TeamFormationState
    {
        /// <summary>Остриё строя. Едет на врага, пока кто-то держит строй.</summary>
        public Vector2 Anchor;

        /// <summary>+1 если команда смотрит вправо, -1 если влево.</summary>
        public float FacingSign;

        public bool IsActive;

        public float BreakRange;
        public float MarchSpeed;
        public FormationShape Shape;

        /// <summary>Название строя. Нужно только диагностике.</summary>
        public string DisplayName = string.Empty;

        /// <summary>
        /// Бонусы строя, скопированные из ассета. Копия, а не ссылка,
        /// по той же причине, что и остальные параметры: состояние боя
        /// не должно зависеть от живости ScriptableObject.
        /// </summary>
        public readonly List<StatModifier> Modifiers = new(8);

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

            state.Anchor = anchor;
            state.FacingSign = facingSign;

            // Без конфига команда просто бежит на врага, как до Фазы 3.
            state.IsActive = config != null;

            if (config == null)
            {
                ResetShape(state);
                return;
            }

            state.BreakRange = config.BreakRange;
            state.MarchSpeed = config.MarchSpeed;
            state.Shape = config.Shape;
            state.DisplayName = config.DisplayName;

            state.Modifiers.Clear();

            IReadOnlyList<StatModifier> modifiers = config.Modifiers;

            for (var i = 0; i < modifiers.Count; i++)
                state.Modifiers.Add(modifiers[i]);
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
            state.Anchor = Vector2.zero;
            state.FacingSign = 1f;
            state.IsActive = false;

            ResetShape(state);
        }

        private static void ResetShape(TeamFormationState state)
        {
            state.BreakRange = 0f;
            state.MarchSpeed = 0f;
            state.DisplayName = string.Empty;
            state.Modifiers.Clear();

            state.Shape = new FormationShape(
                FormationLayout.Box, 1, 1, 0.8f, 0.8f);
        }
    }
}
