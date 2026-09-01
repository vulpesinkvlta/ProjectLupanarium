using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    /// <summary>
    /// Платформенные настройки рантайма.
    ///
    /// Живёт в корневом скоупе и отрабатывает один раз за запуск.
    /// Всё, что здесь настраивается, нельзя задать в ProjectSettings
    /// одинаково для всех платформ: на мобилке важен потолок кадров
    /// ради батареи, на десктопе — нет.
    /// </summary>
    public sealed class PlatformBootstrap : IStartable
    {
        private const int MobileTargetFrameRate = 60;
        private const int DesktopTargetFrameRate = 120;

        public void Start()
        {
            ApplyFrameRate();

            // Бой идёт сам, без ввода игрока — экран не должен гаснуть
            // посреди волны.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log(
                $"[Platform] {Application.platform}, " +
                $"target {Application.targetFrameRate} fps, " +
                $"vSync {QualitySettings.vSyncCount}.");
        }

        private static void ApplyFrameRate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // В браузере частоту диктует requestAnimationFrame: попытка
            // задать свою приводит к рванью. Отдаём управление странице.
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
#else
            // vSync перекрывает targetFrameRate, поэтому его снимаем,
            // иначе потолок кадров просто не применится.
            QualitySettings.vSyncCount = 0;

            Application.targetFrameRate =
                IsHandheld()
                    ? MobileTargetFrameRate
                    : DesktopTargetFrameRate;
#endif
        }

        private static bool IsHandheld()
        {
            return Application.isMobilePlatform;
        }
    }
}
