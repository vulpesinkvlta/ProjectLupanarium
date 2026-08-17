using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class BattleHealthHudPresenter : IStartable, ITickable, IDisposable
    {
        private const int InitialUnitCapacity = 512;
        private const int InitialRowCapacity = 16;

        private readonly ArenaContext _context;
        private readonly BattleHudDirtyTracker _dirtyTracker;
        private readonly BattleHealthHudView _view;

        private readonly List<UnitHealthSegmentView>
            _activeSegments =
                new(InitialUnitCapacity);

        private readonly List<ActiveRow>
            _activeRows =
                new(InitialRowCapacity);

        private int _lastRosterVersion = -1;
        private int _lastHealthVersion = -1;

        public BattleHealthHudPresenter(
            ArenaContext context,
            BattleHudDirtyTracker dirtyTracker,
            BattleHealthHudView view)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _dirtyTracker = dirtyTracker ??
                throw new ArgumentNullException(
                    nameof(dirtyTracker));

            _view = view ??
                throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            RebuildHud();
        }

        public void Tick()
        {
            if (_lastRosterVersion !=
                _dirtyTracker.RosterVersion)
            {
                RebuildHud();
                return;
            }

            if (_lastHealthVersion !=
                _dirtyTracker.HealthVersion)
            {
                RefreshHealth();
            }
        }

        public void Dispose()
        {
            ReleaseAllViews();
        }

        private void RebuildHud()
        {
            ReleaseAllViews();

            BuildTeamRows(
                TeamId.Player,
                _context.PlayerUnits);

            BuildTeamRows(
                TeamId.Enemy,
                _context.EnemyUnits);

            RefreshHealth();

            _lastRosterVersion =
                _dirtyTracker.RosterVersion;
        }

        private void BuildTeamRows(
            TeamId team,
            IReadOnlyList<UnitRuntime> teamUnits)
        {
            IReadOnlyList<UnitClassHudCatalog.Entry>
                presentations =
                    _view.Catalog.Entries;

            for (var presentationIndex = 0;
                 presentationIndex < presentations.Count;
                 presentationIndex++)
            {
                UnitClassHudCatalog.Entry presentation =
                    presentations[presentationIndex];

                if (presentation == null ||
                    presentation.ClassId ==
                    UnitClassId.None)
                {
                    continue;
                }

                int total =
                    CountUnitsByClass(
                        teamUnits,
                        presentation.ClassId,
                        aliveOnly: false);

                if (total == 0)
                    continue;

                HealthClassRowView row =
                    _view.AcquireRow(
                        team,
                        presentation);

                _activeRows.Add(
                    new ActiveRow(
                        team,
                        presentation.ClassId,
                        total,
                        row));

                for (var unitIndex = 0;
                     unitIndex < teamUnits.Count;
                     unitIndex++)
                {
                    UnitRuntime unit =
                        teamUnits[unitIndex];

                    if (unit.ClassId !=
                        presentation.ClassId)
                    {
                        continue;
                    }

                    UnitHealthSegmentView segment =
                        _view.AcquireSegment(
                            row,
                            unit.Id);

                    _activeSegments.Add(segment);
                }
            }
        }

        private void RefreshHealth()
        {
            for (var i = 0;
                 i < _activeSegments.Count;
                 i++)
            {
                UnitHealthSegmentView segment =
                    _activeSegments[i];

                // Юнита нет в контексте — значит он погиб и был убран
                // из симуляции UnitCleanupSystem. Сегмент при этом
                // остаётся на месте до перестроения ростера.
                if (!_context.TryGetUnit(
                        segment.UnitId,
                        out UnitRuntime unit))
                {
                    segment.SetHealth(
                        normalizedHealth: 0f,
                        isAlive: false);

                    continue;
                }

                segment.SetHealth(
                    unit.HealthNormalized,
                    unit.IsAlive);
            }

            RefreshRowCounts();

            _lastHealthVersion =
                _dirtyTracker.HealthVersion;
        }

        private void RefreshRowCounts()
        {
            for (var i = 0;
                 i < _activeRows.Count;
                 i++)
            {
                ActiveRow activeRow =
                    _activeRows[i];

                IReadOnlyList<UnitRuntime> teamUnits =
                    activeRow.Team == TeamId.Player
                        ? _context.PlayerUnits
                        : _context.EnemyUnits;

                int alive =
                    CountUnitsByClass(
                        teamUnits,
                        activeRow.ClassId,
                        aliveOnly: true);

                activeRow.View.SetCount(
                    alive,
                    activeRow.Total);
            }
        }

        private void ReleaseAllViews()
        {
            for (var i = 0;
                 i < _activeSegments.Count;
                 i++)
            {
                _view.ReleaseSegment(
                    _activeSegments[i]);
            }

            _activeSegments.Clear();

            for (var i = 0;
                 i < _activeRows.Count;
                 i++)
            {
                _view.ReleaseRow(
                    _activeRows[i].View);
            }

            _activeRows.Clear();
        }

        private static int CountUnitsByClass(
            IReadOnlyList<UnitRuntime> units,
            UnitClassId classId,
            bool aliveOnly)
        {
            var count = 0;

            for (var i = 0;
                 i < units.Count;
                 i++)
            {
                UnitRuntime unit =
                    units[i];

                if (unit.ClassId != classId)
                    continue;

                if (aliveOnly &&
                    !unit.IsAlive)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private sealed class ActiveRow
        {
            public TeamId Team { get; }
            public UnitClassId ClassId { get; }
            public int Total { get; }
            public HealthClassRowView View { get; }

            public ActiveRow(
                TeamId team,
                UnitClassId classId,
                int total,
                HealthClassRowView view)
            {
                Team = team;
                ClassId = classId;
                Total = total;
                View = view;
            }
        }
    }
}
