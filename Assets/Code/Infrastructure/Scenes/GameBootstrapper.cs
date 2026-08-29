using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Точка входа игры: со сцены Bootstrap уводит игрока дальше.
    ///
    /// Обходится без DI намеренно. Корневой скоуп создаётся лениво,
    /// при первом обращении из сцены, а бутстраппер отрабатывает раньше
    /// и ничего из контейнера не просит — GameSceneLoader зависимостей
    /// не имеет, поэтому создать его здесь дешевле, чем городить
    /// ещё один скоуп ради одной строки.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameScene _firstScene = GameScene.Base;

        private void Start()
        {
            new GameSceneLoader().TryLoad(_firstScene);
        }
    }
}
