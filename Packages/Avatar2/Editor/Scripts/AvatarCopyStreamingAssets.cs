// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Avatar2
{
    // ScriptableObject class name must match the filename for it to get the path correctly
    public class AvatarCopyStreamingAssets : ScriptableObject
    {
        private static readonly string _sourcePath = "Oculus/Avatar2/StreamingAssets";
        private const string _errorStr = "error";

        [MenuItem("AvatarSDK2/StreamingAssets/Copy Core Assets To Streaming Assets")]
        public static void CopyFilesOver()
        {
            CopyFilesRecursively(new DirectoryInfo(Path.Combine(Application.dataPath, _sourcePath)), Directory.CreateDirectory(Application.streamingAssetsPath));
            File.WriteAllText(GetVersionFilePath(), $"{GetBuildNumber()}\n");
            AssetDatabase.Refresh();
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (FileInfo file in source.GetFiles())
            {
                if (file.Extension != ".meta")
                {
                    var targetPath = Path.Combine(target.FullName, file.Name);
                    file.CopyTo(targetPath, overwrite: true);
                    Debug.LogFormat("Copied {0} to {1}", file.FullName, targetPath);
                }
            }
        }

        public static bool DoesFileVersionMatch()
        {
            var textAsset = GetVersionFilePath();
            if (!File.Exists(textAsset))
            {
                return false;
            }

            try
            {
                return File.ReadAllText(textAsset).Trim() == GetBuildNumber();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return false;
        }

        public static string GetBuildNumber()
        {
            return SDKVersionInfo.CurrentVersion().ToString();
        }

        private static string GetVersionFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "Oculus",
              ".avatar_sdk_assets_version.txt");
        }
    }

    [InitializeOnLoad]
    public class StartupTrigger
    {
        static StartupTrigger()
        {
            if (!SessionState.GetBool("AvatarCopyStreamingAssetsRanOnce", false))
            {
                EditorApplication.update += RunOnce;
            }
        }

        static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            if (!AvatarCopyStreamingAssets.DoesFileVersionMatch())
            {
                PopupDialog.ShowWindow();
                SessionState.SetBool("AvatarCopyStreamingAssetsRanOnce", true);
            }
        }
    }

    public class PopupDialog : EditorWindow
    {
        public static void ShowWindow()
        {
            if (HasOpenInstances<PopupDialog>())
            {
                return;
            }

            var window = ScriptableObject.CreateInstance<PopupDialog>();
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 500, 200);
            window.ShowPopup();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Copy SDK Avatars 2.0 Streaming Assets", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField(
                "SDK Avatars 2.0 Streaming Assets are not present or are out of date.\n\n" +
                "This is expected if it's your first time working with or if you recently upgraded the Avatars SDK.\n\n" +
                "Finally, these assets are optional and you can update them later with the \"AvatarSDK2/StreamingAssets/Copy Assets To Streaming Assets\" menu button.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OK"))
                {
                    AvatarCopyStreamingAssets.CopyFilesOver();
                    this.Close();
                }

                if (GUILayout.Button("Cancel"))
                {
                    this.Close();
                }
            }
        }
    }
}
