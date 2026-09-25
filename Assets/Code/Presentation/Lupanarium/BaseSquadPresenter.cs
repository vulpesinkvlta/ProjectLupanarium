using System;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class BaseSquadPresenter : IStartable, IDisposable
    {
        private readonly RunState _run;
        private readonly UnitClassHudCatalog _catalog;
        private readonly BaseSquadView _view;

        public BaseSquadPresenter(RunState run, UnitClassHudCatalog catalog, BaseSquadView view)
        {
            _run = run;
            _catalog = catalog;
            _view = view;
        }

        public void Start()
        {
            _run.ProgressChanged += Refresh;
            Refresh();
        }

        public void Dispose() => _run.ProgressChanged -= Refresh;

        private void Refresh() =>
            _view.Refresh(_run.IsActive ? _run.Squad : Array.Empty<SquadEntry>(), _catalog);
    }
}
