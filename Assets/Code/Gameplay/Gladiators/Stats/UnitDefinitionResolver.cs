using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Собирает финальные статы юнита и кэширует их на время боя.
    ///
    /// Кэш по паре (конфиг, команда): у всех мирмиллонов игрока в одном бою
    /// статы одинаковы, считать их для каждого из 250 юнитов незачем.
    /// Команда входит в ключ, потому что один и тот же конфиг используют
    /// обе армии, а улучшения забега действуют только на игрока.
    /// </summary>
    public sealed class UnitDefinitionResolver
    {
        private readonly UnitStatsBuilder _statsBuilder;
        private readonly IUnitModifierSource _modifierSource;

        // Рабочий буфер, а не сервис: чистится и наполняется на каждый
        // промах кэша, наружу отдавать нечего.
        private readonly ModifierSet _modifiers = new();

        private readonly Dictionary<(UnitConfig, TeamId), UnitDefinition> _cache =
            new();

        public UnitDefinitionResolver(
            UnitStatsBuilder statsBuilder,
            IUnitModifierSource modifierSource)
        {
            _statsBuilder = statsBuilder ??
                throw new ArgumentNullException(nameof(statsBuilder));

            _modifierSource = modifierSource ??
                throw new ArgumentNullException(nameof(modifierSource));
        }

        public UnitDefinition Resolve(UnitConfig config, TeamId team)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_cache.TryGetValue((config, team), out UnitDefinition definition))
                return definition;

            _modifiers.Clear();
            _modifierSource.Collect(config, team, _modifiers);

            UnitStats stats = _statsBuilder.Build(config, _modifiers);

            definition = new UnitDefinition(
                config.Id,
                config.ClassId,
                stats);

            _cache[(config, team)] = definition;

            return definition;
        }

        /// <summary>
        /// Сбрасывает кэш. Обязателен перед каждой волной: модификаторы
        /// в ключ кэша не входят, поэтому сам он устаревание не заметит,
        /// и взятое между волнами улучшение молча не применилось бы.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}
