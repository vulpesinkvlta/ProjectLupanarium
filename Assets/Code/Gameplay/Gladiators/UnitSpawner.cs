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
        private readonly FormationRegistry _formationRegistry;
        private readonly FormationSolver _formationSolver;
        private readonly UnitViewSynchronizer _viewSynchronizer;
        private readonly ArenaBounds _bounds;

        private readonly List<UnitView> _viewReleaseBuffer =
            new(InitialViewBufferCapacity);

        // Состав отряда, развёрнутый в плоский список: SquadEntry говорит
        // "5 мирмиллонов", а расстановке нужны позиции по одной.
        private readonly List<UnitConfig> _configBuffer =
            new(InitialConfigBufferCapacity);

        private readonly List<Vector2> _slotBuffer =
            new(InitialConfigBufferCapacity);

        private int _nextUnitId;

        public UnitSpawner(
            ArenaContext context,
            UnitViewPool viewPool,
            UnitViewRegistry viewRegistry,
            BattleHudDirtyTracker hudDirtyTracker,
            UnitDefinitionResolver definitionResolver,
            FormationRegistry formationRegistry,
            FormationSolver formationSolver,
            UnitViewSynchronizer viewSynchronizer,
            ArenaBounds bounds)
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

            _formationRegistry = formationRegistry ??
                throw new ArgumentNullException(
                    nameof(formationRegistry));

            _formationSolver = formationSolver ??
                throw new ArgumentNullException(
                    nameof(formationSolver));

            _viewSynchronizer = viewSynchronizer ??
                throw new ArgumentNullException(
                    nameof(viewSynchronizer));

            _bounds = bounds ??
                throw new ArgumentNullException(nameof(bounds));
        }

        public void SpawnSquads(
            IReadOnlyList<SquadEntry> playerSquad,
            FormationConfig playerFormation,
            IReadOnlyList<SquadEntry> enemySquad,
            FormationConfig enemyFormation)
        {
            if (playerSquad == null)
                throw new ArgumentNullException(nameof(playerSquad));

            if (enemySquad == null)
                throw new ArgumentNullException(nameof(enemySquad));

            // Строи выставляются до спавна: FormationModifierSource читает
            // их из реестра, когда резолвер считает статы.
            _formationRegistry.Setup(
                TeamId.Player,
                playerFormation,
                new Vector2(-SpawnCenterX, 0f),
                facingSign: 1f);

            _formationRegistry.Setup(
                TeamId.Enemy,
                enemyFormation,
                new Vector2(SpawnCenterX, 0f),
                facingSign: -1f);

            SpawnTeam(TeamId.Player, playerSquad);
            SpawnTeam(TeamId.Enemy, enemySquad);

            _hudDirtyTracker.MarkRosterChanged();
        }

        public void ClearAll()
        {
            // Идём от реестра вью, а не от ArenaContext: погибшие юниты
            // из контекста уже удалены UnitCleanupSystem, но их вью могут
            // ещё ждать возврата в пул.
            // Догорающие трупы прошлой волны тоже в пул: иначе они
            // доигрывали бы угасание поверх новой расстановки.
            _viewSynchronizer.ReleaseAllDying();

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

            TeamFormationState formation =
                _formationRegistry.Get(team);

            if (formation.IsActive)
                SpawnInFormation(team, formation, count);
            else
                SpawnInDefaultGrid(team, count);
        }

        private void SpawnInFormation(
            TeamId team,
            TeamFormationState formation,
            int count)
        {
            _formationSolver.BuildSlots(
                formation.Shape,
                count,
                _slotBuffer);

            for (var i = 0; i < count; i++)
            {
                Vector2 slotOffset = _slotBuffer[i];

                UnitRuntime runtime = SpawnUnit(
                    team,
                    formation.SlotToWorld(slotOffset),
                    _configBuffer[i]);

                runtime.AssignFormationSlot(slotOffset);
            }
        }

        /// <summary>
        /// Расстановка без строя — та же сетка, что была до Фазы 3.
        /// Нужна, чтобы бой собирался и без ассетов формаций.
        /// </summary>
        private void SpawnInDefaultGrid(
            TeamId team,
            int count)
        {
            int columns = Mathf.CeilToInt(
                Mathf.Sqrt(count));

            int rows = Mathf.CeilToInt(
                count / (float)columns);

            float teamCenterX = team == TeamId.Player
                ? -SpawnCenterX
                : SpawnCenterX;

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

        private UnitRuntime SpawnUnit(
            TeamId team,
            Vector2 position,
            UnitConfig config)
        {
            int unitId = _nextUnitId++;

            UnitDefinition definition =
                _definitionResolver.Resolve(config, team);

            // Строй может вынести слот за круг арены — например широкая
            // фаланга на большом отряде. Вжимаем сразу при спавне, иначе
            // на первом же тике бойцы заметно дёрнулись бы внутрь.
            // Радиус берём из посчитанных статов, а не из конфига:
            // модификаторы могли его изменить.
            position = _bounds.Clamp(position, definition.Stats.Radius);

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

            return runtime;
        }
    }
}
