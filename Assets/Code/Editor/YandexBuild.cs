using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Code.Editor
{
    public static class YandexBuild
    {
        [MenuItem("Gladiator Game/Yandex/Configure Web build")]
        public static void Configure()
        {
            var target = NamedBuildTarget.WebGL;
            var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (!defines.Contains("YANDEX_GAMES")) defines.Add("YANDEX_GAMES");
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            PlayerSettings.WebGL.template = "PROJECT:Yandex";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Gladiator Game/Yandex/Build release")]
        public static void Build()
        {
            Configure();
            Directory.CreateDirectory("Builds/Yandex");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.WebGL, locationPathName = "Builds/Yandex", options = BuildOptions.None });
            File.WriteAllText("Builds/yandex-build-result.txt", report.summary.result + "\n" + report.summary.totalSize + " bytes\n" + report.summary.totalErrors + " errors");
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Yandex build failed; see Editor log.");
            Debug.Log("Yandex release ready in Builds/Yandex. Upload ZIP with index.html at its root.");
        }
    }
}
