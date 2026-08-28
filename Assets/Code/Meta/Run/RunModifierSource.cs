using System;
using System.Collections.Generic;

namespace Code.Gameplay
{
    /// <summary>
    /// Отдаёт симуляции модификаторы, набранные за забег.
    ///
    /// Единственное место, связывающее мета-прогрессию с боевыми статами:
    /// UnitSpawner, UnitDefinitionResolver и UnitStatsBuilder про улучшения
    /// ничего не знают и знать не должны.
    /// </summary>
    public sealed class RunModifierSource : IUnitModifierSource
    {
        private readonly RunState _runState;

        public RunModifierSource(RunState runState)
        {
            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));
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

            // Улучшения игрока не должны баффать врагов.
            if (team != TeamId.Player)
                return;

            IReadOnlyList<UpgradeConfig> upgrades =
                _runState.AcquiredUpgrades;

            for (var i = 0; i < upgrades.Count; i++)
            {
                UpgradeConfig upgrade = upgrades[i];

                if (upgrade == null)
                    continue;

                if (!upgrade.AppliesTo(config.ClassId))
                    continue;

                destination.AddRange(upgrade.Modifiers);
            }
        }
    }
}
