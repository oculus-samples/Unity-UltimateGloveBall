// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using static System.Environment;

namespace Meta.Utilities.Editor
{
    public static class BuildTools
    {
        [MenuItem("Tools/Regenerate Project Files")]
        private static void GenerateProjectFiles()
        {
            Debug.Log("GenerateProjectFiles: CodeEditor.SetExternalScriptEditor");
            CodeEditor.SetExternalScriptEditor("code");

            Debug.Log("GenerateProjectFiles: AssetDatabase.Refresh");
            AssetDatabase.Refresh();

            Debug.Log("GenerateProjectFiles: CurrentCodeEditor.SyncAll");
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }
        private static string ExtractHintPath(string line)
        {
            var match = Regex.Match(line, "\\<HintPath\\>(.*)\\<\\/HintPath\\>");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Tools/Regenerate Project Files and Copy All Assemblies to Root")]
        public static async Task GenerateProjectFilesWithAssemblies()
        {
            GenerateProjectFiles();

            var files = Directory.EnumerateFiles(ProjectRoot, "*.csproj").
                Select(f => File.ReadAllLinesAsync(f)).
                ToArray();
            Debug.Log($"{files.Length} csproj files found.");

            var fileLines = await Task.WhenAll(files);
            Debug.Log($"All csproj files loaded.");

            var paths = fileLines.
                SelectMany(l => l).
                Select(ExtractHintPath).
                Where(path => path != null).
                Distinct().
                ToArray();
            Debug.Log($"{paths.Length} dll files found.");

            foreach (var path in paths)
            {
                var name = Path.Combine(ProjectRoot, "ReferenceAssemblies", Path.GetFileName(path));
                if (!File.Exists(name))
                {
                    Debug.Log($"Copying {name}");
                    File.Copy(path, name, false);
                }
            }
        }

        public static async void GenerateProjectFilesWithAssembliesAndQuit()
        {
            await GenerateProjectFilesWithAssemblies().
                ContinueWith(_ => Application.Quit(0));
        }

        [InitializeOnLoadMethod]
        public static async Task SetKeystorePassword()
        {
            var lines = await File.ReadAllLinesAsync(Path.Combine(ProjectRoot, "keystore-passwords.txt"));

            if (lines != null)
            {
                if (lines.Length > 0)
                    PlayerSettings.Android.keystorePass = lines[0].Trim();

                if (lines.Length > 1)
                    PlayerSettings.Android.keyaliasPass = lines[1].Trim();
            }
        }

        public static async void BuildAndroid()
        {
            var keystorePass = GetEnvironmentVariable("UNITY_KEYSTORE_PASSWORD");
            if (!string.IsNullOrEmpty(keystorePass))
            {
                Debug.Log("Setting keystore password from $UNITY_KEYSTORE_PASSWORD");
                PlayerSettings.Android.keystorePass = keystorePass;
            }
            else
            {
                Debug.Log("$UNITY_KEYSTORE_PASSWORD is not set");
            }

            var keyaliasPass = GetEnvironmentVariable("UNITY_KEYALIAS_PASSWORD");
            if (!string.IsNullOrEmpty(keyaliasPass))
            {
                Debug.Log("Setting keyalias password from $UNITY_KEYALIAS_PASSWORD");
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }
            else
            {
                Debug.Log("$UNITY_KEYALIAS_PASSWORD is not set");
            }

            var apkVersionStr = GetEnvironmentVariable("UNITY_APK_VERSION");
            if (!string.IsNullOrEmpty(keyaliasPass) && int.TryParse(apkVersionStr, out var apkVersion))
            {
                Debug.Log($"Setting apk version from $UNITY_APK_VERSION to {apkVersion}");
                PlayerSettings.Android.bundleVersionCode = apkVersion;
                PlayerSettings.bundleVersion = $"0.1.{apkVersion:d5}";
            }
            else
            {
                Debug.Log("$UNITY_APK_VERSION is not set");
            }

            var androidSdk = GetEnvironmentVariable("UNITY_ANDROID_SDK");
            if (!string.IsNullOrEmpty(androidSdk))
            {
                EditorPrefs.SetString("AndroidSdkRoot", androidSdk);
                Debug.Log($"AndroidSdkRoot={androidSdk}");
            }

            var androidNdk = GetEnvironmentVariable("UNITY_ANDROID_NDK");
            if (!string.IsNullOrEmpty(androidNdk))
            {
                EditorPrefs.SetString("AndroidNdkRootR21D", androidNdk);
                Debug.Log($"AndroidNdkRootR21D={androidNdk}");
            }

            await RunHashFixerOnPrefabs();

            var sceneList = EditorBuildSettings.scenes.
                Where(s => s.enabled).
                Select(s => s.path).
                ToArray();

            ReimportScenes(sceneList);

            var buildOptions = BuildOptions.None;
            // buildOptions |= BuildOptions.AllowDebugging;
            buildOptions |= BuildOptions.Development;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sceneList.ToArray(),
                locationPathName = ".\\build\\QuestSocialGameplay.apk",
                target = BuildTarget.Android,
                options = buildOptions
            };

            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log(buildResult.summary.result);
            Debug.Log(buildResult.steps.
                Select(step => $"{step.name}.\t\n{step.messages.Select(m => m.content).ListToString("\t\n")}").
                ListToString("\n"));
        }

        private static async Task RunHashFixerOnPrefabs()
        {
            var fixer = GetType("NetcodeHashFixer");
            var fixMethod = fixer?.GetMethod("RegenerateAllNetworkPrefabHashIds");
            var fixMethodReturn = fixMethod?.Invoke(null, System.Array.Empty<object>());
            if (fixMethodReturn is Task fixMethodTask)
                await fixMethodTask;
        }

        private static System.Type GetType(string fullTypeName) =>
            System.AppDomain.CurrentDomain.GetAssemblies().
                SelectMany(a => a.GetTypes()).
                FirstOrDefault(t => t.FullName == fullTypeName);

        private static void ReimportScenes(string[] sceneList)
        {
            foreach (var scene in sceneList)
            {
                Debug.Log($"Reimporting: {scene}");
                AssetDatabase.ImportAsset(scene, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
