using System;

namespace Code.Gameplay
{
    /// <summary>
    /// Действия игрока в лупанарии: улучшить постройку, выйти на арену.
    /// </summary>
    public sealed class LupanariumController
    {
        private readonly LupanariumState _lupanarium;
        private readonly RunState _runState;
        private readonly GameSceneLoader _sceneLoader;

        public LupanariumController(
            LupanariumState lupanarium,
            RunState runState,
            GameSceneLoader sceneLoader)
        {
            _lupanarium = lupanarium ??
                throw new ArgumentNullException(nameof(lupanarium));

            _runState = runState ??
                throw new ArgumentNullException(nameof(runState));

            _sceneLoader = sceneLoader ??
                throw new ArgumentNullException(nameof(sceneLoader));
        }

        public bool TryUpgrade(SchoolBuildingConfig building)
        {
            return _lupanarium.TryUpgrade(building);
        }

        /// <summary>
        /// Начинает новый забег и уходит на арену.
        ///
        /// Сброс забега происходит здесь, а не в BattleFlowController:
        /// RunState живёт в корневом скоупе и переживает смену сцены,
        /// поэтому обнулять его должен тот, кто забег начинает.
        /// </summary>
        public void StartRun()
        {
            _runState.Reset();
            _sceneLoader.TryLoad(GameScene.Arena);
        }
    }
}
