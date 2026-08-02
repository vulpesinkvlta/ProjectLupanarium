using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitSpawner
    {
        private const float SpawnCenterX = 1.5f;
        private const float ColumnSpacing = 0.8f;
        private const float RowSpacing = 0.8f;

        private readonly ArenaContext _context;
        private readonly UnitViewPool _viewPool;
        private readonly UnitViewRegistry _viewRegistry;
        private readonly ArenaSandboxRoster _sandboxRoster;

        private int _nextUnitId;

        public UnitSpawner(
            ArenaContext context,
            UnitViewPool viewPool,
            UnitViewRegistry viewRegistry,
            ArenaSandboxRoster sandboxRoster)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _sandboxRoster = sandboxRoster ??
                throw new ArgumentNullException(nameof(sandboxRoster));
        }

        public void SpawnBattle(int unitsPerTeam)
        {
            if (unitsPerTeam <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitsPerTeam),
                    "Units per team must be greater than zero.");
            }

            SpawnTeam(
                TeamId.Player,
                unitsPerTeam);

            SpawnTeam(
                TeamId.Enemy,
                unitsPerTeam);
        }

        public void ClearAll()
        {
            var units = _context.AllUnits;

            for (var i = 0; i < units.Count; i++)
            {
                UnitRuntime unit = units[i];

                if (!_viewRegistry.Remove(
                        unit.Id,
                        out UnitView view))
                {
                    continue;
                }

                _viewPool.Release(view);
            }

            _viewRegistry.Clear();
            _context.Clear();

            _nextUnitId = 0;
        }

        private void SpawnTeam(
            TeamId team,
            int count)
        {
            int columns = Mathf.CeilToInt(
                Mathf.Sqrt(count));

            int rows = Mathf.CeilToInt(
                count / (float)columns);

            float teamCenterX = team switch
            {
                TeamId.Player => -SpawnCenterX,
                TeamId.Enemy => SpawnCenterX,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(team),
                    team,
                    "Unsupported team.")
            };

            // ќбе армии расшир€ютс€ от центра арены наружу.
            float depthDirection =
                team == TeamId.Player
                    ? -1f
                    : 1f;

            float verticalCenterOffset =
                (rows - 1) * RowSpacing * 0.5f;

            for (var i = 0; i < count; i++)
            {
                int column = i % columns;
                int row = i / columns;

                float positionX =
                    teamCenterX +
                    depthDirection *
                    column *
                    ColumnSpacing;

                float positionY =
                    row * RowSpacing -
                    verticalCenterOffset;
                UnitConfig config =
                    _sandboxRoster.GetConfig(
                        team,
                        i);

                SpawnUnit(
                    team,
                    new Vector2(positionX, positionY),
                    config);
            }
        }

        private void SpawnUnit(
            TeamId team,
            Vector2 position,
            UnitConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            int unitId = _nextUnitId++;

            UnitDefinition definition =
                config.CreateDefinition();

            UnitRuntime runtime = new(
                id: unitId,
                team: team,
                position: position,
                definition: definition);

            _context.AddUnit(runtime);

            UnitView view =
                _viewPool.Get(runtime);

            try
            {
                _viewRegistry.Add(
                    unitId,
                    view);
            }
            catch
            {
                _context.RemoveUnit(runtime);
                _viewPool.Release(view);

                throw;
            }
        }
    }
}