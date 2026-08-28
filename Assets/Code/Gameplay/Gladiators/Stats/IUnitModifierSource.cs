namespace Code.Gameplay
{
    public interface IUnitModifierSource
    {
        void Collect(UnitConfig config, TeamId team, ModifierSet destination);
    }
}
