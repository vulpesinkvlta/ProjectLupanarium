namespace Code.Gameplay
{
    public sealed class MainMenuController
    {
        private readonly RunState _run;
        private readonly SaveService _save;
        private readonly IGameSceneLoader _loader;

        public bool HasActiveRun => _run.IsActive;
        public int Round => _run.WaveNumber;

        public MainMenuController(RunState run, SaveService save, IGameSceneLoader loader)
        {
            _run = run;
            _save = save;
            _loader = loader;
        }

        // Arena resumes the saved phase or offers a starting squad for a new run.
        public bool Play() => _loader.TryLoad(GameScene.Arena);

        public void DeleteRun()
        {
            if (!_run.IsActive)
                return;

            _run.Reset();
            // Save the school together with an empty run; never delete the whole save.
            _save.Save();
        }
    }
}
