// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Handle scenes loading and keeps tracks of the current loaded scene and loading scenes through the NetCode
    /// NetworkManager.
    /// </summary>
    public class SceneLoader
    {
        private static string s_currentScene = null;

        public bool SceneLoaded { get; private set; } = false;

        public SceneLoader() => SceneManager.sceneLoaded += OnSceneLoaded;

        ~SceneLoader()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneLoaded = true;
            s_currentScene = scene.name;
            _ = SceneManager.SetActiveScene(scene);
        }

        public void LoadScene(string scene, bool useNetManager = true)
        {
            Debug.Log($"LoadScene({scene}) (currentScene = {s_currentScene}, IsClient = {NetworkManager.Singleton.IsClient})");
            if (scene == s_currentScene) return;

            SceneLoaded = false;

            if (useNetManager && NetworkManager.Singleton.IsClient)
            {
                _ = NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
                return;
            }

            _ = SceneManager.LoadSceneAsync(scene);
        }
    }
}