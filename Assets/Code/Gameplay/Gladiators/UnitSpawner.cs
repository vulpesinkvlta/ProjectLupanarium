using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitSpawner
    {
        private const float SpawnCenterX = 1.5f;
        private const float ColumnSpacing = 0.8f;
        private const float RowSpacing = 0.8f;

        private const int InitialViewBufferCapacity = 512;
        private const int InitialConfigBufferCapacity = 512;

        private readonly ArenaContext _context;
        private readonly UnitViewPool _viewPool;
        private readonly UnitViewRegistry _viewRegistry;
        private readonly BattleHudDirtyTracker _hudDirtyTracker;
        private readonly UnitDefinitionResolver _definitionResolver;

        private readonly List<UnitView> _viewReleaseBuffer =
            new(InitialViewBufferCapacity);

        // Состав отряда развёрнутый в плоский список: SquadEntry говорит
        // "5 мирмиллонов", а сетке размещения нужны позиции по одной.
        private readonly List<UnitConfig> _configBuffer =
            new(InitialConfigBufferCapacity);

        private int _nextUnitId;

        public UnitSpawner(
            ArenaContext context,
            UnitViewPool viewPool,
            UnitViewRegistry viewRegistry,
            BattleHudDirtyTracker hudDirtyTracker,
            UnitDefinitionResolver definitionResolver)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _hudDirtyTracker = hudDirtyTracker ??
                throw new ArgumentNullException(
                    nameof(hudDirtyTracker));

            _definitionResolver = definitionResolver ??
                throw new ArgumentNullException(
                    nameof(definitionResolver));
        }

        public void SpawnSquads(
            IReadOnlyList<SquadEntry> playerSquad,
            IReadOnlyList<SquadEntry> enemySquad)
        {
            if (playerSquad == null)
                throw new ArgumentNullException(nameof(playerSquad));

            if (enemySquad == null)
                throw new ArgumentNullException(nameof(enemySquad));

            SpawnTeam(TeamId.Player, playerSquad);
            SpawnTeam(TeamId.Enemy, enemySquad);

            _hudDirtyTracker.MarkRosterChanged();
        }

        public void ClearAll()
        {
            // Идём от реестра вью, а не от ArenaContext: погибшие юниты
            // из контекста уже удалены UnitCleanupSystem, но их вью могут
            // ещё ждать возврата в пул.
            _viewRegistry.DrainInto(_viewReleaseBuffer);

            for (var i = 0; i < _viewReleaseBuffer.Count; i++)
                _viewPool.Release(_viewReleaseBuffer[i]);

            _viewReleaseBuffer.Clear();
            _context.Clear();

            _nextUnitId = 0;
            _hudDirtyTracker.MarkRosterChanged();
        }

        private void SpawnTeam(
            TeamId team,
            IReadOnlyList<SquadEntry> squad)
        {
            ExpandSquad(squad);

            int count = _configBuffer.Count;

            if (count == 0)
                return;

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

            // Обе армии расширяются от центра арены наружу.
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

                SpawnUnit(
                    team,
                    new Vector2(positionX, positionY),
                    _configBuffer[i]);
            }
        }

        private void ExpandSquad(
            IReadOnlyList<SquadEntry> squad)
        {
            _configBuffer.Clear();

            for (var i = 0; i < squad.Count; i++)
            {
                SquadEntry entry = squad[i];

                if (entry == null || entry.Config == null)
                    continue;

                for (var unit = 0; unit < entry.Count; unit++)
                    _configBuffer.Add(entry.Config);
            }
        }

        private void SpawnUnit(
            TeamId team,
            Vector2 position,
            UnitConfig config)
        {
            int unitId = _nextUnitId++;

            UnitDefinition definition =
                _definitionResolver.Resolve(config, team);

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
