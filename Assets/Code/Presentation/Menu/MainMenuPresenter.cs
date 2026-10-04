using System;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class MainMenuPresenter : IStartable, IDisposable
    {
        private readonly MainMenuView _view;
        private readonly MainMenuController _controller;

        public MainMenuPresenter(MainMenuView view, MainMenuController controller)
        {
            _view = view;
            _controller = controller;
        }

        public void Start()
        {
            _view.PlayRequested += Play;
            _view.DeleteRequested += Delete;
            _view.ResetAllRequested += ResetAll;
            L10n.LanguageChanged += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            _view.PlayRequested -= Play;
            _view.DeleteRequested -= Delete;
            _view.ResetAllRequested -= ResetAll;
            L10n.LanguageChanged -= Refresh;
        }

        private void Refresh() => _view.Refresh(_controller.HasActiveRun, _controller.Round);

        private void Play()
        {
            _view.SetBusy(true);
            if (!_controller.Play())
            {
                _view.SetBusy(false);
                _view.ShowError(L10n.Text("Не удалось открыть арену. Попробуйте ещё раз."));
            }
        }

        private void Delete()
        {
            _controller.DeleteRun();
            Refresh();
        }

        private void ResetAll()
        {
            _controller.ResetAllProgress();
            Refresh();
        }
    }
}
