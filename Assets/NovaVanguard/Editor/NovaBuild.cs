using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NovaVanguard.Editor
{
    /// <summary>
    /// Build entry points. Use from the menu (Nova Vanguard > Build > ...) or headless:
    ///   Unity -batchmode -quit -projectPath . -executeMethod NovaVanguard.Editor.NovaBuild.Android
    /// Output goes to Builds/&lt;Platform&gt;/.
    /// </summary>
    public static class NovaBuild
    {
        const string ScenePath = "Assets/NovaVanguard/Scenes/Main.unity";
        static string[] Scenes => new[] { ScenePath };
        static string Version => PlayerSettings.bundleVersion;

        [MenuItem("Nova Vanguard/Build/Windows (x64)")] public static void Windows() => Build(BuildTarget.StandaloneWindows64, $"Builds/Windows/NovaVanguard.exe");
        [MenuItem("Nova Vanguard/Build/macOS")] public static void Mac() => Build(BuildTarget.StandaloneOSX, $"Builds/macOS/NovaVanguard.app");
        [MenuItem("Nova Vanguard/Build/Linux")] public static void Linux() => Build(BuildTarget.StandaloneLinux64, $"Builds/Linux/NovaVanguard.x86_64");
        [MenuItem("Nova Vanguard/Build/Android APK")] public static void Android() { EditorUserBuildSettings.buildAppBundle = false; Build(BuildTarget.Android, $"Builds/Android/NovaVanguard-{Version}.apk"); }
        [MenuItem("Nova Vanguard/Build/Android AAB (Play Store)")] public static void AndroidBundle() { EditorUserBuildSettings.buildAppBundle = true; Build(BuildTarget.Android, $"Builds/Android/NovaVanguard-{Version}.aab"); }
        [MenuItem("Nova Vanguard/Build/iOS (Xcode project)")] public static void iOS() => Build(BuildTarget.iOS, "Builds/iOS");
        [MenuItem("Nova Vanguard/Build/WebGL")] public static void WebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        [MenuItem("Nova Vanguard/Build/All Desktop + Android + WebGL")]
        public static void AllDesktopMobileWeb() { Windows(); Mac(); Linux(); Android(); WebGL(); }

        static void Build(BuildTarget target, string path)
        {
            if (!File.Exists(ScenePath)) NovaProjectSetup.RunSetup();
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (target == BuildTarget.Android)
            {
                // Android version code must increase for each Play Store upload.
                PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode, VersionCode());
                if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName)) Debug.LogWarning("[Nova Vanguard] No keystore configured; building a debug-signed APK. Configure Player Settings > Publishing Settings for release.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };
            Debug.Log($"[Nova Vanguard] Building {target} -> {path}");
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded) Debug.Log($"[Nova Vanguard] {target} build succeeded: {s.totalSize / (1024 * 1024)} MB in {s.totalTime.TotalSeconds:0}s -> {path}");
            else
            {
                Debug.LogError($"[Nova Vanguard] {target} build {s.result}: {s.totalErrors} errors");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static int VersionCode()
        {
            var parts = Version.Split('.').Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
            int major = parts.Length > 0 ? parts[0] : 1, minor = parts.Length > 1 ? parts[1] : 0, patch = parts.Length > 2 ? parts[2] : 0;
            return major * 10000 + minor * 100 + patch;
        }
    }
}
