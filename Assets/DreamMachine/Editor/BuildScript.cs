using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DreamMachine.EditorTools
{
    public static class BuildScript
    {
        public const string PackageId = "com.jacobgino.dreammachine";

        [MenuItem("Dream Machine/Build Android APK")]
        public static void BuildAndroid()
        {
            ConfigureAndroid();
            string path = "Builds/Android/DreamMachine.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Builds/Android");
            string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            if (scenes == null || scenes.Length == 0)
            {
                scenes = new[] { "Assets/Scenes/DreamMachine.unity" };
            }
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Android build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Debug.Log("Android APK: " + Path.GetFullPath(path));
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "Jacob Gino";
            PlayerSettings.productName = "Dream Machine";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.stripEngineCode = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }
}
