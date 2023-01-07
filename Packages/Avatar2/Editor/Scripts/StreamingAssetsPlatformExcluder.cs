using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// This editor script adds a preprocess build step that excludes streaming assets added by the Avatars SDK which
    /// aren't needed on the current target platform. For example, if building for Quest, then Rift streaming assets are
    /// excluded. This reduces the binary size of release builds. Development builds aren't affected.
    /// </summary>
    public class StreamingAssetsPlatformExcluder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {

        private static char s = Path.DirectorySeparatorChar;

        private List<string> RiftPaths = new List<string>()
        {
            $"Assets{s}StreamingAssets{s}SampleAssets{s}PresetAvatars_Rift.zip",
        };

        private List<string> QuestPaths = new List<string>()
        {
            $"Assets{s}StreamingAssets{s}SampleAssets{s}PresetAvatars_Quest.zip",
        };


        private List<string> FastloadPaths = new List<string>()
        {
            $"Assets{s}StreamingAssets{s}SampleAssets{s}PresetAvatars_Fastload.zip",
        };



        public bool retainOnDevelpoment = false;

        public int callbackOrder => default;

        private static string tempPathForExclusion;

        private static string GetTempPathForExclusion(string filename)
        {
            return Path.Combine(tempPathForExclusion, Path.GetFileName(filename));
        }

        private static async void QueueAssetDatabaseRefresh()
        {
            await System.Threading.Tasks.Task.Yield();
            AssetDatabase.Refresh();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Application.logMessageReceived += OnBuildError; // Start listening for errors

            tempPathForExclusion = FileUtil.GetUniqueTempPathInProject();
            if (!Directory.Exists(tempPathForExclusion))
            {
                Directory.CreateDirectory(tempPathForExclusion);
            }

            if (!retainOnDevelpoment || !report.summary.options.HasFlag(BuildOptions.Development))
            {
                Debug.Log($"BUILD EXCLUSION: NOTE: Excluding non platform files from build. Files temporarily moved to {tempPathForExclusion} ");


                // todo: what platforms use Fastload paths? Should we include / exclude them?
                // todo: we need to figure out exactly which assets will be needed on iOS
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                {
                    IncludeFiles(QuestPaths);
                    ExcludeFiles(RiftPaths);

                }
                else
                {
                    IncludeFiles(RiftPaths);
                    ExcludeFiles(QuestPaths);

                }
            }
            QueueAssetDatabaseRefresh();
        }

        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                Application.logMessageReceived -= OnBuildError; // Stop listening for errors

                IncludeAllFiles();

                QueueAssetDatabaseRefresh();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Application.logMessageReceived -= OnBuildError; // Stop listening for errors

            if (!retainOnDevelpoment || !report.summary.options.HasFlag(BuildOptions.Development))
            {
                IncludeAllFiles();
            }
            QueueAssetDatabaseRefresh();
        }


        private void IncludeAllFiles()
        {
            IncludeFiles(RiftPaths);
            IncludeFiles(QuestPaths);
            IncludeFiles(FastloadPaths);

        }

        private static void MoveFile(string src, string dst)
        {
            // Uncomment to view file moves
            // Debug.Log("mv " + src + ' ' + dst);
            File.Move(src, dst);
        }

        private void ExcludeFiles(List<string> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (File.Exists(paths[i]))
                {
                    MoveFile(paths[i] + ".meta", GetTempPathForExclusion(paths[i] + ".meta")); // Manual storing of the meta file is required to avoid changing guids at the end of the process.
                    MoveFile(paths[i], GetTempPathForExclusion(paths[i]));
                }
                else
                {
                    Debug.LogWarning($"BUILD EXCLUSION: Trying to exclude file that doesn't exist: {paths[i]}");
                }
            }
        }

        private void IncludeFiles(List<string> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (File.Exists(GetTempPathForExclusion(paths[i])))
                {
                    try
                    {
                        MoveFile(GetTempPathForExclusion(paths[i]), paths[i]);
                        MoveFile(GetTempPathForExclusion(paths[i] + ".meta"), paths[i] + ".meta"); // Manual restoring of the meta file is required to avoid changing guids.
                    }
                    catch (IOException)
                    {
                        // This can occur if Unity gets force-closed in the middle of a build
                        Debug.LogWarning("The StreamingAssets folder may be in a bad state. May need to revert deleted Asset files.");
                    }
                }
            }
        }
    }
}
