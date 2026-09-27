using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class BlessingBattlePresenter : IStartable, ITickable, IDisposable
    {
        private readonly BlessingBattleView _view;
        private readonly BlessingBattleController _controller;
        private readonly BlessingCatalog _catalog;
        private readonly LupanariumState _school;
        private readonly RunState _run;
        private readonly ArenaSimulation _simulation;
        private readonly ArenaSimulationClock _clock;
        private BlessingConfig _selected;
        private bool _lastFighting, _lastAvailable;
        private bool Available => _run.Phase == BattleFlowState.Fighting && _simulation.IsRunning && !_clock.IsPaused;

        public BlessingBattlePresenter(BlessingBattleView view, BlessingBattleController controller,
            BlessingCatalog catalog, LupanariumState school, RunState run, ArenaSimulation simulation, ArenaSimulationClock clock)
        { _view = view; _controller = controller; _catalog = catalog; _school = school; _run = run; _simulation = simulation; _clock = clock; }
        public void Start()
        {
            _view.Build(_catalog);
            _view.ValidatePoint = _controller.IsValidPoint;
            _view.SelectionRequested += Select;
            _view.CastRequested += Cast;
            _school.Changed += Refresh;
            Refresh();
        }
        public void Tick()
        {
            if (_lastAvailable != Available || _lastFighting != (_run.Phase == BattleFlowState.Fighting)) Refresh();
        }
        private void Refresh()
        {
            _lastFighting = _run.Phase == BattleFlowState.Fighting;
            _lastAvailable = Available;
            _view.Refresh(_school, _lastFighting, _lastAvailable);
            if (!_lastAvailable) _selected = null;
        }
        private void Select(BlessingConfig config)
        {
            if (!Available || !_controller.CanCast(config)) return;
            _selected = config;
            _view.SelectTarget(config);
        }
        private void Cast(Vector2 point)
        {
            var config = _selected;
            if (!Available || !_controller.TryCast(config, point)) return;
            _view.ShowCast(point, config);
            _selected = null;
        }
        public void Dispose()
        {
            _school.Changed -= Refresh;
            _view.SelectionRequested -= Select;
            _view.CastRequested -= Cast;
            _view.ValidatePoint = null;
        }
    }
}
