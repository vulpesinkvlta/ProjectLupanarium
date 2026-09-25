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
        private readonly IGameSceneLoader _sceneLoader;

        public LupanariumController(
            LupanariumState lupanarium,
            RunState runState,
            IGameSceneLoader sceneLoader)
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
        /// Одна кнопка на предмет: если он не куплен — покупаем,
        /// если куплен, но не надет — надеваем.
        /// </summary>
        public bool TryBuyOrEquip(ItemConfig item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _lupanarium.IsOwned(item)
                ? _lupanarium.TryEquip(item)
                : _lupanarium.TryBuy(item);
        }

        public bool TryUnlockUnit(RosterEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            return _lupanarium.TryUnlockUnit(entry);
        }

        public bool TryUnlockFormation(FormationEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            return _lupanarium.TryUnlockFormation(entry);
        }

        /// <summary>
        /// Продолжает незавершённый забег или начинает новый после поражения.
        /// </summary>
        public void StartRun()
        {
            if (!_runState.IsActive)
                _runState.Reset();
            _sceneLoader.TryLoad(GameScene.Arena);
        }
    }
}
