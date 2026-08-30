using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Бонусы надетого снаряжения. Четвёртый источник модификаторов
    /// после забега, строя и построек школы.
    /// </summary>
    public sealed class EquipmentModifierSource : IUnitModifierSource
    {
        private readonly LupanariumState _state;
        private readonly ItemCatalog _catalog;

        public EquipmentModifierSource(
            LupanariumState state,
            ItemCatalog catalog)
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

            // Снаряжение принадлежит школе игрока.
            if (team != TeamId.Player)
                return;

            IReadOnlyList<ItemConfig> items = _catalog.Items;

            for (var i = 0; i < items.Count; i++)
            {
                ItemConfig item = items[i];

                if (item == null)
                    continue;

                if (!item.AppliesTo(config.ClassId))
                    continue;

                if (!_state.IsEquipped(item))
                    continue;

                destination.AddRange(item.Modifiers);
            }
        }
    }
}
