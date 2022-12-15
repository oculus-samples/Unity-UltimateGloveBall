// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_SEARCH_EXTENSIONS

using System;
using System.Collections.Generic;
using UnityEditor;

public static class MenuHelpers
{
    private static Type DependencyGraphViewerType { get; } = Type.GetType("UnityEditor.Search.DependencyGraphViewer, com.unity.search.extensions.editor");

    [MenuItem("Assets/Dependencies/Graph Dependencies", true)]
    public static bool GraphDependencies_Validate() => DependencyGraphViewerType != null;

    [MenuItem("Assets/Dependencies/Graph Dependencies", priority = 10110)]
    public static void GraphDependencies()
    {
        var createWindow = typeof(EditorWindow).GetMethod(
                nameof(EditorWindow.CreateWindow),
                new[] { typeof(Type[]) }
            ).
            MakeGenericMethod(DependencyGraphViewerType);
        var win = (EditorWindow)createWindow.Invoke(null, new[] { new Type[0] });
        win.Show();
        var import = win.GetMethod<Action<ICollection<UnityEngine.Object>>>("Import");
        EditorApplication.delayCall += () => import.Invoke(Selection.objects);
    }

    [MenuItem("Tools/Find Missing Dependencies")]
    public static async void FindMissingDependencies()
    {
        var source = new TaskCompletionSource<IList<SearchItem>>();
        var provider = SearchService.Providers.FirstOrDefault(p => p.name is "Dependencies");
        var context = new SearchContext(new[] { provider }, "is:missing");
        SearchService.Request(context, (_, results) => source.SetResult(results), SearchFlags.Default | SearchFlags.WantsMore);

        var binding = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var getDescription = typeof(Dependency).GetMethod("GetDescription", binding).
            CreateDelegate(typeof(System.Func<SearchItem, string>)) as System.Func<SearchItem, string>;

        var results = await source.Task;
        foreach (var result in results)
        {
            var proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = true;
            proc.StartInfo = new()
            {
                FileName = "cmd",
                Arguments = $"/c \"git log -S {result.id} --name-only --pretty=format: -- *.meta \"",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            proc.Start();

            var gitResult = await proc.StandardOutput.ReadLineAsync();
            var description = getDescription(result);
            var sourceFile = Path.GetFileNameWithoutExtension(description);
            if (gitResult?.Length > 1)
            {
                var targetFile = Path.GetFileNameWithoutExtension(gitResult);
                Debug.Log($"{sourceFile} missing link to {targetFile}\n\n{description}\n\n{gitResult}\n");
            }
            else
            {
                Debug.Log($"{sourceFile} missing link to {result.id}\n\n{description}\n");
            }
        }
    }
}

#endif
