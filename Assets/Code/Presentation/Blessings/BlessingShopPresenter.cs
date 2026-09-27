using System;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class BlessingShopPresenter : IStartable, IDisposable
    {
        private readonly BlessingShopView _view;
        private readonly BlessingCatalog _catalog;
        private readonly LupanariumState _school;
        public BlessingShopPresenter(BlessingShopView view, BlessingCatalog catalog, LupanariumState school)
        { _view = view; _catalog = catalog; _school = school; }
        public void Start()
        {
            _view.Build(_catalog);
            _view.BuyRequested += Buy;
            _school.Changed += Refresh;
            Refresh();
        }
        public void Dispose() { _view.BuyRequested -= Buy; _school.Changed -= Refresh; }
        private void Buy(BlessingConfig config) { if (_catalog.Contains(config)) _school.TryBuyBlessing(config); }
        private void Refresh() => _view.Refresh(_school);
    }
}
