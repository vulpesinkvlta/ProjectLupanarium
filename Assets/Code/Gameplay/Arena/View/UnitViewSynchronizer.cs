using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class UnitViewSynchronizer
    {
        private const int InitialDyingCapacity = 64;

        /// <summary>
        /// Раз во сколько кадров юнит пересчитывает порядок отрисовки.
        /// Юниты разнесены по кадрам через свой id, поэтому нагрузка
        /// размазана, а не приходит пиком раз в четыре кадра.
        /// </summary>
        private const int SortingUpdateInterval = 4;

        private static readonly ProfilerMarker UpdateMarker =
            new("Arena.ViewSynchronization");

        private readonly ArenaContext _context;
        private readonly UnitViewRegistry _viewRegistry;
        private readonly UnitViewPool _viewPool;
        private readonly DeadViewQueue _deadViewQueue;

        // Вью погибших не возвращаются в пул сразу: им дают доиграть
        // угасание. Список короткий — столько, сколько успело умереть
        // за полсекунды.
        private readonly List<DyingView> _dyingViews =
            new(InitialDyingCapacity);

        private int _frameIndex;

        public int DyingViewCount => _dyingViews.Count;

        public UnitViewSynchronizer(
            ArenaContext context,
            UnitViewRegistry viewRegistry,
            UnitViewPool viewPool,
            DeadViewQueue deadViewQueue)
        {
            _context = context ??
                throw new ArgumentNullException(nameof(context));

            _viewRegistry = viewRegistry ??
                throw new ArgumentNullException(nameof(viewRegistry));

            _viewPool = viewPool ??
                throw new ArgumentNullException(nameof(viewPool));

            _deadViewQueue = deadViewQueue ??
                throw new ArgumentNullException(
                    nameof(deadViewQueue));
        }

        public void UpdateViews(float interpolationAlpha, float deltaTime)
        {
            using (UpdateMarker.Auto())
            {
                StartDeathAnimations();
                TickDyingViews(deltaTime);

                _frameIndex++;

                float alpha =
                    Mathf.Clamp01(interpolationAlpha);

                var units = _context.AllUnits;

                for (var i = 0; i < units.Count; i++)
                {
                    var unit = units[i];

                    if (!unit.IsAlive)
                        continue;

                    if (!_viewRegistry.TryGet(
                            unit.Id,
                            out UnitView view))
                    {
                        continue;
                    }

                    Vector2 visualPosition =
                        Vector2.Lerp(
                            unit.PreviousPosition,
                            unit.Position,
                            alpha);

                    bool updateSorting =
                        (_frameIndex + unit.Id) % SortingUpdateInterval == 0;

                    view.SetVisualPosition(visualPosition, updateSorting);
                    view.TickVisuals(deltaTime);
                }
            }
        }

        /// <summary>
        /// Немедленно возвращает все догорающие вью в пул. Нужен при
        /// зачистке арены: иначе трупы прошлой волны доигрывали бы
        /// угасание поверх новой.
        /// </summary>
        public void ReleaseAllDying()
        {
            for (var i = 0; i < _dyingViews.Count; i++)
                _viewPool.Release(_dyingViews[i].View);

            _dyingViews.Clear();
        }

        private void StartDeathAnimations()
        {
            IReadOnlyList<int> deadUnitIds =
                _deadViewQueue.UnitIds;

            for (var i = 0; i < deadUnitIds.Count; i++)
            {
                if (!_viewRegistry.Remove(
                        deadUnitIds[i],
                        out UnitView view))
                {
                    continue;
                }

                view.PlayDeath();

                _dyingViews.Add(
                    new DyingView(view, view.DeathDuration));
            }

            _deadViewQueue.Clear();
        }

        private void TickDyingViews(float deltaTime)
        {
            for (var i = _dyingViews.Count - 1; i >= 0; i--)
            {
                DyingView dying = _dyingViews[i];

                dying.Remaining -= deltaTime;
                dying.View.TickVisuals(deltaTime);

                if (dying.Remaining > 0f)
                {
                    _dyingViews[i] = dying;
                    continue;
                }

                _viewPool.Release(dying.View);

                // Порядок догорающих вью не важен, поэтому удаляем
                // подстановкой последнего вместо сдвига хвоста.
                _dyingViews[i] = _dyingViews[^1];
                _dyingViews.RemoveAt(_dyingViews.Count - 1);
            }
        }

        private struct DyingView
        {
            public readonly UnitView View;
            public float Remaining;

            public DyingView(UnitView view, float remaining)
            {
                View = view;
                Remaining = remaining;
            }
        }
    }
}
