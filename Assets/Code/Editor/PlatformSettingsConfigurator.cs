using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Code.Editor
{
    /// <summary>
    /// Применяет рекомендованные настройки сборки для целевых платформ.
    ///
    /// Эти настройки нельзя держать в коде игры: они живут в ProjectSettings
    /// и правятся мышкой в десятке разных вкладок. Меню приводит их
    /// к одному известному состоянию и документирует, почему выбрано
    /// именно так.
    /// </summary>
    public static class PlatformSettingsConfigurator
    {
        private const string MenuRoot = "Gladiator Game/Platform/";

        [MenuItem(MenuRoot + "Настроить WebGL")]
        public static void ConfigureWebGl()
        {
            var target = NamedBuildTarget.WebGL;

            // Главные рычаги размера билда. Brotli жмёт сильнее gzip,
            // но требует правильных заголовков от сервера — на itch.io
            // и GitHub Pages работает.
            PlayerSettings.WebGL.compressionFormat =
                WebGLCompressionFormat.Brotli;

            // Кэш в IndexedDB: повторный заход не качает данные заново.
            PlayerSettings.WebGL.dataCaching = true;

            // Исключения дорого стоят по размеру и скорости. Для релиза
            // хватает явно брошенных, полный стек не нужен.
            PlayerSettings.WebGL.exceptionSupport =
                WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            ApplySizeOptimizations(target);

            Debug.Log(
                "[Platform] WebGL настроен: Brotli, кэш данных, " +
                "обрезка кода.");

            Save();
        }

        [MenuItem(MenuRoot + "Настроить Android")]
        public static void ConfigureAndroid()
        {
            var target = NamedBuildTarget.Android;

            PlayerSettings.SetScriptingBackend(
                target,
                ScriptingImplementation.IL2CPP);

            // Google Play уже давно не принимает сборки без ARM64.
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARM64;

            ApplySizeOptimizations(target);

            Debug.Log(
                "[Platform] Android настроен: IL2CPP, ARM64, обрезка кода.");

            Save();
        }

        [MenuItem(MenuRoot + "Настроить iOS")]
        public static void ConfigureIos()
        {
            var target = NamedBuildTarget.iOS;

            PlayerSettings.SetScriptingBackend(
                target,
                ScriptingImplementation.IL2CPP);

            ApplySizeOptimizations(target);

            Debug.Log("[Platform] iOS настроен: IL2CPP, обрезка кода.");

            Save();
        }

        /// <summary>
        /// Общее для всех платформ: выкинуть неиспользуемый код
        /// и собирать IL2CPP с упором на размер, а не на скорость.
        ///
        /// Высокий уровень обрезки может выкинуть типы, которые
        /// достаются только через рефлексию. У нас таких нет — вся
        /// сериализация идёт через JsonUtility и поля, — но если
        /// появятся, их придётся защитить файлом link.xml.
        /// </summary>
        private static void ApplySizeOptimizations(NamedBuildTarget target)
        {
            PlayerSettings.SetManagedStrippingLevel(
                target,
                ManagedStrippingLevel.High);

            PlayerSettings.SetIl2CppCodeGeneration(
                target,
                Il2CppCodeGeneration.OptimizeSize);

            PlayerSettings.stripEngineCode = true;
        }

        private static void Save()
        {
            AssetDatabase.SaveAssets();
        }
    }
}
