// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_NETCODE_GAMEOBJECTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Utilities.Editor
{
    internal class NetcodeHashFixer : AssetPostprocessor
    {
        [MenuItem("Netcode/Regenerate all NetworkPrefab ids from NetworkManager")]
        public static async Task RegenerateAllNetworkPrefabHashIds()
        {
            var source = new TaskCompletionSource<(SearchContext context, IList<SearchItem> results)>();
            SearchService.Request(
                $"t:{nameof(NetworkManager)}",
                (context, results) => source.SetResult((context, results)));

            var (_, results) = await source.Task;
            foreach (var result in results)
            {
                var obj = result.ToObject<GameObject>();
                Debug.Log($"[NetcodeHashFixer] Checking NetworkManager: {obj}", obj);
                var managers = obj.GetComponentsInChildren<NetworkManager>(true);
                foreach (var manager in managers)
                {
                    RegenerateManagerNetworkPrefabHashIds(manager);
                }
            }
        }

        private static async void RegenerateManagerNetworkPrefabHashIds(NetworkManager manager)
        {
            var config = new SerializedObject(manager);
            var prefabs = config.FindProperty("NetworkConfig.NetworkPrefabs");
            foreach (var prop in prefabs.GetEnumerator().AsEnumerable<SerializedProperty>())
            {
                var prefabProp = prop.FindPropertyRelative("Prefab");
                var prefabObj = prefabProp.objectReferenceValue;
                if (prefabObj != null)
                {
                    var prefabPath = AssetDatabase.GetAssetPath(prefabObj);
                    await RegeneratePrefabHashIds(prefabPath);
                }
            }
        }

        private static async Task RegeneratePrefabHashIds(string prefabPath)
        {
            var yaml = await File.ReadAllTextAsync(prefabPath);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var changes = RegenerateNetworkHashIds(prefab.GetComponentsInChildren<NetworkObject>(true));
            EditorUtility.UnloadUnusedAssetsImmediate();

            if (changes?.Any() is true && prefabPath?.EndsWith(".prefab") is true)
            {
                // Debug.Log($"[NetcodeHashFixer] Checking {prefabPath}...");

                var numMatches = 0u;
                foreach (var (fileId, targetId, oldHash, newHash) in changes)
                {
                    if (fileId is { } id && targetId is { } target)
                    {
                        var start = yaml.IndexOf($"--- !u!1001 &{id}");
                        if (start == -1)
                            continue;

                        var end = yaml.IndexOf("--- !", start + 1);
                        var endIndex = end == -1 ? Index.End : Index.FromStart(end);

                        var section = yaml[start..endIndex];

                        var pattern = @$"(target: {{fileID: {target}, guid: .*, type: 3}}[\n\r]*\s*propertyPath: GlobalObjectIdHash[\n\r]* *value:) {oldHash}";
                        section = Regex.Replace(section, pattern, match =>
                        {
                            numMatches += 1u;
                            return match.Result($"$1 {newHash}");
                        });

                        yaml = yaml[..start] + section + yaml[endIndex..];
                    }
                }

                if (numMatches != 0)
                {
                    Debug.Log($"[NetcodeHashFixer] Saving {numMatches} changes in {prefabPath}...");
                    await File.WriteAllTextAsync(prefabPath, yaml);
                }
            }
        }

        [MenuItem("Netcode/Regenerate all network ids in all open scenes")]
        [Obsolete]
        public static void RegenerateAllNetworkHashIds()
        {
            var scenes = SceneManager.GetAllScenes().Where(s => s.isLoaded);
            foreach (var scene in scenes)
            {
                _ = RegenerateNetworkHashIds(scene);
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                _ = RegenerateNetworkHashIds(prefabStage.scene);
            }
        }

        public static (long? prefabFileId, long? targetFileId, uint oldHash, uint newHash)[] RegenerateNetworkHashIds(Scene scene)
        {
            var objects = scene.GetRootGameObjects().
                SelectMany(go => go.GetComponentsInChildren<NetworkObject>(true));
            return RegenerateNetworkHashIds(objects);
        }

        private static (long? prefabFileId, long? targetFileId, uint oldHash, uint newHash)[] RegenerateNetworkHashIds(IEnumerable<NetworkObject> objects)
        {
            IEnumerable<(long?, long?, uint, uint)> Impl()
            {
                foreach (var obj in objects)
                {
                    using var serializedObject = new SerializedObject(obj);
                    serializedObject.Update();
                    using var property = serializedObject.FindProperty("GlobalObjectIdHash");

                    // if the value overrides the prefab, it doesn't show up in the SerializedProperty
                    var oldHash = (uint)obj.GetField("GlobalObjectIdHash");
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (source != null)
                    {
                        var mod = PrefabUtility.GetPropertyModifications(obj).
                            LastOrDefault(p => p.propertyPath == "GlobalObjectIdHash" && source == p.target);

                        // if none is found, then the hash isn't being overridden - which is a problem.
                        // the hash must be overridden in the scene, so this will fix that.
                        oldHash = (uint)long.Parse(mod?.value ?? "0");
                    }

                    obj.GetMethod<Action>("GenerateGlobalObjectIdHash").Invoke();
                    // EditorUtility.SetDirty(obj);

                    var hash = (uint)obj.GetField("GlobalObjectIdHash");

                    // Debug.Log(new { obj, oldHash, hash, property.isDefaultOverride, property.prefabOverride, source = source != null });

                    property.intValue = 0;
                    _ = serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    serializedObject.Update();

                    property.intValue = (int)hash;
                    _ = serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    if (oldHash != hash)
                    {
                        Debug.Log($"[NetcodeHashFixer] {obj.name} ({oldHash} => {hash})", obj);

                        if (source != null)
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);

                            var handle = PrefabUtility.GetPrefabInstanceHandle(obj);
                            EditorUtility.SetDirty(handle);
                        }

                        var instance = PrefabUtility.GetPrefabInstanceHandle(obj);
                        var fileId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance.GetInstanceID(), out _, out long id) ? (long?)id : null;
                        var targetId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out _, out long objId) ? (long?)objId : null;
                        yield return (fileId, targetId, oldHash, hash);
                    }
                }
            }
            return Impl().ToArray();
        }

        private static IEnumerable<Scene> GetActiveScenes()
        {
            for (var i = 0; i != SceneManager.sceneCount; i += 1)
                yield return SceneManager.GetSceneAt(i);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            var types = importedAssets.Select(AssetDatabase.GetMainAssetTypeAtPath);
            foreach (var (path, type) in importedAssets.Zip(types))
            {
                if (typeof(SceneAsset).IsAssignableFrom(type))
                {
                    CheckScene(path);
                }
                if (typeof(GameObject).IsAssignableFrom(type))
                {
                    _ = RegeneratePrefabHashIds(path);
                }
            }
        }

        private static void CheckScene(string path)
        {
            var scene = GetActiveScenes().FirstOrDefault(s => s.path == path);
            var wasValid = scene.IsValid();
            var wasLoaded = scene.isLoaded;

            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            var anyChanges = RegenerateNetworkHashIds(scene).Any();
            anyChanges = RemoveOldPrefabModifications(in scene) || anyChanges;

            if (anyChanges)
            {
                _ = EditorSceneManager.MarkSceneDirty(scene);
                _ = EditorSceneManager.SaveScene(scene);
            }

            if (!wasLoaded)
            {
                _ = EditorSceneManager.CloseScene(scene, !wasValid);
            }
        }

        private static bool RemoveOldPrefabModifications(in Scene scene)
        {
            var anyChanges = false;
            foreach (var instance in scene.
                GetRootGameObjects().
                SelectMany(go => go.GetComponentsInChildren<Transform>(true)).
                Where(t => PrefabUtility.GetPrefabInstanceHandle(t.gameObject)))
            {
                var mods = PrefabUtility.GetPropertyModifications(instance.gameObject);
                var cleanedMods = mods.Where(m => m.target != null).ToArray();
                if (cleanedMods.Length != mods.Length)
                {
                    PrefabUtility.SetPropertyModifications(instance, cleanedMods);
                    anyChanges = true;
                }
            }
            return anyChanges;
        }
    }
}

#endif
