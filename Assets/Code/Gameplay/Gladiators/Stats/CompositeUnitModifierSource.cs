using System;

namespace Code.Gameplay
{
    /// <summary>
    /// Складывает модификаторы из всех источников в один набор.
    ///
    /// Источники перечислены в конструкторе явно, а не приходят коллекцией
    /// из контейнера: так видно, что именно влияет на статы, и порядок
    /// не зависит от порядка регистрации.
    /// </summary>
    public sealed class CompositeUnitModifierSource : IUnitModifierSource
    {
        private readonly RunModifierSource _runModifiers;
        private readonly FormationModifierSource _formationModifiers;
        private readonly LupanariumModifierSource _lupanariumModifiers;

        public CompositeUnitModifierSource(
            RunModifierSource runModifiers,
            FormationModifierSource formationModifiers,
            LupanariumModifierSource lupanariumModifiers)
        {
            _runModifiers = runModifiers ??
                throw new ArgumentNullException(nameof(runModifiers));

            _formationModifiers = formationModifiers ??
                throw new ArgumentNullException(
                    nameof(formationModifiers));

            _lupanariumModifiers = lupanariumModifiers ??
                throw new ArgumentNullException(
                    nameof(lupanariumModifiers));
        }

        public void Collect(
            UnitConfig config,
            TeamId team,
            ModifierSet destination)
        {
            _runModifiers.Collect(config, team, destination);
            _formationModifiers.Collect(config, team, destination);
            _lupanariumModifiers.Collect(config, team, destination);
        }
    }
}
