using System;

namespace Code.Gameplay
{
    /// <summary>
    /// Бонусы строя. Читает формацию команды из реестра, поэтому работает
    /// одинаково и для игрока, и для врага: у обоих строй уже выставлен
    /// к моменту спавна.
    /// </summary>
    public sealed class FormationModifierSource : IUnitModifierSource
    {
        private readonly FormationRegistry _registry;

        public FormationModifierSource(FormationRegistry registry)
        {
            _registry = registry ??
                throw new ArgumentNullException(nameof(registry));
        }

        public void Collect(
            UnitConfig config,
            TeamId team,
            ModifierSet destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            TeamFormationState formation = _registry.Get(team);

            if (!formation.IsActive)
                return;

            destination.AddRange(formation.Modifiers);
        }
    }
}
