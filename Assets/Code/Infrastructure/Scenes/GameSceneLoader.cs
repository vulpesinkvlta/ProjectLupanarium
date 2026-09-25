using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Gameplay
{
    public enum GameScene
    {
        MainMenu = 0,
        Base = 1,
        Arena = 2
    }

    /// <summary>
    /// Загрузка сцен по логическому имени.
    ///
    /// Намеренно не падает, если сцены нет в Build Settings: проект
    /// собирается по частям, и отсутствие лупанария не должно ломать
    /// уже работающую арену. Вместо исключения — понятная ошибка в лог
    /// и false, чтобы вызывающий мог выбрать запасной путь.
    /// </summary>
    public interface IGameSceneLoader
    {
        bool TryLoad(GameScene scene);
    }

    public sealed class GameSceneLoader : IGameSceneLoader
    {
        public bool TryLoad(GameScene scene)
        {
            string sceneName = GetSceneName(scene);

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneLoader] Сцена '{sceneName}' не добавлена " +
                    $"в Build Settings, переход отменён.");

                return false;
            }

            Debug.Log($"[SceneLoader] Переход на сцену '{sceneName}'.");

            SceneManager.LoadScene(sceneName);
            return true;
        }

        public static string GetSceneName(GameScene scene)
        {
            return scene switch
            {
                GameScene.MainMenu => "1.MainMenu",
                GameScene.Base => "2.Base",
                GameScene.Arena => "3.Arena",

                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(scene),
                    scene,
                    "Неизвестная сцена.")
            };
        }
    }
}
