using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Постоянные бонусы от построек школы.
    ///
    /// Третий источник модификаторов после забега и строя. Симуляция
    /// про школу по-прежнему ничего не знает — всё сходится
    /// в CompositeUnitModifierSource.
    /// </summary>
    public sealed class LupanariumModifierSource : IUnitModifierSource
    {
        private readonly LupanariumState _state;
        private readonly SchoolCatalog _catalog;

        public LupanariumModifierSource(
            LupanariumState state,
            SchoolCatalog catalog)
        {
            _state = state ??
                throw new ArgumentNullException(nameof(state));

            _catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
        }

        public void Collect(
            UnitConfig config,
            TeamId team,
            ModifierSet destination)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Школа принадлежит игроку — врагов её постройки не касаются.
            if (team != TeamId.Player)
                return;

            IReadOnlyList<SchoolBuildingConfig> buildings =
                _catalog.Buildings;

            for (var i = 0; i < buildings.Count; i++)
            {
                SchoolBuildingConfig building = buildings[i];

                if (building == null)
                    continue;

                if (!building.AppliesTo(config.ClassId))
                    continue;

                int level = _state.GetLevel(building);

                if (level <= 0)
                    continue;

                building.CollectModifiers(level, destination);
            }
        }
    }
}
